# Checklist (Validação / Baseline)

Este checklist é o “contrato” verificável (logs + invariantes) do pipeline **SceneFlow + WorldLifecycle + GameLoop**.

## Evidência de referência

Log base usado para esta atualização: boot → startup/menu → gameplay → pause/resume → defeat/victory → restart → exit-to-menu.

Assinaturas observadas (representativas):

- Startup/Menu:
  - `p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap`
- Menu → Gameplay:
  - `p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene`
- Gameplay → Menu (ExitToMenu):
  - `p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene`

## A) Infra global (boot)

- [x] `GlobalBootstrap` inicializa logging + DI global.
- [x] EventBus (GameLoop + SceneFlow + WorldLifecycle) inicializado.
- [x] Serviços globais registrados (mínimo):
  - [x] `IUniqueIdFactory`
  - [x] `ISimulationGateService`
  - [x] `IGameLoopService` + `GameLoopEventInputBridge`
  - [x] `IGameRunEndRequestService`
  - [x] `IGameRunStatusService`
  - [x] `IGameRunOutcomeService` + `GameRunOutcomeEventInputBridge`
  - [x] `WorldLifecycleRuntimeCoordinator` (driver de `SceneTransitionScenesReadyEvent`)
  - [x] `INewScriptsFadeService` (ADR-0009)
  - [x] `INewScriptsLoadingHudService` + `SceneFlowLoadingService`
  - [x] `ISceneTransitionCompletionGate` = `WorldLifecycleResetCompletionGate` (timeoutMs=20000)
  - [x] `ISceneTransitionService` (Loader nativo + FadeAdapter NewScripts + CompletionGate)
  - [x] `IGameNavigationService` + bridges:
    - [x] `ExitToMenuNavigationBridge` (GameExitToMenuRequestedEvent → RequestMenuAsync)
    - [x] `RestartNavigationBridge` (GameResetRequestedEvent → RequestGameplayAsync)
  - [x] `GamePauseGateBridge` (pause/resume/exit → SimulationGate)
  - [x] `IStateDependentService` (NewScriptsStateDependentService gate-aware)
  - [x] `ICameraResolver`
  - [x] `GameReadinessService` (Scene Flow → SimulationGate snapshots)

Observações:

- ⚠️ `DebugUtility` reporta “chamada repetida” em algumas resoluções de serviços no frame 0/frames específicos. Isto está classificado como **ruído tolerado** (não quebra invariantes). Pode ser mitigado depois com supressão local ou ajustes no callsite.

## B) Startup → Menu (profile=startup)

- [x] `GameStartRequestedEvent` dispara transição inicial.
- [x] `SceneTransitionService` executa:
  - [x] FadeIn (`alpha=1`) antes de carregar cenas.
  - [x] LoadingHUD: Ensure no Started, Show após FadeIn, Hide antes de FadeOut (safety hide no Completed).
  - [x] Load additive: `MenuScene`, `UIGlobalScene`.
  - [x] Unload: `NewBootstrap`.
  - [x] `Active='MenuScene'` definido.
- [x] Gate de transição:
  - [x] `SceneTransitionStarted` → `SimulationGate.Acquire(flow.scene_transition)`.
  - [x] `SceneTransitionCompleted` → `SimulationGate.Release(flow.scene_transition)`.
- [x] WorldLifecycle:
  - [x] `SceneTransitionScenesReadyEvent` recebido.
  - [x] Reset **SKIPPED** (startup/frontend).
  - [x] `WorldLifecycleResetCompletedEvent` emitido com reason `Skipped_StartupOrFrontend:profile=startup;scene=MenuScene`.
- [x] Completion gate:
  - [x] `WorldLifecycleResetCompletionGate` conclui antes de FadeOut.
- [x] GameLoop:
  - [x] `TransitionCompleted` profile não-gameplay → `RequestReady()`.

Invariantes confirmadas:

- [x] `SceneTransitionStarted` fecha gate (`flow.scene_transition`).
- [x] `ScenesReady` ocorre antes de `Completed`.
- [x] `WorldLifecycleResetCompletedEvent` ocorre antes do `FadeOut` (gate de completion).

## C) Menu → Gameplay (profile=gameplay)

- [x] Navegação por feature (não QA): `MenuPlayButtonBinder` → `IGameNavigationService.NavigateAsync(routeId='to-gameplay', reason='Menu/PlayButton')`.
- [x] `SceneTransitionService` executa:
  - [x] FadeIn → Load `GameplayScene` (additive) + (UIGlobalScene já carregada, skip).
  - [x] Unload `MenuScene`.
  - [x] `Active='GameplayScene'`.
- [x] `NewSceneBootstrapper` cria scope da `GameplayScene` e registra spawn services via `WorldDefinition`:
  - [x] `PlayerSpawnService (ordem 1)`
  - [x] `EaterSpawnService (ordem 2)`

### C1) WorldLifecycle hard reset após ScenesReady

