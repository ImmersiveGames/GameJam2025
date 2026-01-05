# WorldLifecycle Reset — Status e Pendências (macro-estruturas)

## Atualização (2026-01-05)

- **Ownership de `WorldLifecycleResetCompletedEvent` formalizado (canônico):**
  - **Publisher (produção):** `WorldLifecycleRuntimeCoordinator`.
  - **Consumidores (produção):** `WorldLifecycleResetCompletionGate` (completion gate do SceneFlow) e `GameLoopSceneFlowCoordinator` (sync do GameLoop).
  - **Consumidores (dev/QA):** `BaselineInvariantAsserter` (opt-in) e QAs (ex.: `WorldLifecycleMultiActorSpawnQa`).
- **Production trigger do reset consolidado:** `SceneTransitionScenesReadyEvent` → (profile gameplay) hard reset → `WorldLifecycleResetCompletedEvent`; (startup/frontend) SKIP + `WorldLifecycleResetCompletedEvent`.
- **Ownership de limpeza (Global vs Scene vs Object) consolidado:** ver seção "Ownership e limpeza de serviços" em `Docs/WORLD_LIFECYCLE.md`; ajustes de runtime: `SceneServiceCleaner` implementa `IDisposable` e só loga limpeza quando realmente remove entradas (via `SceneServiceRegistry.TryClear`).

## Atualização (2026-01-03)

- **Assinatura canônica** corrigida na documentação:
  - `contextSignature` é **`SceneTransitionContext.ContextSignature`**.
  - `SceneTransitionSignatureUtil.Compute(context)` retorna exatamente essa assinatura.
  - `SceneTransitionContext.ToString()` é apenas **debug/log**.
- **Ordem de Loading HUD** alinhada com o runtime:
  - **UseFade=true:** `FadeInCompleted → Show` e `BeforeFadeOut → Hide` (com safety hide no `Completed`).
  - **UseFade=false:** `Started → Show` e `BeforeFadeOut → Hide` (com safety hide no `Completed`).
- **Targets de GameplayReset** confirmados com QA/Reports:
  - `AllActorsInScene`, `PlayersOnly`, `EaterOnly`, `ActorIdSet`, `ByActorKind`.
  - Evidências: [QA-GameplayReset-RequestMatrix](Reports/QA-GameplayReset-RequestMatrix.md) e [QA-GameplayResetKind](Reports/QA-GameplayResetKind.md).

## Atualização (2025-12-30)

### Confirmado em runtime
- Fluxo de **produção** validado end-to-end: Startup → Menu → Gameplay via SceneFlow + Fade + Loading HUD + Navigation.
- `WorldLifecycleRuntimeCoordinator` reage a `SceneTransitionScenesReadyEvent`:
    - Profile `startup`/frontend: reset **skip** + emite `WorldLifecycleResetCompletedEvent`.
    - Profile `gameplay`: dispara **hard reset** (`ResetWorldAsync`) e emite `WorldLifecycleResetCompletedEvent` ao concluir.
- `SceneTransitionService` aguarda `WorldLifecycleResetCompletionGate` antes do FadeOut.
- Hard reset em Gameplay confirma spawn via `WorldDefinition` (2 entries: Player + Eater) e execução do orchestrator com Gate (`WorldLifecycle.WorldReset`).
- `IStateDependentService` bloqueia input/movimento durante gate e libera ao final; pausa também fecha gate via `GamePauseGateBridge`.

### Pendências remanescentes (curto prazo)
- Fixar blockers restantes da `GameplayScene` fora do pipeline de reset (erros de gameplay específicos).

Data: 2025-12-26
Escopo: NewScripts — WorldLifecycle + Gameplay/Reset

## O que está funcional (confirmado por logs)
### 1) Macro-fluxo do WorldLifecycle Reset
O pipeline “grande” de reset está operacional e resiliente mesmo quando **não há spawn services**:
- Gate fecha/abre durante o reset (`WorldLifecycle.WorldReset`).
- Ordem de fases do orchestrator executa sem falhas:
    - `OnBeforeDespawn` (hooks) → `Despawn` → `Scoped reset participants` → `OnBeforeSpawn` → `Spawn` → `OnAfterSpawn`
