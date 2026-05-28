# Teste Técnico - CaseCellShop

Este repositório está organizado em duas partes principais do desafio técnico.

## Parte 1 - Respostas conceituais e arquitetura

A Parte 1 está em:

- [parte1/README.md](parte1/README.md)

Ela contém a análise conceitual dos três problemas do case:

- performance da vitrine;
- consistência de estoque;
- resiliência do checkout.

Também inclui os desenhos arquiteturais em:

- [parte1/assets](parte1/assets)

Os principais diagramas são:

- arquitetura pragmática para 30 a 90 dias;
- arquitetura orientada a domínios;
- fluxo da vitrine;
- fluxo do checkout;
- ciclo de vida da reserva de estoque.

## Parte 2 - Implementação backend

A Parte 2 está em:

- [parte2/README.md](parte2/README.md)

Ela contém uma API backend em C#/.NET com:

- Clean Architecture;
- Redis para cache;
- EF Core para transações e reserva de estoque;
- MassTransit com outbox;
- checkout assíncrono;
- idempotência;
- observabilidade com OpenTelemetry, Prometheus e Grafana;
- testes automatizados com xUnit e FluentAssertions;
- testes de carga com k6.

## Documentação da Parte 2

Arquivos importantes:

- [Relatório da refatoração](parte2/docs/relatorio-refatoracao-clean-architecture.md)
- [Architecture Haiku](parte2/docs/architecture-haiku.md)
- [OpenAPI](parte2/docs/openapi.yaml)
- [ADRs](parte2/docs/architectural-decision-records)
- [Prompts utilizados](parte2/PROMPTS.md)

## Testes de carga

O teste k6 está em:

- [parte2/load-tests/casecellshop.js](parte2/load-tests/casecellshop.js)

Resultados gerados:

- [summary.html](parte2/load-tests/results/summary.html)
- [summary.json](parte2/load-tests/results/summary.json)

## Como executar a Parte 2

Entre na pasta `parte2`:

```bash
cd parte2
docker compose up --build
```

Serviços principais:

- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`
- Health check: `http://localhost:8080/health`
- Grafana: `http://localhost:3000`
- Prometheus: `http://localhost:9090`

Para rodar os testes:

```bash
dotnet test CaseCellShop.slnx -m:1
```

Para rodar o k6:

```bash
docker compose --profile loadtest run --rm k6
```
