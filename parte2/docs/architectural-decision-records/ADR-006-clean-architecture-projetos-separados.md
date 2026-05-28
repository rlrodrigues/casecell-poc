# ADR-006 - Separar projetos com Clean Architecture

## Status

Aceito

## Contexto

A primeira versão resolvia o fluxo, mas deixava API, regras de aplicação, contratos, banco, cache e mensageria próximos demais. Isso dificulta leitura, testes e evolução, principalmente em um cenário de checkout assíncrono, estoque consistente e integrações externas.

## Decisão

Organizar a solução em projetos separados seguindo Clean Architecture:

- `CaseCellShop.Domain`: entidades, contratos, eventos e interfaces.
- `CaseCellShop.Application`: casos de uso e regras de aplicação.
- `CaseCellShop.Infrastructure`: EF Core, Redis, MassTransit, ERP fake e implementações de portas.
- `CaseCellShop.Api`: endpoints, DI, health check e observabilidade.

As interfaces ficam no Domain para representar portas da regra de negócio. A Infrastructure depende do Domain para implementar essas portas, e a Application depende apenas das abstrações.

## Consequências

Pontos positivos:

- regra de negócio mais isolada;
- testes mais diretos;
- dependências externas trocáveis;
- menos acoplamento entre endpoint e persistência.

Trade-offs:

- mais projetos e mais arquivos;
- mais configuração de DI;
- pode parecer excessivo para um desafio pequeno, mas deixa o desenho mais próximo de um sistema real.

## Implementação técnica

- `src/CaseCellShop.Domain`
- `src/CaseCellShop.Application`
- `src/CaseCellShop.Infrastructure`
- `src/CaseCellShop.Api`
- `tests/CaseCellShop.Tests`
