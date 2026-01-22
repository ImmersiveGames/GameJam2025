# ADR-0017 — Tipos de troca de conteúdo (ContentSwap: In-Place vs SceneTransition)

## Status
- Estado: Implementado
- Data: 2025-12-24
- Implementado em: 2026-01-18
- Escopo: ContentSwap (ContentSwapChangeService) + SceneFlow (NewScripts)

## Contexto

O sistema de ContentSwap precisa suportar dois tipos de troca com objetivos distintos:

- **Troca dentro do gameplay** (ex.: “conteúdo 1 concluído, inicia conteúdo 2 na mesma rodada”), sem descarregar a cena base.
- **Troca com transição completa** (ex.: novo conteúdo exige troca de cenas, unload de conteúdos atuais, loading e fade).

A meta é eliminar ambiguidades (ex.: “troca de fase” significar tanto reset in-place quanto scene transition), mantendo rastreabilidade por `contextSignature/reason` e evitando regressões do baseline.

### Baseline 2.0 (Opção B) / Escopo

- **IntroStage não é exigido pelo baseline** atual, pois não há evidência no smoke log vigente.
- A validação de IntroStage será feita em smoke separado (Baseline 2.1 ou “IntroStage smoke”) quando o fluxo estiver promovido.

## Decisão

Existem **dois tipos explícitos** de ContentSwap, com APIs e contratos distintos (nomes reais do código):

### 1) ContentSwap/In-Place

**Quando usar:** o conteúdo muda dentro da mesma “rodada”/cena (sem unload/load de cena).

**API (overloads canônicos):**

- `ContentSwapChangeService.RequestContentSwapInPlaceAsync(ContentSwapPlan plan, string reason)`
- `ContentSwapChangeService.RequestContentSwapInPlaceAsync(string contentId, string reason, ContentSwapOptions? options = null)`
- `ContentSwapChangeService.RequestContentSwapInPlaceAsync(ContentSwapPlan plan, string reason, ContentSwapOptions? options)`

**Contrato operacional:**

- Executa reset determinístico **sem SceneFlow**.
- **Sem Loading HUD** (mesmo se solicitado via options, o serviço ignora por design).
- **Fade opcional** (via `options.UseFade=true`) permitido como “mini transição” quando for desejável esconder reconstrução do reset.
- Gate/serialização: token `flow.contentswap_inplace`.
- Timeout: `options.TimeoutMs`.

### 2) ContentSwap/SceneTransition

**Quando usar:** o novo conteúdo exige transição completa (cenas, recursos pesados, feedback visual de loading, etc.).

**API (overloads canônicos):**

- `ContentSwapChangeService.RequestContentSwapWithTransitionAsync(ContentSwapPlan plan, SceneTransitionRequest transition, string reason)`
- `ContentSwapChangeService.RequestContentSwapWithTransitionAsync(string contentId, SceneTransitionRequest transition, string reason, ContentSwapOptions? options = null)`
- `ContentSwapChangeService.RequestContentSwapWithTransitionAsync(ContentSwapPlan plan, SceneTransitionRequest transition, string reason, ContentSwapOptions? options)`

**Contrato operacional:**

- Registra intent (`ContentSwapTransitionIntentRegistry`) e inicia **SceneFlow** via `ISceneTransitionService.TransitionAsync(transition)`.
- O `WorldLifecycleSceneFlowResetDriver` dispara o reset em `SceneTransitionScenesReadyEvent` (profile gameplay). O consumo do intent e o commit do conteúdo ocorrem após `WorldLifecycleResetCompletedEvent` via `ContentSwapTransitionIntentWorldLifecycleBridge`.
- Gate de simulação durante a transição é governado pelo token padrão do SceneFlow (`flow.scene_transition`).
- Fade/HUD são controlados pelo **profile do `SceneTransitionRequest`** (não por `ContentSwapOptions`).
- Timeout: `options.TimeoutMs`.

## Fora de escopo

- (não informado)

## Consequências

### Benefícios

- Nomenclatura elimina ambiguidade de “troca de conteúdo”.
- In-Place atende casos de “próximo conteúdo no mesmo gameplay” sem custo de trocar cenas.
- SceneTransition atende casos de “novo conteúdo exige troca de conteúdo pesado” com feedback visual e reset canônico.

### Trade-offs / Riscos

- In-Place não deve depender de loading HUD; quando precisar de experiência completa de loading, usar SceneTransition.
- Permitir `UseFade` no In-Place adiciona flexibilidade, mas exige disciplina de uso (para não virar “transição completa disfarçada”).

## Notas de implementação

### Termos e tipos (nomes reais do código)

