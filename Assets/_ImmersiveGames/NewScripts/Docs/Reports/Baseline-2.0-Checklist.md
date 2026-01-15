# Baseline 2.0 — Checklist (Operacional)

**Data da última validação:** 2026-01-05  
**Fonte de verdade:** `Reports/Baseline-2.0-Smoke-LastRun.log` (validação manual)  
**Spec canônica:** [Baseline 2.0 — Spec](Baseline-2.0-Spec.md)

## Resumo
- Validação manual devido à instabilidade do parser (`Baseline2SmokeLastRunTool`).
- O log cobre: **Startup → Menu → Gameplay → Pause/Resume → Victory → Restart → Defeat → ExitToMenu → Menu**.
- Evidência hard foi extraída do `Baseline-2.0-Smoke-LastRun.log` usando assinaturas/strings **exatas**.

## Checklist por cenário (A–E)

| ID | Cenário | Evidências principais (strings exatas do log) | Status |
|---|---|---|---|
| A | Boot → Menu (startup) | `<color=#A8DEED>[INFO] [SceneTransitionService] [SceneFlow] Iniciando transição: Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap], Active='MenuScene', UseFade=True, Profile='startup'</color>`<br>`<color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Reset SKIPPED (startup/frontend). why='profile', profile='startup', activeScene='MenuScene'. (@ 5,93s)</color>`<br>`[VERBOSE] [SceneTransitionService] [SceneFlow] Completion gate concluído. Prosseguindo para FadeOut. signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap'. (@ 5,94s)`<br>`[VERBOSE] [GameLoopService] [GameLoop] ENTER: Ready (active=False) (@ 6,44s)` | PASS |
| B | Menu → Gameplay (gameplay) | `<color=#A8DEED>[INFO] [GameNavigationService] [Navigation] NavigateAsync -> routeId='to-gameplay', reason='Menu/PlayButton', Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay'.</color>`<br>`[INFO] [WorldLifecycleController] Processando reset. label='WorldReset(reason='ScenesReady/GameplayScene')', scene='GameplayScene'.`<br>`<color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='gameplay', signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', reason='ScenesReady/GameplayScene'. (@ 9,50s)</color>`<br>`[VERBOSE] [SceneTransitionService] [SceneFlow] Completion gate concluído. Prosseguindo para FadeOut. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 9,50s)` | PASS |
| C | IntroStage / PreGame (opcional) | Sem evidência no log atual. | NOT COVERED / PENDING (needs new smoke log) |
| D | Pause → Resume | `[VERBOSE] [SimulationGateService] [Gate] Acquire token='state.pause'. Active=1. IsOpen=False (@ 10,98s)`<br>`[VERBOSE] [GameLoopService] [GameLoop] ENTER: Paused (active=False) (@ 10,99s)`<br>`[VERBOSE] [SimulationGateService] [Gate] Release token='state.pause'. Active=0. IsOpen=True (@ 11,75s)`<br>`[VERBOSE] [GameLoopService] [GameLoop] ENTER: Playing (active=True) (@ 11,75s)` | PASS |
| E | PostGame (Victory/Defeat) + Restart + ExitToMenu | `[INFO] [GameRunOutcomeService] [GameLoop] Publicando GameRunEndedEvent. Outcome=Victory, Reason='Gameplay/DevManualVictory'.`<br>`<color=#A8DEED>[INFO] [GameNavigationService] [Navigation] NavigateAsync -> routeId='to-gameplay', reason='PostGame/Restart', Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay'.</color>`<br>`[INFO] [GameRunOutcomeService] [GameLoop] Publicando GameRunEndedEvent. Outcome=Defeat, Reason='Gameplay/DevManualDefeat'.`<br>`<color=#A8DEED>[INFO] [GameNavigationService] [Navigation] NavigateAsync -> routeId='to-menu', reason='ExitToMenu/Event', Load=[MenuScene, UIGlobalScene], Unload=[GameplayScene], Active='MenuScene', UseFade=True, Profile='frontend'.</color>`<br>`<color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='frontend', signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene', reason='Skipped_StartupOrFrontend:profile=frontend;scene=MenuScene'. (@ 19,61s)</color>` | PASS |

## Invariantes globais

