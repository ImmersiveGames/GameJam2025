# Canon Index

## O que e canonico hoje
- Macro restart: `MacroRestartCoordinator` e owner unico de `GameResetRequestedEvent`.
- `GameLoopCommandEventBridge` nao consome reset.
- `RestartNavigationBridge` desativado (LEGACY).
- LevelFlow por `LevelDefinitionAsset` (`levelRef`) e `LevelCollection` por macro gameplay.
- `MacroLevelPrepareCompletionGate` executa `LevelPrepare`/`LevelClear` antes do FadeOut.
- `LevelStageOrchestrator` dedupe por `LevelSignature` (com regra de rewind para fallback sem assinatura).

## Como validar (manual curto)
1. Boot -> Menu.
2. Menu -> Gameplay.
3. Executar `QA/LevelFlow/NextLevel`.
4. Executar `QA/LevelFlow/RestartCurrentLevelLocal`.
5. Finalizar run (Victory/Defeat) e acionar PostGame/Restart.
6. Confirmar retorno para default Level1 e IntroStage -> Playing.

## Ancoras de log obrigatorias (grep)
- `[OBS][Navigation] MacroRestartStart`
- `[OBS][Navigation] MacroRestartCompleted`
- `[OBS][Navigation] MacroRestartQueued` (quando houver solicitacao durante in-flight)
- `[OBS][Navigation] MacroRestartDebounced` (quando houver duplo clique em janela curta)
- `[OBS][LevelFlow] LevelDefaultSelected`
- `[OBS][LevelFlow] LevelAdditiveApplySummary`
- `[OBS][SceneFlow] MacroLoadingPhase='LevelPrepare'`
- `[OBS][IntroStageController] IntroStageStartRequested source='SceneFlowCompleted'`
- `[OBS][IntroStageController] IntroStageStarted`
- `[OBS][IntroStageController] GameplaySimulationBlocked`
- `[OBS][IntroStageController] GameplaySimulationUnblocked`

## Owners por responsabilidade
| Responsabilidade | Arquivo owner |
|---|---|
| Macro restart canonico | `Modules/Navigation/Runtime/MacroRestartCoordinator.cs` |
| Level prepare/clear por macro | `Modules/LevelFlow/Runtime/LevelMacroPrepareService.cs` |
| Aplicacao additive/reload/clear local | `Modules/LevelFlow/Runtime/LevelAdditiveSceneRuntimeApplier.cs` |
| Dedupe de intro por assinatura local | `Modules/LevelFlow/Runtime/LevelStageOrchestrator.cs` |
| Gate obrigatorio antes do FadeOut | `Modules/SceneFlow/Transition/Runtime/MacroLevelPrepareCompletionGate.cs` |

