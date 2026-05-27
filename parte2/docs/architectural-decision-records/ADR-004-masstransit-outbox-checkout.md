# ADR-004 - Usar MassTransit EF Outbox para checkout assíncrono

## Status

Aceito

## Contexto

O pedido não pode depender do tempo de resposta do ERP. Ao mesmo tempo, publicar mensagem fora da transação pode criar pedido sem mensagem ou mensagem sem pedido.

## Decisão

Usar MassTransit com EF Outbox e Bus Outbox. O evento `OrderCreated` é publicado dentro do fluxo do checkout, mas persistido junto com a transação do EF Core.

## Consequências

- Evita pedido fantasma e mensagem perdida.
- Permite retry e consumo idempotente.
- Mantém a execução local simples com transporte in-memory.
- Em produção, o transporte pode ser trocado para RabbitMQ sem mudar a regra de negócio.

## Implementação técnica

- `AddEntityFrameworkOutbox<AppDbContext>` em `Program.cs`.
- Entidades de outbox adicionadas no `AppDbContext`.
- `OrderCreatedConsumer` processa faturamento simulado.
- `MassTransitEventPublisher` encapsula `IPublishEndpoint`.
