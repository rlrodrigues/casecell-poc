# CaseCellShop - Parte 2

API backend em C# para demonstrar cache de catálogo, checkout assíncrono, reserva transacional de estoque, idempotência, outbox e rastreabilidade.

## Stack

- .NET 8 / ASP.NET Core Minimal APIs
- EF Core 8 + SQLite
- Redis via `IDistributedCache`
- MassTransit + EF Outbox
- OpenAPI/Swagger
- OpenTelemetry Console Exporter
- xUnit + FluentAssertions
- Docker / Docker Compose

## Como rodar com Docker

```bash
docker compose up --build
```

A API ficará disponível em:

- `http://localhost:8080/products`
- `http://localhost:8080/swagger`
- `http://localhost:8080/health`

## Como rodar localmente

Suba um Redis local:

```bash
docker run --rm -p 6379:6379 redis:7-alpine
```

Depois execute:

```bash
dotnet restore CaseCellShop.slnx
dotnet run --project src/CaseCellShop.Api/CaseCellShop.Api.csproj
```

## Endpoints

O contrato está disponível em runtime via Swagger (`/swagger`) e também como arquivo estático em `docs/openapi.yaml`.

### GET /products

Retorna catálogo com disponibilidade exibida. Usa Redis com TTL e fallback para o banco da loja.

```bash
curl http://localhost:8080/products
```

### POST /checkout

Cria pedido, reserva estoque e retorna `202 Accepted`. O faturamento no ERP é processado em segundo plano.

```bash
curl -X POST http://localhost:8080/checkout \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: pedido-001" \
  -d "{\"items\":[{\"sku\":\"CASE-IPHONE-15-BLACK\",\"quantity\":1}]}"
```

Resposta esperada:

```json
{
  "orderId": "00000000-0000-0000-0000-000000000000",
  "status": "PendingBilling"
}
```

### GET /orders/{orderId}/status

Consulta o status do pedido.

```bash
curl http://localhost:8080/orders/{orderId}/status
```

Status possíveis:

- `PendingBilling`
- `Billing`
- `Billed`
- `BillingFailed`

## Decisões e trade-offs

### Cache

O Redis acelera a vitrine, mas não é fonte de verdade para checkout. A disponibilidade exibida pode estar levemente defasada; a compra só é confirmada se a reserva transacional no banco for bem-sucedida.

### Estoque

A reserva usa `ExecuteUpdateAsync` com condição `available >= quantity`. Isso evita o padrão inseguro de ler saldo, decidir em memória e salvar depois.

### Idempotência

O header `Idempotency-Key` é obrigatório no checkout. Se a mesma chave chegar com o mesmo payload, a API retorna o mesmo pedido. Se chegar com payload diferente, retorna conflito.

### Outbox

O MassTransit EF Outbox grava o evento junto com a transação do pedido. Isso reduz o risco de pedido sem mensagem ou mensagem sem pedido. Para manter a execução local simples, o transporte configurado é in-memory; em produção, a troca natural seria RabbitMQ.

### ERP simulado

O ERP é representado por `FakeErpBillingClient`, com atraso configurável por `Erp:BillingDelayMs`.

## Testes

```bash
dotnet test CaseCellShop.slnx
```

Cobertura implementada:

- cache de catálogo;
- idempotência com mesma chave e payload;
- rejeição de mesma chave com payload diferente;
- concorrência para impedir overselling.

Resultado validado:

```text
Aprovado: 4, Com falha: 0
```

## Observabilidade

Logs estruturados cobrem:

- cache hit/miss;
- pedido criado;
- rejeição de reserva de estoque;
- faturamento iniciado;
- faturamento concluído;
- mensagem sem pedido correspondente.

Traces locais são emitidos via OpenTelemetry Console Exporter para:

- `products.list`;
- `checkout.start`;
- `orders.status`;
- instrumentação ASP.NET Core;
- chamadas HTTP futuras.

## Métricas sugeridas para Datadog ou equivalente

Counters:

- `products_requests_total`
- `cache_hits_total`
- `cache_misses_total`
- `checkout_requests_total`
- `orders_created_total`
- `stock_reservation_rejected_total`
- `erp_billing_success_total`
- `erp_billing_failure_total`
- `dlq_messages_total`

Gauges:

- `queue_depth`
- `orders_pending_billing_count`
- `stock_available`
- `stock_reserved`
- `outbox_pending_count`

Histograms:

- `http_request_duration_ms`
- `cache_get_duration_ms`
- `db_query_duration_ms`
- `checkout_processing_duration_ms`
- `erp_billing_duration_ms`
- `message_processing_duration_ms`

## SLOs e alertas

SLOs iniciais:

- `GET /products` com 99,9% de disponibilidade.
- p95 de `GET /products` menor que 200 ms com cache quente.
- `POST /checkout` com 99,5% de disponibilidade.
- p95 do aceite do checkout menor que 500 ms.
- 99% dos pedidos faturados em até 5 minutos.

Alertas:

- queda de cache hit ratio;
- crescimento de pedidos em `PendingBilling`;
- aumento de rejeição de reserva para produtos exibidos como disponíveis;
- falhas de faturamento no ERP;
- mensagens presas na outbox;
- mensagens em DLQ, quando houver transporte externo.

## Runbooks

### Redis indisponível

1. Verificar conectividade com Redis.
2. Conferir logs de `catalog_cache_miss`.
3. Confirmar se a API está respondendo pelo banco.
4. Reduzir tráfego ou aumentar réplicas se o banco ficar pressionado.

### ERP lento

1. Conferir pedidos em `PendingBilling` e `Billing`.
2. Acompanhar retries do worker.
3. Manter aceite de pedido se a reserva local estiver saudável.
4. Comunicar atraso de faturamento se o SLO for rompido.

### Suspeita de overselling

1. Consultar `stock_available` e `stock_reserved`.
2. Verificar pedidos concorrentes para o mesmo SKU.
3. Confirmar se houve falha no `ExecuteUpdateAsync` condicional.
4. Rodar conciliação entre pedidos, reservas e estoque.

## ADRs

Os ADRs estão em `docs/architectural-decision-records`:

- ADR-001 - Usar C# com ASP.NET Core Minimal APIs
- ADR-002 - Usar Redis para cache de catálogo
- ADR-003 - Usar EF Core com transação e reserva atômica de estoque
- ADR-004 - Usar MassTransit EF Outbox para checkout assíncrono
- ADR-005 - Observabilidade local com logs estruturados e OpenTelemetry

## Limitações

- SQLite foi escolhido para execução local. Em produção, eu avaliaria MySQL ou PostgreSQL.
- O ERP é simulado.
- O transporte do MassTransit é in-memory para simplificar o desafio.
- Não há autenticação, pagamento real ou frontend, porque estão fora do escopo.
