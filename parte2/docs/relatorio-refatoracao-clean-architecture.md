# Relatório - Refatoração da Parte 2

## Objetivo

Refatorei a Parte 2 para sair de uma estrutura concentrada na API e seguir uma organização mais próxima de Clean Architecture, com separação clara entre domínio, aplicação, infraestrutura e borda HTTP. Também adicionei observabilidade local com OpenTelemetry, Prometheus e Grafana, além de teste de carga com k6 e relatório visual.

## O que foi alterado

### 1. Separação por camadas

Foram criados projetos separados:

- `CaseCellShop.Domain`: entidades, contratos, eventos e interfaces.
- `CaseCellShop.Application`: serviços de aplicação e orquestração dos casos de uso.
- `CaseCellShop.Infrastructure`: EF Core, Redis, MassTransit, ERP fake, seed e relógio.
- `CaseCellShop.Api`: endpoints, configuração de DI, health check, Swagger e observabilidade.

Com isso, as regras de checkout, catálogo e faturamento deixam de depender diretamente da API ou de detalhes de banco/cache.

### 2. Interfaces no domínio

As portas principais foram movidas para `CaseCellShop.Domain.Abstractions`, incluindo:

- repositório de catálogo;
- cache de catálogo;
- store de checkout;
- leitura de status;
- faturamento do ERP;
- publicação de eventos;
- transação de aplicação;
- métricas de aplicação;
- relógio.

Essa decisão permite testar os casos de uso e trocar infraestrutura sem mexer na regra de negócio.

### 3. EF Core com práticas mais seguras

A reserva de estoque continua sendo feita com `ExecuteUpdateAsync` condicional, usando `available >= quantity`. Isso evita o padrão inseguro de ler o saldo, decidir em memória e tentar salvar depois sob concorrência.

Também foram mantidos:

- transação explícita no checkout;
- `AsNoTracking` em leituras;
- idempotência por chave;
- controle de status do pedido;
- outbox MassTransit junto da transação.

### 4. Observabilidade

Foi adicionada uma stack local:

- OpenTelemetry Collector;
- Prometheus;
- Grafana;
- dashboard versionado no repositório.

A API exporta métricas, traces e logs via OpenTelemetry. Para Datadog, o caminho previsto é apontar `Observability__OtlpEndpoint` para um Datadog Agent/OTLP ou configurar o Collector com exporter Datadog.

URLs locais:

- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`
- Grafana: `http://localhost:3000` (`admin` / `admin`)
- Prometheus: `http://localhost:9090`

### 5. Massa de dados

O seed agora cria 5.000 SKUs numéricos, de `CASE-00001` até `CASE-05000`, além dos produtos demonstrativos. Isso torna a execução do k6 mais justa, porque o checkout distribui carga em vários produtos e reduz colisão artificial de estoque.

### 6. Teste de carga com k6

Foi criado o teste `load-tests/casecellshop.js` com dois cenários:

- vitrine com `GET /products`;
- checkout com `POST /checkout`.

O teste gera:

- `load-tests/results/summary.json`;
- `load-tests/results/summary.html`.

Resultado da execução validada:

```text
Total de requisições: 1661
Checks aprovados: 3322
Checks com falha: 0
HTTP fail rate: 0%
p95 geral: 46.09 ms
p95 GET /products: 48.02 ms
p95 POST /checkout: 22.59 ms
Checkout accepted rate: 100%
```

## ADRs criados

- ADR-006 - Separar projetos com Clean Architecture.
- ADR-007 - Observabilidade com OpenTelemetry, Prometheus, Grafana e caminho para Datadog.
- ADR-008 - Testar carga com k6 e relatório HTML.

## Comandos executados

```bash
dotnet build CaseCellShop.slnx --no-restore -m:1 -v minimal
dotnet test CaseCellShop.slnx --no-restore -m:1 -v minimal
docker compose up --build -d
docker compose --profile loadtest run --rm k6
```

## Trade-offs

- SQLite facilita execução local, mas eu não usaria como banco de produção para esse domínio.
- O transporte MassTransit está in-memory para simplificar o desafio; em produção, eu evoluiria para RabbitMQ.
- O dashboard Grafana é inicial; em produção, eu separaria painéis de API, banco, outbox, fila, ERP e SLOs.
- A integração com Datadog está preparada por OTLP, mas não foi ativada com credenciais reais para manter o projeto executável sem conta externa.
- No SDK .NET 10 local, o build paralelo falhou sem erro de compilação ao resolver projetos referenciados. A validação final foi feita com `-m:1`, que compila os projetos sequencialmente e passou sem avisos ou erros.
