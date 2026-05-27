# ADR-002 - Usar Redis para cache de catálogo

## Status

Aceito

## Contexto

A vitrine não deve consultar o ERP ou o banco a cada acesso. O desafio pede cache e métricas de hit/miss.

## Decisão

Usar Redis através de `IDistributedCache` para cachear a resposta de `GET /products`.

## Consequências

- Reduz latência e pressão no banco da loja.
- Permite troca de implementação de cache em testes.
- O cache não é fonte de verdade para checkout.
- É necessário invalidar o cache após reserva e confirmação de estoque.

## Implementação técnica

- Configuração em `Program.cs` com `AddStackExchangeRedisCache`.
- Uso em `ProductCatalogService`.
- TTL com jitter entre 45 e 75 segundos.
- Invalidação após checkout e faturamento.
