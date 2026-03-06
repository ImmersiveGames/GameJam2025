# Baseline 3.1 Freeze (2026-03-06)

## Objetivo
Este freeze consolida o baseline canonico de SceneFlow/Navigation/LevelFlow com MacroRestart serializado por owner unico, LevelFlow por `levelRef` e evidencia local completa para evitar regressoes por retorno de compat/fallback.

## Checklist de cenarios cobertos
- [x] A) Boot -> Menu
- [x] B) Menu -> Gameplay (default Level1)
- [x] C) NextLevel (Level1 -> Level2) com unload/load correto
- [x] D) RestartCurrentLevelLocal (Level2 -> Level2) sem macro transition
- [x] E) Victory/Defeat -> PostGame/Restart -> Gameplay default (Level1) + IntroStage -> Playing
- [x] F) MacroRestart com owner unico + serializacao/coalescing ativo no trilho canonico (nesta captura nao houve dupla solicitacao para gerar queued/debounced).

## Ancoras de log (essenciais)
### A) Boot -> Menu
- `lastlog.log:687` `[OBS][SceneFlow] MacroLoadingPhase='LevelPrepare' routeId='to-menu' reason='Boot/StartPlan'`

### B) Menu -> Gameplay default
- `lastlog.log:1145` `MacroLoadingPhase='LevelPrepare' routeId='to-gameplay' reason='Menu/PlayButton'`
- `lastlog.log:1147` `[OBS][LevelFlow] LevelDefaultSelected ... levelRef='Level1'`
- `lastlog.log:1215` `[OBS][IntroStageController] IntroStageStartRequested source='SceneFlowCompleted' ...`

### C) NextLevel (L1 -> L2)
- `lastlog.log:1449` `[OBS][LevelFlow] LevelAdditiveApplySummary ... loadedIndices=[8] unloadedIndices=[7]`
- `lastlog.log:1485` `[OBS][LevelFlow] LevelSwapLocalApplied fromLevelRef='Level1' toLevelRef='Level2'`

### D) RestartCurrentLevelLocal (L2 -> L2)
- `lastlog.log:1553` `[OBS][LevelFlow] LevelAdditiveApplySummary ... loadedIndices=[8] unloadedIndices=[8]`
- `lastlog.log:1593` `[OBS][QA][LevelFlow] RestartCurrentLevelLocalCompleted ... noMacroTransition='true' transitionStartedCount='0'`

### E) PostGame/Restart -> default Level1
- `lastlog.log:1685` `[OBS][Navigation] MacroRestartStart runId='1' effectiveReason='PostGame/Restart#r1'`
- `lastlog.log:2033` `[OBS][LevelFlow] LevelDefaultSelected ... levelRef='Level1' reason='PostGame/Restart#r1'`
- `lastlog.log:2061` `[OBS][LevelFlow] LevelAdditiveApplySummary ... loadedIndices=[7] unloadedIndices=[8]`
- `lastlog.log:2111` `[OBS][IntroStageController] IntroStageStarted ...`
- `lastlog.log:2117` `[OBS][IntroStageController] GameplaySimulationBlocked token='sim.gameplay' ...`
- `lastlog.log:2173` `[OBS][IntroStageController] GameplaySimulationUnblocked token='sim.gameplay' ...`

### F) MacroRestart owner unico
- `lastlog.log:363` `MacroRestartCoordinator registered (GameResetRequestedEvent -> canonical macro restart)`
- `lastlog.log:101` `[OBS][LEGACY] GameResetRequestedEvent listener disabled in GameLoopCommandEventBridge ...`

## Source of Truth (canonico)
- Identidade local de level: `LevelSignature` + `LevelDefinitionAsset` (`levelRef`)
- Policy macro/reset: `SceneRoute` + `RoutePolicy` + `MacroLevelPrepareCompletionGate`
- Macro restart: `MacroRestartCoordinator` como owner unico de `GameResetRequestedEvent` (serializacao/coalescing/debounce)

## Evidencia completa`n- Observacao: `doisResets-na-sequencia.txt` foi congelado a partir do log local completo disponivel nesta sessao.`n
- `Docs/Reports/Baseline/2026-03-06/lastlog.log` (arquivo completo)
- `Docs/Reports/Baseline/2026-03-06/doisResets-na-sequencia.txt` (arquivo completo local para trilha de restart)

