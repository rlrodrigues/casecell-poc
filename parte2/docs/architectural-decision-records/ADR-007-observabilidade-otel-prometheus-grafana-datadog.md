# ADR-007 - Observabilidade com OpenTelemetry, Prometheus, Grafana e caminho para Datadog

## Status

Aceito

## Contexto

O checkout passa por etapas assíncronas: criação do pedido, reserva de estoque, publicação do evento, faturamento no ERP e atualização do status. Sem logs, métricas e traces, uma falha nesse caminho fica difícil de explicar e recuperar.

Datadog é uma boa opção de mercado, mas exige conta, agente e credenciais. Para o desafio técnico, a solução precisa ser executável localmente sem dependência paga.

## Decisão

Usar OpenTelemetry como camada neutra de instrumentação, com Collector local, Prometheus para métricas e Grafana para visualização. A integração com Datadog fica viável apontando o endpoint OTLP para um Datadog Agent ou configurando o Collector com exporter Datadog.

## Consequências

Pontos positivos:

- stack gratuita e executável via Docker Compose;
- baixo acoplamento com fornecedor;
- caminho claro para Datadog em produção;
- dashboards versionados no repositório.

Trade-offs:

- o dashboard local é simples;
- logs ainda são exportados para console/debug no ambiente local;
- alertas reais exigiriam Alertmanager, Grafana Alerting ou Datadog Monitors em produção.

## Implementação técnica

- `observability/otel-collector.yaml`
- `observability/prometheus.yml`
- `observability/grafana/dashboards/casecellshop-overview.json`
- `src/CaseCellShop.Api/Observability/ApplicationMetrics.cs`
- `src/CaseCellShop.Api/Program.cs`
