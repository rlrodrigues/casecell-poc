# ADR-008 - Testar carga com k6 e relatório HTML

## Status

Aceito

## Contexto

O desafio pede performance de vitrine e resiliência de checkout. Testes unitários validam regras, mas não mostram comportamento sob concorrência, latência percebida ou taxa de erro quando vitrine e checkout rodam juntos.

## Decisão

Adicionar k6 como teste de carga executável por Docker Compose. O cenário roda duas jornadas em paralelo:

- navegação na vitrine com `GET /products`;
- início de checkout com `POST /checkout`.

O teste gera `summary.json` para análise técnica e `summary.html` com gráfico SVG para leitura visual dos principais indicadores.

## Consequências

Pontos positivos:

- teste reproduzível localmente;
- evidencia p95, taxa de erro e aceite de checkout;
- usa a mesma API subida no Docker Compose;
- facilita discussão objetiva de performance.

Trade-offs:

- não substitui teste distribuído real;
- a base SQLite local não representa uma topologia produtiva;
- os limiares são iniciais e deveriam ser calibrados com SLOs reais.

## Implementação técnica

- `load-tests/casecellshop.js`
- `load-tests/results/summary.json`
- `load-tests/results/summary.html`
- `docker-compose.yml` com profile `loadtest`