- `ContentSwapChangeService` (serviço de ContentSwap, nome legado preservado)
- `ContentSwapPlan`
  - `ContentId` (identificador lógico do conteúdo)
  - `ContentSignature` (assinatura rastreável do “conteúdo montado”)
- `ContentSwapContextService`
  - `Current` / `Pending`
  - `TryCommitPending(...)`
- `ContentSwapTransitionIntentRegistry`
  - `Register(...)` / `TryConsume(...)`
- `ContentSwapOptions`
  - `UseFade`, `UseLoadingHud`, `TimeoutMs`
- `SceneTransitionRequest`
  - representa a solicitação do SceneFlow (load/unload/active/profile/useFade/contextSignature)

### Diagramas (sequência)

#### In-Place

```mermaid
sequenceDiagram
    participant Caller as Caller
    participant ContentSwap as ContentSwapChangeService
    participant WL as WorldLifecycleSceneFlowResetDriver
    participant WLC as WorldLifecycleController
    participant ContentSwapCtx as ContentSwapContextService

    Caller->>ContentSwap: RequestContentSwapInPlaceAsync(plan, reason, options)
    ContentSwap->>ContentSwapCtx: SetPending(plan, reason+signature)
    ContentSwap->>WL: ResetAsync(sourceSignature="contentswap.inplace:<ContentId>")
    WL->>WLC: Reset pipeline (despawn/spawn/hooks)
    WLC-->>WL: ResetCompleted
    WL->>ContentSwapCtx: TryCommitPending(...)
    ContentSwapCtx-->>ContentSwap: Pending committed (Current=plan)

    Note over ContentSwap: Contrato: sem Loading HUD; Fade opcional (UseFade)
    Note over ContentSwap: Sem SceneFlow (não há unload/load de cenas)
```

#### SceneTransition

```mermaid
sequenceDiagram
    participant Caller as Caller
    participant ContentSwap as ContentSwapChangeService
    participant Intent as ContentSwapTransitionIntentRegistry
    participant SceneFlow as SceneTransitionService
    participant Readiness as GameReadinessService
    participant WL as WorldLifecycleSceneFlowResetDriver
    participant ContentSwapCtx as ContentSwapContextService
    participant WLC as WorldLifecycleController

    Caller->>ContentSwap: RequestContentSwapWithTransitionAsync(plan, transition, reason, options)
    ContentSwap->>Intent: Register(signature, plan, reason)
    ContentSwap->>SceneFlow: TransitionAsync(transition)
    SceneFlow-->>Readiness: SceneTransitionStarted
    Readiness->>Readiness: Acquire gate token 'flow.scene_transition'
    SceneFlow-->>WL: SceneTransitionScenesReadyEvent(signature)
    WL->>WLC: Reset pipeline
    WLC-->>WL: WorldLifecycleResetCompletedEvent
    WL->>Intent: (bridge) TryConsume(signature)
    WL->>ContentSwapCtx: (bridge) SetPending(plan, reason+signature)
    WL->>ContentSwapCtx: (bridge) TryCommitPending(...)
    ContentSwapCtx-->>WL: Pending committed (Current=plan)
    SceneFlow-->>Readiness: SceneTransitionCompleted
    Readiness->>Readiness: Release gate token 'flow.scene_transition'

    Note over SceneFlow: Fade/Loading HUD são controlados pelo profile do SceneFlow
```

## Evidências

- Metodologia: [`Reports/Evidence/README.md`](../Reports/Evidence/README.md)
- Evidência canônica (LATEST): [`Reports/Evidence/LATEST.md`](../Reports/Evidence/LATEST.md)
- Snapshot (2026-01-18): [`Baseline-2.1-Evidence-2026-01-18.md`](../Reports/Evidence/2026-01-18/Baseline-2.1-Evidence-2026-01-18.md)
- ADR-0017 Evidence (2026-01-18): [`ADR-0017-Evidence-2026-01-18.md`](../Reports/Evidence/2026-01-18/ADR-0017-Evidence-2026-01-18.md)
- Contrato: [`Observability-Contract.md`](../Reports/Observability-Contract.md)

## Referências

- [ADR-0016 — ContentSwap + modos de avanço + IntroStage opcional](ADR-0016-ContentSwap-WorldLifecycle.md)
- [ADR-0018 — Mudança de semântica: ContentSwap + introdução do LevelManager](ADR-0018-Gate-de-Promoção-Baseline2.2.md)
- [WORLD_LIFECYCLE.md](../WORLD_LIFECYCLE.md)
- [Observability-Contract.md](../Reports/Observability-Contract.md) — contrato canônico de reasons, campos mínimos e invariantes
