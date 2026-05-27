# ADR-001 - Usar C# com ASP.NET Core Minimal APIs

## Status

Aceito

## Contexto

O desafio pede uma entrega backend pequena, executável e com foco em cache, concorrência, resiliência e observabilidade. A stack solicitada nesta implementação é C#.

## Decisão

Usar C# com ASP.NET Core Minimal APIs em `net8.0`.

## Consequências

- Reduz boilerplate para a mini-tarefa.
- Mantém OpenAPI/Swagger simples.
- Facilita testes de serviços de aplicação sem exigir controllers.
- Exige disciplina para não concentrar regra de negócio no `Program.cs`; por isso, a lógica foi movida para serviços em `Services/`.

## Implementação técnica

- API em `src/CaseCellShop.Api`.
- Endpoints em `Program.cs`.
- Regras em `CheckoutService` e `ProductCatalogService`.
- Contratos em `Contracts/`.
