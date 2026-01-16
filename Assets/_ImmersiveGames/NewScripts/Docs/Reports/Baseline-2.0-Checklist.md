# Baseline 2.0 — Checklist (Operacional)

**Data da última validação:** 2026-01-05  
**Fonte de verdade:** `Reports/Baseline-2.0-Smoke-LastRun.log` (baseline log-driven)  
**Spec canônica (histórico):** [Baseline 2.0 — Spec](Baseline-2.0-Spec.md)

## Resumo
- Validação manual devido à instabilidade do parser (`Baseline2SmokeLastRunTool`).
- O log cobre: **Startup → Menu → Gameplay → Pause/Resume → Victory → Restart → Defeat → ExitToMenu → Menu**.
- A evidência hard (strings **exatas** do log) está listada na seção de evidências.
- Restart volta para Boot no fluxo atual (após `PostGame/Restart`): `NavigateAsync -> routeId='to-gameplay', reason='PostGame/Restart'` seguido de `EXIT: Playing` e `ENTER: Boot (active=False)` no mesmo momento do reset do GameLoop.

## Scope do Baseline 2.0 (Opção B)
- **IntroStage é opcional e está fora do baseline** atual.
- O baseline 2.0 fica oficialmente fechado para os cenários A–E cobertos pelo smoke log.
- A validação de IntroStage será feita em smoke separado (Baseline 2.1 ou “IntroStage smoke”) quando o fluxo estiver promovido.

## Checklist por cenário (A–E)

| ID | Cenário | Evidências principais (resumo) | Status |
|---|---|---|---|
| A | Boot → Menu (startup) | `SceneTransitionStarted ... profile='startup'`; `Reset SKIPPED (startup/frontend)` com `reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'`; `SceneTransitionCompleted ... profile='startup'`; `GameLoop: Boot → Ready` | PASS |
| B | Menu → Gameplay (gameplay) | `NavigateAsync ... routeId='to-gameplay' ... profile='gameplay'`; `WorldLifecycle Reset REQUESTED reason='ScenesReady/GameplayScene'`; `Emitting WorldLifecycleResetCompletedEvent ... reason='ScenesReady/GameplayScene'`; `Completion gate concluído ... Prosseguindo para FadeOut` | PASS |
| C | IntroStage / PreGame (opcional) | **Sem evidência no log atual.** | NOT COVERED / PENDING (needs new smoke log) |
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

### Cenário A — Boot → Menu (startup)
- `<color=#A8DEED>[INFO] [SceneTransitionService] [SceneFlow] Iniciando transição: Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap], Active='MenuScene', UseFade=True, Profile='startup'</color>`
- `[INFO] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] SceneTransitionScenesReady recebido. Context=Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap], Active='MenuScene', UseFade=True, Profile='startup'`
- `<color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Reset SKIPPED (startup/frontend). why='profile', profile='startup', activeScene='MenuScene'. (@ 5,93s)</color>`
- `<color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='startup', signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap', reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'. (@ 5,93s)</color>`
- `[VERBOSE] [GameLoopService] [GameLoop] ENTER: Ready (active=False) (@ 6,44s)`

### Cenário B — Menu → Gameplay (gameplay)
- `<color=#A8DEED>[INFO] [GameNavigationService] [Navigation] NavigateAsync -> routeId='to-gameplay', reason='Menu/PlayButton', Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay'.</color>`
- `[INFO] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] SceneTransitionScenesReady recebido. Context=Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay'`
- `<color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='gameplay', signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', reason='ScenesReady/GameplayScene'. (@ 9,50s)</color>`
- `[VERBOSE] [SceneTransitionService] [SceneFlow] Completion gate concluído. Prosseguindo para FadeOut. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 9,50s)`
- `[VERBOSE] [GameLoopService] [GameLoop] ENTER: Playing (active=True) (@ 9,97s)`

### Cenário D — Pause → Resume
- `[VERBOSE] [SimulationGateService] [Gate] Acquire token='state.pause'. Active=1. IsOpen=False (@ 10,98s)`
- `[VERBOSE] [GameLoopService] [GameLoop] ENTER: Paused (active=False) (@ 10,99s)`
- `[VERBOSE] [SimulationGateService] [Gate] Release token='state.pause'. Active=0. IsOpen=True (@ 11,75s)`
- `[VERBOSE] [GameLoopService] [GameLoop] ENTER: Playing (active=True) (@ 11,75s)`

