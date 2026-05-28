import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';

export const productLatency = new Trend('product_latency_ms');
export const checkoutLatency = new Trend('checkout_latency_ms');
export const checkoutAccepted = new Rate('checkout_accepted');

export const options = {
  scenarios: {
    browsing: {
      executor: 'ramping-vus',
      exec: 'browseCatalog',
      stages: [
        { duration: '20s', target: 20 },
        { duration: '40s', target: 20 },
        { duration: '20s', target: 0 }
      ]
    },
    checkout: {
      executor: 'constant-arrival-rate',
      exec: 'startCheckout',
      rate: 8,
      timeUnit: '1s',
      duration: '60s',
      preAllocatedVUs: 20,
      maxVUs: 50
    }
  },
  thresholds: {
    http_req_failed: ['rate<0.02'],
    'http_req_duration{endpoint:products}': ['p(95)<500'],
    'http_req_duration{endpoint:checkout}': ['p(95)<900'],
    checkout_accepted: ['rate>0.90']
  }
};

export function browseCatalog() {
  const response = http.get(`${BASE_URL}/products`, {
    tags: { endpoint: 'products' }
  });

  productLatency.add(response.timings.duration);
  check(response, {
    'products status is 200': (r) => r.status === 200,
    'products body has items': (r) => r.json('value')?.length > 0 || r.json()?.length > 0
  });
  sleep(1);
}

export function startCheckout() {
  const sku = `CASE-${String(randomIntBetween(1, 5000)).padStart(5, '0')}`;
  const payload = JSON.stringify({ items: [{ sku, quantity: 1 }] });
  const response = http.post(`${BASE_URL}/checkout`, payload, {
    headers: {
      'Content-Type': 'application/json',
      'Idempotency-Key': `k6-${__VU}-${__ITER}-${Date.now()}`
    },
    tags: { endpoint: 'checkout' }
  });

  checkoutLatency.add(response.timings.duration);
  checkoutAccepted.add(response.status === 202);
  check(response, {
    'checkout accepted': (r) => r.status === 202,
    'checkout has order id': (r) => Boolean(r.json('orderId'))
  });
  sleep(0.2);
}

export function handleSummary(data) {
  return {
    'results/summary.json': JSON.stringify(data, null, 2),
    'results/summary.html': renderHtml(data)
  };
}

function metricValue(data, name, stat) {
  return data.metrics[name]?.values?.[stat] || 0;
}

function renderHtml(data) {
  const productP95 = metricValue(data, 'http_req_duration{endpoint:products}', 'p(95)');
  const checkoutP95 = metricValue(data, 'http_req_duration{endpoint:checkout}', 'p(95)');
  const failRate = metricValue(data, 'http_req_failed', 'rate') * 100;
  const acceptedRate = metricValue(data, 'checkout_accepted', 'rate') * 100;
  const totalRequests = metricValue(data, 'http_reqs', 'count');

  const bars = [
    ['Products p95 ms', productP95, 500],
    ['Checkout p95 ms', checkoutP95, 900],
    ['Fail rate %', failRate, 2],
    ['Checkout accepted %', acceptedRate, 100]
  ];

  const svgBars = bars.map((bar, index) => {
    const [label, value, target] = bar;
    const width = Math.min(700, Math.round((value / target) * 700));
    const y = 40 + index * 58;
    return `
      <text x="20" y="${y}" class="label">${label}: ${Number(value).toFixed(2)}</text>
      <rect x="220" y="${y - 18}" width="700" height="24" fill="#eef2f7" rx="4"></rect>
      <rect x="220" y="${y - 18}" width="${width}" height="24" fill="#2f6f9f" rx="4"></rect>
      <line x1="920" y1="${y - 22}" x2="920" y2="${y + 10}" stroke="#b42318" stroke-width="2"></line>`;
  }).join('');

  return `<!doctype html>
<html lang="pt-BR">
<head>
  <meta charset="utf-8">
  <title>CaseCellShop - Relatório k6</title>
  <style>
    body { font-family: Arial, sans-serif; margin: 32px; color: #1f2937; }
    .card { border: 1px solid #d8dde8; border-radius: 8px; padding: 20px; margin-bottom: 20px; }
    .grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; }
    .stat { background: #f7f8fb; border: 1px solid #d8dde8; border-radius: 8px; padding: 16px; }
    .value { font-size: 28px; font-weight: 700; }
    .label { font-size: 14px; fill: #1f2937; }
  </style>
</head>
<body>
  <h1>CaseCellShop - Relatório de carga k6</h1>
  <p>Resumo gerado automaticamente a partir do teste de carga.</p>
  <div class="grid">
    <div class="stat"><div>Total requests</div><div class="value">${totalRequests}</div></div>
    <div class="stat"><div>Products p95</div><div class="value">${productP95.toFixed(0)} ms</div></div>
    <div class="stat"><div>Checkout p95</div><div class="value">${checkoutP95.toFixed(0)} ms</div></div>
    <div class="stat"><div>Fail rate</div><div class="value">${failRate.toFixed(2)}%</div></div>
  </div>
  <div class="card">
    <h2>Gráfico de metas</h2>
    <svg width="960" height="285" role="img" aria-label="Comparação das métricas de carga com metas">
      ${svgBars}
    </svg>
  </div>
</body>
</html>`;
}

function randomIntBetween(min, max) {
  return Math.floor(Math.random() * (max - min + 1)) + min;
}
