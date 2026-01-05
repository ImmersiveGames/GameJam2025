# Baseline 2.0 — Checklist (Operacional)

**Data da última validação:** 2026-01-05  
**Fonte de verdade:** `Reports/Baseline-2.0-Smoke-LastRun.log` (validação manual)  
**Spec canônica:** [Baseline 2.0 — Spec](Baseline-2.0-Spec.md)

## Resumo
- Validação manual devido à instabilidade do parser (`Baseline2SmokeLastRunTool`).
- O log cobre: **Startup → Menu → Gameplay → Pause/Resume → Victory → Restart → Defeat → ExitToMenu → Menu**.
- Evidência hard foi extraída do `Baseline-2.0-Smoke-LastRun.log` usando assinaturas/strings **exatas**.

## Checklist por cenário (A–E)

| ID | Cenário | Evidências principais (resumo) | Status |
|---|---|---|---|
| A | Boot → Menu (startup) | `SceneTransitionStarted → gate adquirido` (Profile='startup'); `Reset SKIPPED (startup/frontend)`; `Emitting WorldLifecycleResetCompletedEvent` com `signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap'`; `Completion gate concluído ... Prosseguindo para FadeOut` | PASS |
| B | Menu → Gameplay (gameplay) | `NavigateAsync ... routeId='to-gameplay' ... Profile='gameplay'`; `Disparando hard reset após ScenesReady`; `Emitting WorldLifecycleResetCompletedEvent ... signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'`; `ActorRegistry count at 'After Spawn': 2`; `GameLoop ENTER: Playing` | PASS |
| C | Pause → Resume | `Acquire token='state.pause'` + `GameLoop ENTER: Paused`; `Release token='state.pause'` + `GameLoop EXIT: Paused` | PASS |
| D | PostGame (Defeat) → Restart | `RequestEnd(Defeat, reason='Gameplay/DevManualDefeat')`; `NavigateAsync ... routeId='to-gameplay' ... Profile='gameplay'`; reset completo com `ActorRegistry count at 'After Spawn': 2` (segunda execução) | PASS |
| E | PostGame (Victory) → ExitToMenu | `RequestEnd(Victory, reason='Gameplay/DevManualVictory')`; `NavigateAsync ... routeId='to-menu' ... Profile='frontend'`; `Reset SKIPPED (startup/frontend)` + `WorldLifecycleResetCompletedEvent` com `signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene'` | PASS |

## Invariantes globais

| Invariante | Requisito | Status |
|---|---|---|
| I1 | `SceneTransitionStarted` adquire `flow.scene_transition` e `SceneTransitionCompleted` libera (ex.: `SceneTransitionStarted → gate adquirido ... Profile='startup'` / `Profile='gameplay'`) | PASS |
| I2 | `ScenesReady` ocorre antes de `Completed` (ex.: `SceneTransitionScenesReady recebido ... Profile='gameplay'` antes de `SceneTransitionCompleted → gate liberado`) | PASS |
| I3 | `ResetCompleted` existe para toda transição (inclusive SKIP) | PASS |
| I4 | Completion gate é aguardado antes do `FadeOut` (ex.: `Completion gate concluído. Prosseguindo para FadeOut. signature='p:...') | PASS |
| I5 | Correlação usa `ContextSignature` canônica (ex.: `signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap'`) | PASS |

## Ordem Fade/Loading (UseFade=true)
- `FadeIn` → `LoadingHUD.Show` → Load/Unload → `ScenesReady` → `ResetCompleted` → `LoadingHUD.Hide` → `FadeOut` → `Completed`.

## Evidências hard (log — strings exatas)
- **Boot → Menu (startup)**
  - `[Readiness] SceneTransitionStarted → gate adquirido ... Profile='startup'`
  - `[WorldLifecycle] Reset SKIPPED (startup/frontend). ... profile='startup', activeScene='MenuScene'.`
  - `[WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='startup', signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap', reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'.`
  - `[SceneFlow] Completion gate concluído. Prosseguindo para FadeOut. signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap'.`
- **Menu → Gameplay (gameplay)**
  - `[Navigation] NavigateAsync -> routeId='to-gameplay', reason='Menu/PlayButton', ... Profile='gameplay'.`
  - `[WorldLifecycle] Disparando hard reset após ScenesReady. reason='ScenesReady/GameplayScene', profile='gameplay'`
  - `[WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='gameplay', signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', reason='ScenesReady/GameplayScene'.`
  - `[WorldLifecycleOrchestrator] ActorRegistry count at 'After Spawn': 2`
  - `[GameLoop] ENTER: Playing (active=True)`
- **Pause → Resume**
  - `[Gate] Acquire token='state.pause'. Active=1. IsOpen=False`
  - `[GameLoop] ENTER: Paused (active=False)`
  - `[Gate] Release token='state.pause'. Active=0. IsOpen=True`
  - `[GameLoop] EXIT: Paused`
- **PostGame (Victory) → ExitToMenu**
  - `[GameRunEndRequestService] RequestEnd(Victory, reason='Gameplay/DevManualVictory')`
  - `[Navigation] NavigateAsync -> routeId='to-menu', reason='ExitToMenu/Event', ... Profile='frontend'.`
  - `[WorldLifecycle] Reset SKIPPED (startup/frontend). ... profile='frontend', activeScene='MenuScene'.`
  - `[WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='frontend', signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene', reason='Skipped_StartupOrFrontend:profile=frontend;scene=MenuScene'.`
- **PostGame (Defeat) → Restart**
  - `[GameRunEndRequestService] RequestEnd(Defeat, reason='Gameplay/DevManualDefeat')`
  - `[Navigation] NavigateAsync -> routeId='to-gameplay', reason='PostGame/Restart', ... Profile='gameplay'.`
  - `[WorldLifecycleOrchestrator] ActorRegistry count at 'After Spawn': 2`

## Dívida aceita
- `Baseline2SmokeLastRunTool` permanece **não-bloqueante**; o log é a evidência oficial até o tool estabilizar.
