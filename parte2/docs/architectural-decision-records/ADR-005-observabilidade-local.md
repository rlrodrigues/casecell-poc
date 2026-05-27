# ADR-005 - Observabilidade local com logs estruturados e OpenTelemetry

## Status

Aceito

## Contexto

O desafio pede logs, métricas, traces, SLOs, alertas e runbooks, mas não exige conta real em Datadog.

## Decisão

Instrumentar logs estruturados e traces com OpenTelemetry Console Exporter como stub local. Documentar métricas, alertas e runbooks no README.

## Consequências

- Permite validar correlação e rastreabilidade localmente.
- Mantém a solução simples para execução no desafio.
- Produção exigiria exportador OTLP/Datadog e dashboards reais.

## Implementação técnica

- `OpenTelemetry` configurado em `Program.cs`.
- `TraceSources` centraliza o `ActivitySource`.
- Logs incluem eventos como cache hit/miss, pedido criado, rejeição de estoque e faturamento.