- Quando não existem spawn services, `Despawn/Spawn` são **skip** com warning, mas o reset segue (hooks + scoped participants).

### 2) Macro “WorldLifecycle → Gameplay Reset (targets/grupos)”
O bridge está estável e validado para **PlayersOnly**:
- `IResetScopeParticipant` (WorldLifecycle) → `PlayersResetParticipant` → `IGameplayResetOrchestrator`
- QA dedicado validou execução real por fases:
    - `GameplayResetQaSpawner` spawnou players QA
    - `GameplayResetOrchestrator` resolveu `targets=2`
    - `GameplayResetQaProbe` recebeu `Cleanup/Restore/Rebind` por target

## Status macro (escala 0–100, aproximado)
Estimativa **qualitativa** baseada nos logs e nos docs atuais (não é métrica de performance).

- **Boot → Menu:** ~80
  Pipeline observado e estável, com SKIP no startup/menu.
- **SceneFlow:** ~85
  Fluxo `Started → ScenesReady → gate → BeforeFadeOut → Completed` confirmado.
- **Fade:** ~85
  Cena aditiva com ordenação e integração com SceneFlow.
- **LoadingHud:** ~80
  HUD integrado ao SceneFlow e respeitando o gate.
- **Gate/Readiness:** ~85
  Tokens de transição/pausa aparecem nos logs (pausa confirmada, transição ainda sem evidência dedicada).
- **WorldLifecycle:** ~80
  Reset por escopos com hooks/participants e emissão de `ResetCompleted`.
- **GameplayScene:** ~70
  Reset e spawn funcional no gameplay, com cobertura ampliada de targets.
- **Addressables (planejado):** ~0
  Ainda não implementado; apenas diretrizes em documentação.

## Targets de GameplayReset — status atual (QA/Logs)

| Target | Status | Evidência |
|---|---|---|
| `AllActorsInScene` | **Confirmado** | [QA-GameplayReset-RequestMatrix](Reports/QA-GameplayReset-RequestMatrix.md) |
| `PlayersOnly` | **Confirmado** | [QA-GameplayReset-RequestMatrix](Reports/QA-GameplayReset-RequestMatrix.md) + [GameplayReset-QA.md](QA/GameplayReset-QA.md) |
| `EaterOnly` | **Confirmado** | [QA-GameplayReset-RequestMatrix](Reports/QA-GameplayReset-RequestMatrix.md) + [QA-GameplayResetKind](Reports/QA-GameplayResetKind.md) |
| `ActorIdSet` | **Confirmado** | [QA-GameplayReset-RequestMatrix](Reports/QA-GameplayReset-RequestMatrix.md) |
| `ByActorKind` | **Confirmado** | [QA-GameplayReset-RequestMatrix](Reports/QA-GameplayReset-RequestMatrix.md) + [QA-GameplayResetKind](Reports/QA-GameplayResetKind.md) |

## O que ainda falta (provável parcial / não confirmado)

### 1) Evidência dedicada para `flow.scene_transition` (Readiness)
- O token é **implementado** via `GameReadinessService`, mas ainda não possui report/log dedicado confirmando sua presença no runtime.

### 2) Evidência dedicada da ordem completa dos hooks
- A ordem completa dos hooks (`OnBeforeDespawn → Despawn → Scoped Participants → OnBeforeSpawn → Spawn → OnAfterSpawn`) é **implementada**,
  porém ainda não está registrada em logs dedicados nos reports atuais.

## Artefatos relacionados (onde olhar primeiro)
- Logs confirmando:
    - `WorldLifecycleController` hard reset (Gameplay) com gate + fases.
    - `GameplayResetQaSpawner` + `GameplayResetOrchestrator` + `GameplayResetQaProbe` para PlayersOnly.
- Código-chave a revisar para “fechar 100%”:
    - `IGameplayResetTargetClassifier` (implementação concreta)
    - `GameplayResetOrchestrator` (implementação concreta)
