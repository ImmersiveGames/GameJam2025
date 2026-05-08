# ADR-0068 - SceneRouteProfile e rebaixamento de SceneFlow/Navigation para Pipeline Adapter

## Status
- Estado: Accepted
- Data: 2026-05-02
- Tipo: Direction / Canonical architecture
- Fonte de verdade canônica deste contrato: este ADR.

## Contexto

A auditoria de Navigation/Routes/SceneFlow mostrou que `routeKind` deixou de ser apenas classificação de rota e passou a influenciar decisões de várias camadas:

- gameplay vs non-gameplay;
- reset e world lifecycle;
- payload de entrada;
- input mode;
- actor set;
- save eligibility;
- loading e progress;
- observabilidade da fronteira de gameplay.

Isso faz `SceneFlow/Navigation` acumular responsabilidades que deveriam pertencer a `RouteProfile`, `NavigationIntent`, `SceneTransitionPayload` ou aos pipelines.

Base 1.1 exige que lifecycle, policy e handoff vivam nos pipelines. SceneFlow/Navigation deve executar side-effects e adaptar intenções, não decidir lifecycle por conta própria.

## Decisão

Adota-se o seguinte contrato arquitetural:

### 1. `RouteDefinition`
`RouteDefinition` descreve a topologia da rota:

- `routeId`;
- `scenesToLoad`;
- `scenesToUnload`;
- `targetActiveScene`.

### 2. `SceneRouteProfile`
`SceneRouteProfile` descreve capacidades e policies operacionais da rota:

- route classification;
- gameplay participation;
- world reset requirement;
- payload eligibility;
- input behavior;
- actor set behavior;
- save behavior;
- loading behavior.

### 3. `NavigationIntent`
`NavigationIntent` representa a intenção de navegação e apresentação.

### 4. `SceneTransitionPayload`
`SceneTransitionPayload` transporta dados operacionais mínimos. Ele não decide lifecycle.

### 5. Pipelines
Session Pipeline e Run Pipeline continuam donos de:

- lifecycle;
- pipeline policy;
- pipeline handoff.

### 6. SceneFlow / Navigation
`SceneFlow/Navigation` passam a ser tratados como `Pipeline Adapter`.

Eles executam side-effects, resolvem topologia, aplicam profile e disparam transições. Eles não devem permanecer como fonte central de policy.

### 7. `routeKind`
`routeKind` não deve continuar sendo a fonte central de policy.

Ele pode permanecer temporariamente como compatibilidade e histórico durante a migração.

`Overlay` pode permanecer temporariamente para o sandbox v0, mas não é a semântica final.

## Consequências

- Consumidores que hoje usam `routeKind` como policy devem migrar gradualmente para `SceneRouteProfile`.
- `GameNavigationService` não deve decidir lifecycle de gameplay por `routeKind`.
- `SceneFlowInputModeBridge` deve aplicar comando ou policy explícita, não inferir livremente por `routeKind`.
- `SceneFlowRouteActorSetRefService` deve ler capability/profile explícito.
- `SaveOrchestrationService` não deve inferir save apenas por `routeKind`.
- `LoadingProgressOrchestrator` deve usar loading behavior explícito vindo de profile.
- `Route_to-session-activity-sandbox` deve ganhar profile próprio em patch futuro.
- A compatibilidade com `routeKind` atual pode existir por um período de migração, mas não vira nova fonte de verdade.

## Invariantes

- Não criar Base 2.0 genérica.
- Não reorganizar fisicamente em Core/Concrete/Adapter.
- Não transformar o sandbox em gameplay paralelo.
- Não criar fallback silencioso para profile obrigatório.
- Rota sem profile obrigatório deve falhar explicitamente quando a migração tornar profile obrigatório.
- Pipeline decide lifecycle; SceneFlow/Navigation executa side-effects.
- foreign/stale events não podem alterar o pipeline ativo.
- `routeKind` não deve mais ser o ponto onde decisões transversais são inferidas por conveniência.

## Relação com Base 1.0 e Base 2.0

- Base 1.0 permitiu que a decisão de rota carregasse política demais.
- Base 1.1 formaliza a separação entre topologia, profile, intenção e lifecycle.
- Base 2.0 futura só deve nascer se essa separação continuar válida na prática.

## ADRs históricos relacionados

- `ADR-0060`
- `ADR-0061`
- `ADR-0062`
- `ADR-0063`
- `ADR-0064`
- `ADR-0065`
- `ADR-0066`
- `ADR-0067`
