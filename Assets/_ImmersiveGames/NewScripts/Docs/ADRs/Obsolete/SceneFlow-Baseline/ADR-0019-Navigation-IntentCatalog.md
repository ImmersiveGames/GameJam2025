> [!WARNING]
> **Obsoleto por supersedência.**
>
> Este ADR foi movido para histórico da baseline de SceneFlow/LevelFlow.
> Use os ADRs canônicos `ADR-0030` a `ADR-0033` para leitura operacional atual.
>
> Motivo: consolidação pós-baseline 0027 para reduzir leitura cruzada e ambiguidade de ownership.

# ADR-0019 — Navigation Catalog como asset canônico de navigation

## Status

- Estado: **Implementado**
- Data (decisão): **2026-02-16**
- Última atualização: **2026-03-25**
- Escopo: `Navigation` + wiring com `SceneRouteDefinitionAsset` / `TransitionStyleAsset`

## Contexto

A arquitetura atual de navigation/transition foi consolidada para um modelo de **direct-ref + fail-fast**. O sistema não deve depender de resolução nominal paralela para rotas e styles no trilho principal.

## Decisão canônica atual

### 1) `GameNavigationCatalogAsset` é o asset canônico de navigation

Ele continua sendo o owner da associação entre:
- `routeRef`
- `transitionStyleRef`

### 2) `TransitionStyleAsset` é o owner estrutural do style

Cada style carrega:
- `profileRef`
- `useFade`

`SceneTransitionProfile` permanece como asset leaf visual. O style não carrega semântica de fluxo.

### 3) `startup` não pertence ao catálogo normal de navigation

`startup` pertence ao bootstrap via `startupTransitionStyleRef` em `NewScriptsBootstrapConfigAsset`.

Ou seja:
- `startup` não passa pelo mesmo shape de intent/rota usado para `frontend` e `gameplay`;
- `frontend` e `gameplay` pertencem ao domínio de `SceneRouteKind`.

### 4) A semântica do fluxo fica fora de style/profile

Style/profile não definem:
- `RouteKind`;
- reset policy;
- seleção de level;
- intro/post;
- swap local.

Essas decisões pertencem a `SceneRouteDefinitionAsset`, `LevelFlow` e `GameLoop`.

## Consequências

### Positivas
- um único shape estrutural para navigation/transition;
- menos wiring por string;
- validação explícita e fail-fast para assets obrigatórios.

### Trade-offs
- exige disciplina para não reintroduzir ids paralelos como fonte de verdade;
- `startup` continua como exceção estrutural do bootstrap, por design.

## Relação com outros ADRs

- `ADR-0009`: envelope do fade no SceneFlow.
- `ADR-0018`: resiliência do fade/style.
- `ADR-0020`: route macro não carrega identidade de conteúdo/local.
- `ADR-0024`: `LevelCollection` por macro route.
