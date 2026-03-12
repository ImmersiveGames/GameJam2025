# Canon Index

## Fonte de verdade associada
- Baseline/evidencia canonica vigente: `Docs/Reports/Evidence/LATEST.md`
- Baseline congelada vigente: `Docs/Reports/Baseline/2026-03-06/Baseline-3.1-Freeze.md`
- Auditoria estatica vigente: `Docs/Reports/Audits/LATEST.md`
- Trilho de planejamento vigente: `Docs/Plans/Plan-Continuous.md`

## O que e canonico hoje
- Macro restart: `MacroRestartCoordinator` e owner unico de `GameResetRequestedEvent`.
- `GameLoopCommandEventBridge` nao consome reset.
- `RestartNavigationBridge` nao faz parte do runtime canonico atual.
- LevelFlow por `LevelDefinitionAsset` (`levelRef`) e `LevelCollection` por macro gameplay.
- `LevelDefinition` reduzido ao shape canonico `levelRef + macroRouteRef`.
- `MacroLevelPrepareCompletionGate` executa `LevelPrepare`/`LevelClear` antes do FadeOut.
- `LevelStageOrchestrator` dedupe por `LevelSignature`.
- `GameplayStartSnapshot`, `LevelSelectedEvent` e `LevelSwapLocalAppliedEvent` promovem `LevelRef`, `MacroRouteId` e `LevelSignature` como shape principal.
- `IGameNavigationService` expoe apenas a superficie canonica; nao ha trilho publico string-first em Navigation.
- `WorldLifecycle V2` e apenas telemetria/observabilidade com `MacroRouteId`, `Reason`, `MacroSignature` e `LevelSignature`.
- Tooling/editor/QA do eixo principal foi higienizado para o contrato canonico atual.

## Fechamento do eixo principal

- Considerar **canon-only no eixo principal**:
  - `LevelFlow`
  - `LevelDefinition`
  - `Navigation`
  - `WorldLifecycle V2`
  - tooling/editor/QA associado
- Nao considerar **canon-only absoluto em todo `NewScripts/**` ainda**:
  - permanece excecao localizada em `Gameplay RunRearm` com fallback legado de actor-kind/string;
  - permanece residuo menor editor/serializado em `GameNavigationIntentCatalogAsset`, sem reabrir trilho paralelo de runtime.

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
| Gameplay route principal (resolucao canonica) | `Modules/Navigation/GameNavigationService.cs` |
