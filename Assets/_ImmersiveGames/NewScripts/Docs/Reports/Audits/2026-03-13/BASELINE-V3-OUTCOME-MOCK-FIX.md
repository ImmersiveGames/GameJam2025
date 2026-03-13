# Baseline v3 Outcome Mock Fix

## Mantido do fix do menu

- O fix canônico de `Menu -> Gameplay` foi preservado.
- O `Play` continua entrando pelo trilho oficial via `MenuPlayButtonBinder -> ILevelFlowRuntimeService.StartGameplayDefaultAsync(...)`.
- O prefab canônico do menu continua com os callbacks persistentes apontando para `Modules.Navigation.Bindings`.

## Revertido dos triggers/timeout impropriamente canonizados

- `GameplayOutcomeTrigger` foi removido como fonte de `Victory/Defeat`.
- `SceneTest.unity` e `SceneTest2.unity` voltaram ao estado anterior, sem forçar `Victory` por trigger de level.
- O timeout do `GameplayEndConditionsController` foi despromovido:
  - continua no componente apenas como capacidade técnica,
  - mas fica desabilitado na `GameplayScene`,
  - com `timeoutReason` explícito de mock/QA.

## Novo mock explícito de Victory/Defeat

- Foi criado `GameplayOutcomeMockPanel`.
- O mock aparece durante `Playing` e expõe dois botões:
  - `Victory`
  - `Defeat`
- Ambos chamam diretamente `IGameRunEndRequestService` com razões rastreáveis:
  - `QA/BaselineV3/VictoryButton`
  - `QA/BaselineV3/DefeatButton`

## Remoção do acoplamento indevido com IntroStage

- `LevelPostGameHookService` não usa mais `IntroStageRuntimeDebugGui`.
- O post mock por level deixa de depender de:
  - `IntroStageControlService`
  - `IntroStageCoordinator`
  - `CompleteIntroStage`
  - `RequestStart`
- O hook de post atual permanece apenas como mock leve/log-only, sem reiniciar run e sem sair de `PostGame` por caminho indevido.

## Arquivos modificados

- `Modules/LevelFlow/Runtime/LevelPostGameHookService.cs`
- `Modules/GameLoop/Bindings/EndConditions/GameplayOutcomeMockPanel.cs`
- `Modules/GameLoop/Bindings/EndConditions/GameplayOutcomeMockPanel.cs.meta`
- `../Scenes/GameplayScene.unity`
- `../Scenes/SceneTest.unity`
- `../Scenes/SceneTest2.unity`
- `Docs/Reports/Audits/2026-03-13/BASELINE-V3-OUTCOME-MOCK-FIX.md`
- `Docs/Reports/Audits/2026-03-13/BASELINE-V3-OUTCOME-MOCK-FIX.md.meta`

## QA executado

- Validação estática do menu:
  - `Play` continua ligado ao binder canônico.
- Validação estática do mock de resultado:
  - `GameplayScene` agora contém `GameplayOutcomeMockPanel`.
  - O mock usa `IGameRunEndRequestService`.
  - O timeout está desligado na cena canônica.
- Validação estática do post:
  - `LevelPostGameHookService` não chama mais `IntroStageRuntimeDebugGui`.
  - Não há uso do contrato de IntroStage para concluir mock de post neste fluxo.