| Invariante | Requisito | Status |
|---|---|---|
| I1 | `SceneTransitionStarted` adquire `flow.scene_transition` e `SceneTransitionCompleted` libera (ex.: `[VERBOSE] [SimulationGateService] [Gate] Acquire token='flow.scene_transition'. Active=1. IsOpen=False (@ 3,61s)` / `[VERBOSE] [GameReadinessService] [Readiness] SceneTransitionCompleted → gate liberado e fase GameplayReady marcada. gameplayReady=True. Context=Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay' (@ 9,96s)`) | PASS |
| I2 | `ScenesReady` ocorre antes de `Completed` (ex.: `[VERBOSE] [SceneFlowLoadingService] [Loading] ScenesReady → Update pending. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 9,50s)` antes de `[VERBOSE] [SceneFlowLoadingService] [Loading] Completed → Safety hide. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 9,95s)`) | PASS |
| I3 | `ResetCompleted` existe para toda transição (inclusive SKIP) (ex.: `<color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='startup', signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap', reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'. (@ 5,93s)</color>` / `<color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='gameplay', signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', reason='ScenesReady/GameplayScene'. (@ 9,50s)</color>`) | PASS |
| I4 | Completion gate é aguardado antes do `FadeOut` (ex.: `[VERBOSE] [SceneTransitionService] [SceneFlow] Completion gate concluído. Prosseguindo para FadeOut. signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap'. (@ 5,94s)`) | PASS |
| I5 | Correlação usa `ContextSignature` canônica (ex.: `[VERBOSE] [SceneTransitionService] [SceneFlow] Aguardando completion gate antes do FadeOut. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 9,50s)`) | PASS |

## Ordem Fade/Loading (UseFade=true)
- `TransitionStarted` → `FadeIn` (alpha=1) → `LoadingHUD.Show` → Load/Unload → `ScenesReady` → `ResetCompleted` (ou SKIP) → `LoadingHUD.Hide` (BeforeFadeOut) → `FadeOut` (alpha=0) → `Completed` (safety hide).

## Evidências hard (log — strings exatas)
- **Boot → Menu (startup)**
  - `<color=#A8DEED>[INFO] [SceneTransitionService] [SceneFlow] Iniciando transição: Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap], Active='MenuScene', UseFade=True, Profile='startup'</color>`
  - `<color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Reset SKIPPED (startup/frontend). why='profile', profile='startup', activeScene='MenuScene'. (@ 5,93s)</color>`
  - `[VERBOSE] [SceneTransitionService] [SceneFlow] Completion gate concluído. Prosseguindo para FadeOut. signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap'. (@ 5,94s)`
  - `[VERBOSE] [GameLoopService] [GameLoop] ENTER: Ready (active=False) (@ 6,44s)`
- **Menu → Gameplay (gameplay)**
  - `<color=#A8DEED>[INFO] [GameNavigationService] [Navigation] NavigateAsync -> routeId='to-gameplay', reason='Menu/PlayButton', Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay'.</color>`
  - `[INFO] [WorldLifecycleController] Processando reset. label='WorldReset(reason='ScenesReady/GameplayScene')', scene='GameplayScene'.`
  - `<color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='gameplay', signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', reason='ScenesReady/GameplayScene'. (@ 9,50s)</color>`
  - `[VERBOSE] [SceneTransitionService] [SceneFlow] Completion gate concluído. Prosseguindo para FadeOut. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 9,50s)`
- **Pause → Resume**
  - `[VERBOSE] [SimulationGateService] [Gate] Acquire token='state.pause'. Active=1. IsOpen=False (@ 10,98s)`
  - `[VERBOSE] [GameLoopService] [GameLoop] ENTER: Paused (active=False) (@ 10,99s)`
  - `[VERBOSE] [SimulationGateService] [Gate] Release token='state.pause'. Active=0. IsOpen=True (@ 11,75s)`
  - `[VERBOSE] [GameLoopService] [GameLoop] ENTER: Playing (active=True) (@ 11,75s)`
- **PostGame (Victory/Defeat) + Restart + ExitToMenu**
  - `[INFO] [GameRunOutcomeService] [GameLoop] Publicando GameRunEndedEvent. Outcome=Victory, Reason='Gameplay/DevManualVictory'.`
  - `<color=#A8DEED>[INFO] [GameNavigationService] [Navigation] NavigateAsync -> routeId='to-gameplay', reason='PostGame/Restart', Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay'.</color>`
  - `[INFO] [GameRunOutcomeService] [GameLoop] Publicando GameRunEndedEvent. Outcome=Defeat, Reason='Gameplay/DevManualDefeat'.`
  - `<color=#A8DEED>[INFO] [GameNavigationService] [Navigation] NavigateAsync -> routeId='to-menu', reason='ExitToMenu/Event', Load=[MenuScene, UIGlobalScene], Unload=[GameplayScene], Active='MenuScene', UseFade=True, Profile='frontend'.</color>`
  - `<color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='frontend', signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene', reason='Skipped_StartupOrFrontend:profile=frontend;scene=MenuScene'. (@ 19,61s)</color>`

## Dívida aceita
- `Baseline2SmokeLastRunTool` permanece **não-bloqueante**; o log é a evidência oficial até o tool estabilizar.
