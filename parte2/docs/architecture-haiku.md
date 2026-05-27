# Architecture Haiku - CaseCellShop Parte 2

## 1. Descritivo do sistema

A solução é uma API backend para a CaseCellShop que expõe catálogo de produtos com cache Redis, inicia checkout assíncrono com reserva transacional de estoque e permite consultar o status do pedido. O ERP é simulado como uma dependência lenta de faturamento, processada em segundo plano para preservar a experiência do cliente e manter rastreabilidade.

## 2. Objetivos de negócio

- Reduzir latência da vitrine.
- Impedir venda acima do estoque disponível.
- Tolerar timeout e retry no faturamento do ERP.
- Permitir acompanhamento do pedido por status.
- Demonstrar decisões arquiteturais, observabilidade e uso responsável de IA.

## 3. Restrições identificadas

- O ERP real não deve ser alterado.
- A solução deve ser pequena, executável e adequada a um desafio técnico.
- Não há necessidade de autenticação, pagamento real, frontend ou deploy real.
- Redis deve ser usado para cache.
- EF Core deve ser usado com transações e sem más práticas comuns, como lazy loading implícito, consultas rastreadas desnecessárias ou concorrência no mesmo `DbContext`.

## 4. Atributos de qualidade priorizados

Consistência > resiliência > observabilidade > performance > simplicidade operacional

Consistência vem primeiro porque overselling é o risco de negócio mais sensível. Resiliência vem em seguida porque o faturamento no ERP é lento e não pode derrubar a jornada. Observabilidade é necessária para operar o fluxo assíncrono. Performance é tratada com Redis e read model. Simplicidade operacional fecha a lista porque a solução precisa ser executável localmente e fácil de avaliar.

## 5. Decisões de design

- C# / ASP.NET Core Minimal APIs para reduzir boilerplate e manter a entrega objetiva.
- EF Core + SQLite para transações reais no desafio local.
- Redis via `IDistributedCache` para cache de catálogo.
- Reserva de estoque com `UPDATE` condicional usando `ExecuteUpdateAsync`.
- Idempotência por header `Idempotency-Key`.
- MassTransit EF Outbox para gravar evento de pedido criado junto com a transação.
- Worker MassTransit para simular faturamento assíncrono no ERP.
- OpenAPI/Swagger para contrato.
- Logs estruturados e OpenTelemetry Console Exporter como stub local de traces.