### Cenário E — PostGame (Victory/Defeat) + Restart + ExitToMenu
- `[INFO] [GameRunOutcomeService] [GameLoop] Publicando GameRunEndedEvent. Outcome=Victory, Reason='Gameplay/DevManualVictory'.`
- `[INFO] [GameRunOutcomeService] [GameLoop] Publicando GameRunEndedEvent. Outcome=Defeat, Reason='Gameplay/DevManualDefeat'.`
- `<color=#A8DEED>[INFO] [GameNavigationService] [Navigation] NavigateAsync -> routeId='to-gameplay', reason='PostGame/Restart', Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay'.</color>`
- `[VERBOSE] [GameLoopService] [GameLoop] EXIT: Playing (@ 14,62s)`
- `[VERBOSE] [GameLoopService] [GameLoop] ENTER: Boot (active=False) (@ 14,62s)`
- `<color=#A8DEED>[INFO] [GameNavigationService] [Navigation] NavigateAsync -> routeId='to-menu', reason='ExitToMenu/Event', Load=[MenuScene, UIGlobalScene], Unload=[GameplayScene], Active='MenuScene', UseFade=True, Profile='frontend'.</color>`

### Invariantes globais (I1–I5)

**I1 — `SceneTransitionStarted` fecha gate e `SceneTransitionCompleted` libera**
- `[VERBOSE] [SimulationGateService] [Gate] Acquire token='flow.scene_transition'. Active=1. IsOpen=False (@ 3,61s)`
- `[VERBOSE] [GameReadinessService] [Readiness] SceneTransitionStarted → gate adquirido e jogo marcado como NOT READY. Context=Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap], Active='MenuScene', UseFade=True, Profile='startup' (@ 3,61s)`
- `[VERBOSE] [SimulationGateService] [Gate] Release token='flow.scene_transition'. Active=0. IsOpen=True (@ 6,43s)`
- `[VERBOSE] [GameReadinessService] [Readiness] SceneTransitionCompleted → gate liberado e fase GameplayReady marcada. gameplayReady=False. Context=Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap], Active='MenuScene', UseFade=True, Profile='startup' (@ 6,43s)`

**I2 — `ScenesReady` ocorre antes de `Completed`**
- `[INFO] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] SceneTransitionScenesReady recebido. Context=Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay'`
- `[VERBOSE] [GameReadinessService] [Readiness] SceneTransitionScenesReady → fase WorldLoaded sinalizada. Context=Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay' (@ 9,50s)`
- `[VERBOSE] [GameReadinessService] [Readiness] SceneTransitionCompleted → gate liberado e fase GameplayReady marcada. gameplayReady=True. Context=Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay' (@ 15,63s)`

**I3 — `ResetCompleted` existe para toda transição (inclusive SKIP)**
- `<color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Reset SKIPPED (startup/frontend). why='profile', profile='startup', activeScene='MenuScene'. (@ 5,93s)</color>`
- `<color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='startup', signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap', reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'. (@ 5,93s)</color>`
- `<color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='gameplay', signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', reason='ScenesReady/GameplayScene'. (@ 9,50s)</color>`
- `<color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='frontend', signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene', reason='Skipped_StartupOrFrontend:profile=frontend;scene=MenuScene'. (@ 19,61s)</color>`

**I4 — Completion gate aguardado antes do FadeOut**
- `[VERBOSE] [SceneTransitionService] [SceneFlow] Aguardando completion gate antes do FadeOut. signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap'. (@ 5,94s)`
- `[VERBOSE] [WorldLifecycleResetCompletionGate] [SceneFlowGate] Já concluído (cached). signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap'. (@ 5,94s)`
- `[VERBOSE] [SceneTransitionService] [SceneFlow] Completion gate concluído. Prosseguindo para FadeOut. signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap'. (@ 5,94s)`

**I5 — `ContextSignature` canônica usada na correlação**
- `[VERBOSE] [SceneFlowLoadingService] [Loading] Started → Ensure only (Show após FadeIn). signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap'. (@ 3,61s)`
- `[VERBOSE] [SceneFlowLoadingService] [Loading] Started → Ensure only (Show após FadeIn). signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 8,82s)`
- `[VERBOSE] [SceneFlowLoadingService] [Loading] Started → Ensure only (Show após FadeIn). signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene'. (@ 19,03s)`

## Dívida aceita
- `Baseline2SmokeLastRunTool` permanece **não-bloqueante**; o log é a evidência oficial até o tool estabilizar.
