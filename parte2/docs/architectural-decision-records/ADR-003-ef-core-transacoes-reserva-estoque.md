# ADR-003 - Usar EF Core com transação e reserva atômica de estoque

## Status

Aceito

## Contexto

Uma checagem simples de estoque permite corrida entre compradores. A solução precisa impedir overselling.

## Decisão

Usar EF Core com SQLite local e uma transação explícita no checkout. A reserva de estoque é feita por `ExecuteUpdateAsync` condicional.

## Consequências

- A reserva só acontece se `available >= quantity`.
- A criação do pedido, reserva e evento de outbox ficam na mesma fronteira transacional.
- Evita carregar entidades para alterar estoque, reduzindo janela de concorrência.
- SQLite é suficiente para o desafio local, mas produção poderia usar MySQL ou PostgreSQL.

## Implementação técnica

- `CheckoutService.StartCheckoutAsync`.
- `AppDbContext` com índices para SKU, status e idempotência.
- Consultas de leitura com `AsNoTracking`.
- Sem lazy loading.
- Sem reuso paralelo do mesmo `DbContext`.
