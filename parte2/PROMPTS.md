# PROMPTS.md

Este arquivo registra o prompt principal usado para orientar a construção da Parte 2 do desafio, alinhado aos ADRs do projeto.

## Prompt principal alinhado aos ADRs

```text
Você é um arquiteto de software sênior e engenheiro backend especialista em C#, ASP.NET Core, EF Core, mensageria, cache distribuído e resiliência.

Estou desenvolvendo a Parte 2 do desafio técnico CaseCellShop. Implemente uma API backend pequena, executável e bem documentada, respeitando obrigatoriamente as decisões arquiteturais abaixo:

ADR-001 - Usar C# com ASP.NET Core Minimal APIs
- Use .NET 8 e ASP.NET Core Minimal APIs.
- Mantenha os endpoints simples e mova regras de negócio para serviços de aplicação.
- Exponha os endpoints:
  - GET /products
  - POST /checkout
  - GET /orders/{orderId}/status

ADR-002 - Usar Redis para cache de catálogo
- Use Redis via IDistributedCache.
- GET /products deve usar cache com TTL.
- O cache pode melhorar a vitrine, mas nunca pode ser fonte de verdade para confirmar compra.
- Invalide ou atualize o cache quando houver mudança relevante em estoque/pedido.

ADR-003 - Usar EF Core com transação e reserva atômica de estoque
- Use EF Core com SQLite local para a mini-tarefa.
- Use transação explícita no checkout.
- Implemente reserva de estoque com update condicional atômico, evitando overselling.
- Não use lazy loading.
- Use AsNoTracking em consultas somente leitura.
- Não reutilize o mesmo DbContext em operações paralelas.
- Evite carregar entidades desnecessariamente para operações de atualização concorrente.

ADR-004 - Usar MassTransit EF Outbox para checkout assíncrono
- Use MassTransit com EF Outbox.
- Ao criar um pedido, grave o evento de domínio na mesma fronteira transacional.
- Evite pedido fantasma e mensagem fantasma.
- O consumidor deve ser idempotente.
- Use worker assíncrono para simular o faturamento no ERP.
- Use retry para falhas transientes.
- Para manter o desafio simples, o transporte pode ser in-memory, documentando RabbitMQ como evolução natural para produção.

ADR-005 - Observabilidade local com logs estruturados e OpenTelemetry
- Adicione logs estruturados para cache hit/miss, criação de pedido, reserva rejeitada, faturamento iniciado e faturamento concluído.
- Adicione traces com OpenTelemetry Console Exporter.
- Documente métricas, SLOs, alertas e runbooks no README.

ADR-006 - Separar projetos com Clean Architecture
- Separe a solução em projetos Domain, Application, Infrastructure e Api.
- Mantenha entidades, contratos, eventos e interfaces no Domain.
- Mantenha casos de uso na Application.
- Mantenha EF Core, Redis, MassTransit e ERP fake na Infrastructure.
- Mantenha endpoints e composição da aplicação na Api.

ADR-007 - Observabilidade com OpenTelemetry, Prometheus, Grafana e caminho para Datadog
- Use OpenTelemetry como camada neutra de instrumentação.
- Suba OpenTelemetry Collector, Prometheus e Grafana no Docker Compose.
- Provisione dashboard básico no Grafana.
- Documente como apontar OTLP para Datadog em ambiente real.

ADR-008 - Testar carga com k6 e relatório HTML
- Crie teste de carga com k6 para vitrine e checkout.
- Gere `summary.json` e `summary.html`.
- Inclua gráfico visual no HTML com p95, taxa de falha e aceite de checkout.
- Documente comando de execução e resultados no README.

Requisitos adicionais:
- Implemente idempotência no POST /checkout usando o header Idempotency-Key.
- Se a mesma chave chegar com o mesmo payload, retorne o mesmo pedido.
- Se a mesma chave chegar com payload diferente, retorne conflito.
- Crie Dockerfile e docker-compose.yml com Redis.
- Inclua Prometheus, Grafana, OpenTelemetry Collector e profile de k6 no Docker Compose.
- Crie testes automatizados com xUnit e FluentAssertions cobrindo:
  - cache de catálogo;
  - idempotência;
  - conflito de idempotência;
  - concorrência impedindo overselling.
- Crie README com instruções de execução, endpoints, decisões, trade-offs, testes, observabilidade, métricas, SLOs, alertas e runbooks.
- Crie PROMPTS.md registrando este prompt e os ajustes manuais.
- Crie ADRs em docs/architectural-decision-records seguindo um template simples.
- Crie um Architecture Haiku resumindo sistema, objetivos, restrições, atributos de qualidade e decisões.

Restrições:
- Não implemente autenticação, pagamento real, frontend ou integração real com ERP.
- Não use cache para decidir disponibilidade final no checkout.
- Não use padrões de EF Core que mascarem problemas de concorrência.
- Não dependa de serviços externos pagos.
- A solução deve ser pequena o suficiente para ser avaliada rapidamente, mas robusta o bastante para demonstrar raciocínio sênior.

Critério de aceite:
- dotnet build deve passar sem erros.
- dotnet test deve passar.
- A API deve rodar localmente e via Docker Compose.
- O teste de carga k6 deve executar e gerar relatório visual.
- O README deve explicar claramente as simplificações feitas por se tratar de desafio técnico.
```

## Por que este prompt está aderente aos ADRs

| ADR | Como o prompt garante aderência |
|---|---|
| ADR-001 | Define C#, .NET 8, Minimal APIs e separação entre endpoints e serviços. |
| ADR-002 | Exige Redis, TTL, invalidação e explicita que cache não confirma compra. |
| ADR-003 | Exige EF Core, transação explícita, update condicional e boas práticas de leitura/concorrência. |
| ADR-004 | Exige MassTransit EF Outbox, consumidor idempotente e processamento assíncrono do ERP. |
| ADR-005 | Exige logs estruturados, OpenTelemetry, métricas, SLOs, alertas e runbooks. |
| ADR-006 | Exige separação por projetos, com portas no Domain e implementações na Infrastructure. |
| ADR-007 | Exige stack local de observabilidade e mantém caminho de integração com Datadog via OTLP. |
| ADR-008 | Exige teste de carga k6 e relatório HTML com gráfico. |

## Como validei a saída

- Revisei a arquitetura contra as restrições do desafio: ERP simulado, cache Redis, checkout assíncrono e rastreabilidade por pedido.
- Mantive o cache fora da decisão final de estoque.
- Usei EF Core com transação explícita e `ExecuteUpdateAsync` condicional para evitar overselling.
- Usei MassTransit EF Outbox para registrar eventos junto com a transação do pedido.
- Rodei build e testes automatizados com FluentAssertions.

## Ajustes manuais relevantes

- Fixei os pacotes EF Core na linha 8.x para compatibilidade com `net8.0`.
- Usei SQLite como banco local para permitir transações reais sem exigir MySQL local no desafio.
- Usei transporte in-memory do MassTransit para manter a execução local simples; RabbitMQ é uma evolução direta para produção.
- Incluí `docs/openapi.yaml` para deixar o contrato disponível mesmo sem subir a aplicação.
- Refatorei para Clean Architecture com projetos separados.
- Incluí Prometheus, Grafana, OpenTelemetry Collector e k6 no Docker Compose.
- Aumentei o seed para 5.000 SKUs numéricos para teste de carga mais justo.