- [x] `WorldLifecycleRuntimeCoordinator` emite hard reset: reason `ScenesReady/GameplayScene`.
- [x] `WorldLifecycleController` processa reset na `GameplayScene` com 2 spawn services.
- [x] `WorldLifecycleOrchestrator` executa fases na ordem:
  - [x] Acquire token `WorldLifecycle.WorldReset` (gate fechado).
  - [x] Hooks (scene): `SceneLifecycleHookLoggerA` em `OnBeforeDespawn/OnAfterDespawn/OnBeforeSpawn/OnAfterSpawn`.
  - [x] Despawn (primeira entrada pode ser “no actor”; em restart remove atores existentes).
  - [x] Spawn Player + Eater.
  - [x] Actor hooks phase `OnAfterActorSpawn` para ambos.
  - [x] Release token `WorldLifecycle.WorldReset`.
  - [x] `World Reset Completed`.
- [x] `WorldLifecycleResetCompletedEvent` emitido com signature gameplay e reason `ScenesReady/GameplayScene`.

### C2) Sinalização de prontidão / Playing

- [x] `SceneTransitionCompleted` libera gate `flow.scene_transition`.
- [x] `GameReadinessService` publica snapshot final: `gameplayReady=True`, `gateOpen=True`, `activeTokens=0`.
- [x] `InputModeService` aplica modo `Gameplay` (map 'Player' em PlayerInput).
- [x] `InputModeSceneFlowBridge` sincroniza GameLoop → `Playing`.
- [x] `NewScriptsStateDependentService` libera ação `Move` quando `serviceState=Playing`.

Resultado esperado confirmado:

- [x] Player e Eater spawnados (`ActorRegistry count at 'After Spawn': 2`).
- [x] `GameLoop` entra em `Playing (active=True)`.
- [x] Movimento deixa de ficar bloqueado (de `NotPlaying`/`GameplayNotReady` para “liberada”).

## D) Pause / Resume (gate tokens coerentes)

- [x] Pause:
  - [x] `GamePauseGateBridge` adquire token `state.pause`.
  - [x] `GameReadinessService` snapshot: `gateOpen=False`, `activeTokens=1`, `paused=True`.
  - [x] `PauseOverlayController` ativa overlay e muda `InputMode` → `PauseOverlay`.
  - [x] `GameLoop` entra em `Paused`.
- [x] Resume:
  - [x] `GamePauseGateBridge` libera token `state.pause`.
  - [x] `GameReadinessService` snapshot: `gateOpen=True`, `activeTokens=0`, `paused=False`.
  - [x] `InputMode` volta para `Gameplay`.
  - [x] `GameLoop` retorna a `Playing`.
  - [x] `GameRunStartedEvent` duplicado é suprimido (reentrada `Playing` marcada como `resume/duplicate`).

## E) PostGame: Victory/Defeat → Restart/ExitToMenu (idempotente)

### E1) End-of-run (Defeat/Victory)

- [x] `GameRunOutcomeService` publica `GameRunEndedEvent` (Defeat e Victory observados via QA hotkeys).
- [x] `GameRunStatusService` atualiza status: `Outcome=<...>, Reason='QA_Forced...'`.
- [x] `GameRunEndedEvent` dispara `GamePauseCommandEvent(true)` para congelar simulação.
- [x] `PostGameOverlayController` exibe overlay ao receber `GameRunEndedEvent`.
- [x] `GameLoop` entra em `Paused` após o encerramento da run.

### E2) Restart (reset + rearm)

- [x] `RestartNavigationBridge`: `GameResetRequestedEvent` → `RequestGameplayAsync`.
- [x] SceneFlow em `profile=gameplay`:
  - [x] Cenas já carregadas → `Load` skip conforme esperado.
  - [x] `ScenesReady` → WorldLifecycle hard reset executa despawn (remove) e respawn (novos IDs).
  - [x] `SceneTransitionCompleted` → `GameplayReady=True` e `GameLoop` transita Boot → Ready → Playing.
- [x] `GameRunStatusService.Clear()` chamado na nova run (resultado resetado).

### E3) ExitToMenu (frontend skip)

- [x] `ExitToMenuNavigationBridge`: `GameExitToMenuRequestedEvent` → `RequestMenuAsync`.
- [x] SceneFlow em `profile=frontend`:
  - [x] Load `MenuScene` (+ `UIGlobalScene` já carregada).
  - [x] Unload `GameplayScene`.
  - [x] `ScenesReady` → WorldLifecycle reset **SKIPPED** (startup/frontend), com `WorldLifecycleResetCompletedEvent`.
  - [x] `Completed` → `InputMode` muda para `FrontendMenu`.
- [x] `GameLoopEventInputBridge`: ExitToMenu → `RequestReady` (não retornar automaticamente para Playing).

## Pendências (não bloqueantes para o baseline)

- ⚠️ Ruído de `DebugUtility` (“chamada repetida”) em frames específicos.
- ⚠️ Durante transições, logs de bloqueio de ação por `IStateDependentService` são esperados (GateClosed / GameplayNotReady / NotPlaying).
