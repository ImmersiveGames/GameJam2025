# Baseline 2.0 — Checklist (Operacional)

**Data da última validação:** 2026-01-05  
**Fonte de verdade:** `Reports/Baseline-2.0-Smoke-LastRun.log` (validação manual)  
**Spec canônica:** [Baseline 2.0 — Spec](Baseline-2.0-Spec.md)

## Resumo
- Validação manual devido à instabilidade do parser (`Baseline2SmokeLastRunTool`).
- O log cobre: **Startup → Menu → Gameplay → IntroStage → Pause/Resume → Victory → Restart → Defeat → ExitToMenu → Menu**.
- Evidência hard foi extraída do `Baseline-2.0-Smoke-LastRun.log` usando assinaturas/strings **exatas**.

## Checklist por cenário (A–E)

| ID | Cenário | Evidências principais (resumo) | Status |
|---|---|---|---|
| A | Boot → Menu (startup) | `SceneTransitionStarted ... profile='startup'`; `Reset SKIPPED (startup/frontend)` com `reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'`; `SceneTransitionCompleted ... profile='startup'`; `GameLoop: Boot → Ready` | PASS |
| B | Menu → Gameplay (gameplay) | `NavigateAsync ... routeId='to-gameplay' ... profile='gameplay'`; `WorldLifecycle Reset REQUESTED reason='ScenesReady/GameplayScene'`; spawn `Player + Eater`; `Emitting WorldLifecycleResetCompletedEvent ... reason='ScenesReady/GameplayScene'`; `Completion gate concluído ... Prosseguindo para FadeOut` | PASS |
| C | IntroStage / PreGame (opcional) | `IntroStageStarted ... reason='SceneFlow/Completed'`; `Acquire token='sim.gameplay'`; `CompleteIntroStage ... reason='IntroStage/UIConfirm'` **ou** `IntroStage/NoContent`; `GameLoop: IntroStage → Playing`; `InputMode Apply mode='Gameplay' ... reason='GameLoop/Playing'` | PASS |
| D | Pause → Resume | `Acquire token='state.pause'` + `GameLoop: Playing → Paused`; `Release token='state.pause'` + `GameLoop: Paused → Playing` | PASS |
| E | PostGame (Victory/Defeat) + Restart + ExitToMenu | `GameRunEndedEvent Outcome=Victory/Defeat`; `Restart -> NavigateAsync routeId='to-gameplay' ... profile='gameplay'` (Boot cycle determinístico); `ExitToMenu -> NavigateAsync routeId='to-menu' ... profile='frontend'`; `Reset SKIPPED ... reason='Skipped_StartupOrFrontend:profile=frontend;scene=MenuScene'` | PASS |

## Invariantes globais

| Invariante | Requisito | Status |
|---|---|---|
| I1 | `SceneTransitionStarted` adquire `flow.scene_transition` e `SceneTransitionCompleted` libera (ex.: `SceneTransitionStarted → gate adquirido ... Profile='startup'` / `Profile='gameplay'`) | PASS |
| I2 | `ScenesReady` ocorre antes de `Completed` (ex.: `SceneTransitionScenesReady recebido ... Profile='gameplay'` antes de `SceneTransitionCompleted → gate liberado`) | PASS |
| I3 | `ResetCompleted` existe para toda transição (inclusive SKIP) | PASS |
| I4 | Completion gate é aguardado antes do `FadeOut` (ex.: `Completion gate concluído. Prosseguindo para FadeOut. signature='p:...') | PASS |
| I5 | Correlação usa `ContextSignature` canônica (ex.: `signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap'`) | PASS |

## Ordem Fade/Loading (UseFade=true)
- `TransitionStarted` → `FadeIn` (alpha=1) → `LoadingHUD.Show` → Load/Unload → `ScenesReady` → `ResetCompleted` (ou SKIP) → `LoadingHUD.Hide` (BeforeFadeOut) → `FadeOut` (alpha=0) → `Completed` (safety hide).

## Evidências hard (log — strings exatas)
- **Boot → Menu (startup)**
  - `[SceneFlow] TransitionStarted ... profile='startup'`
  - `[WorldLifecycle] Reset SKIPPED (startup/frontend) ... reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'`
  - `[SceneFlow] TransitionCompleted ... profile='startup'`
  - `GameLoop: Boot → Ready`
- **Menu → Gameplay (gameplay)**
  - `[Navigation] NavigateAsync ... routeId='to-gameplay' ... profile='gameplay'`
  - `[WorldLifecycle] Reset REQUESTED reason='ScenesReady/GameplayScene'`
  - `Spawn: Player + Eater`
  - `[WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent ... reason='ScenesReady/GameplayScene'`
  - `[SceneFlow] Completion gate concluído -> FadeOut -> Completed`
- **IntroStage / PreGame**
  - `[OBS][IntroStage] IntroStageStarted ... reason='SceneFlow/Completed'`
  - `Acquire token='sim.gameplay'`
  - `CompleteIntroStage received reason='IntroStage/UIConfirm'`
  - `GameLoop: IntroStage → Playing`
  - `[InputMode] Apply mode='Gameplay' ... reason='GameLoop/Playing'`
- **IntroStage / NoContent (auto-skip)**
  - `[OBS][IntroStage] IntroStageStarted ... reason='SceneFlow/Completed'`
  - `[OBS][IntroStage] IntroStageSkipped ... reason='IntroStage/NoContent'`
- **Pause → Resume**
  - `Acquire token='state.pause'` → `GameLoop: Playing → Paused`
  - `Release token='state.pause'` → `GameLoop: Paused → Playing`
- **PostGame (Victory) → ExitToMenu**
  - `GameRunEndedEvent Outcome=Victory`
  - `[Navigation] NavigateAsync -> routeId='to-menu' ... profile='frontend'`
  - `[WorldLifecycle] Reset SKIPPED ... reason='Skipped_StartupOrFrontend:profile=frontend;scene=MenuScene'`
- **PostGame (Defeat) → Restart**
  - `GameRunEndedEvent Outcome=Defeat`
  - `Restart -> NavigateAsync routeId='to-gameplay' ... profile='gameplay'`

## Dívida aceita
- `Baseline2SmokeLastRunTool` permanece **não-bloqueante**; o log é a evidência oficial até o tool estabilizar.
