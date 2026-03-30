# Production How-To Use Core Modules

## Status documental

- Parcial / leitura junto do runtime atual.
- O guia descreve a superficie publica real sem fingir pureza arquitetural.
- Nomes antigos podem aparecer como compatibilidade, mas o owner real segue a estrutura atual.

## O que este guia cobre

- servicos publicos que voce realmente chama
- assets canonicos que voce realmente configura
- receitas para rota, style, level, GameplayReset e post-run
- loading de producao do macro flow
- contratos atuais de intro, post-run, level e GameplayReset
- superficie de hooks de save como placeholder estavel

## Comece por aqui

| Quero fazer isto | Use isto | Owner atual | Exemplo curto real |
|---|---|---|---|
| Abrir o gameplay | `ILevelFlowRuntimeService.StartGameplayDefaultAsync(reason, ct)` | `Orchestration/LevelLifecycle` | `await levelFlow.StartGameplayDefaultAsync("Menu/PlayButton", cancellationToken);` |
| Reiniciar a run | `IPostLevelActionsService.RestartLevelAsync(reason, ct)` ou `IGameCommands.RequestRestart(reason)` | `Orchestration/LevelLifecycle` / `Orchestration/GameLoop/Commands` | `gameCommands.RequestRestart("Pause/RestartButton");` |
| Ir para o menu | `IGameNavigationService.GoToMenuAsync(reason)` ou `IPostLevelActionsService.ExitToMenuAsync(reason, ct)` | `Orchestration/Navigation` / `Orchestration/LevelLifecycle` | `await navigation.GoToMenuAsync("Pause/ExitToMenu");` |
| Trocar para o proximo level | `IPostLevelActionsService.NextLevelAsync(reason, ct)` | `Orchestration/LevelLifecycle` | `await postLevelActions.NextLevelAsync("PostRun/NextLevel", cancellationToken);` |
| Trocar para um level especifico | `ILevelFlowRuntimeService.SwapLevelLocalAsync(levelRef, reason, ct)` | `Orchestration/LevelLifecycle` | `await levelFlow.SwapLevelLocalAsync(levelRef, "UI/SelectLevel", cancellationToken);` |
| GameplayReset local de atores | `IActorGroupGameplayResetOrchestrator.RequestResetAsync(request)` | `Game/Gameplay/GameplayReset` | `await actorGroupGameplayReset.RequestResetAsync(request);` |
| Fechar ou pular intro atual | `IIntroStageControlService.CompleteIntroStage(reason)` | `Orchestration/GameLoop/IntroStage` | `introStageControl.CompleteIntroStage("Intro/ContinueButton");` |
| Validar o PostStage da cena atual | `IPostStageControlService.TryComplete(reason)` / `TrySkip(reason)` | `Experience/PostRun/Handoff` | `postStageControl.TryComplete("PostStage/ContinueButton");` |
| Atualizar a HUD de loading | `ILoadingPresentationService.SetProgress(signature, snapshot)` | `Orchestration/SceneFlow` | `loadingPresentation.SetProgress(signature, snapshot);` |
| Integrar hooks de Save | `ISaveOrchestrationService` e contratos de `Progression` / `Checkpoint` | `Experience/Save` | `saveOrchestration.TryHandleGameRunEnded(evt, out reason);` |
| Reservar checkpoint manual futuro | `IManualCheckpointRequestService` | `Experience/Save` | `manualCheckpoint.TryRequestManualCheckpoint("UI/CheckpointButton", out reason);` |

## Como pensar o fluxo atual

- `startup` pertence ao bootstrap.
- `SceneFlow` resolve a rota macro.
- `LevelLifecycle` resolve lifecycle local e content handoff.
- `GameLoop` resolve run state, pause e outcome.
- `PostRun` resolve handoff, ownership, result e presentation do pos-run.
- `Gameplay/State` ficou em `Core`, `RuntimeSignals` e `Gate`.
- `Gameplay/GameplayReset` ficou em `Coordination`, `Policy`, `Discovery` e `Execution`.
- `Audio` e `Save` ficaram quebrados em subareas coerentes.
- `Save` hoje e uma superficie de hooks e contratos placeholder; `Progression` e `Checkpoint` ainda nao sao features finais.
- o seam de checkpoint manual existe apenas como contrato reservado
- `Gameplay/Camera` agora e `Experience/GameplayCamera`.
- `PostPlay`, `WorldLifecycle`, `ContentSwap` e `LevelManager` sao termos historicos.

## Servicos publicos que voce realmente chama

- `ILevelFlowRuntimeService`: owner atual `Orchestration/LevelLifecycle`.
- `IPostLevelActionsService`: owner atual `Orchestration/LevelLifecycle`.
- `IGameNavigationService`: owner atual `Orchestration/Navigation`.
- `IGameCommands`: owner atual `Orchestration/GameLoop/Commands`.
- `IActorGroupGameplayResetOrchestrator`: owner atual `Game/Gameplay/GameplayReset/Coordination`.
- `IIntroStageControlService`: owner atual `Orchestration/GameLoop/IntroStage`.
- `IPostStageControlService`: owner atual `Experience/PostRun/Handoff`.
- `ILoadingPresentationService`: owner atual `Orchestration/SceneFlow`.

## Assets canonicos atuais

- `Assets/Resources/NewScriptsBootstrapConfig.asset`
- `Assets/Resources/Navigation/GameNavigationCatalog.asset`
- `Assets/Resources/SceneFlow/SceneRouteCatalog.asset`
- `Assets/Resources/SceneFlow/Styles/TransitionStyle_Startup.asset`
- `Assets/Resources/SceneFlow/Styles/TransitionStyle_Frontend.asset`
- `Assets/Resources/SceneFlow/Styles/TransitionStyle_FrontendNoFade.asset`
- `Assets/Resources/SceneFlow/Styles/TransitionStyle_Gameplay.asset`
- `Assets/Resources/SceneFlow/Styles/TransitionStyle_GameplayNoFade.asset`
- `Assets/Resources/SceneFlow/LevelCollectionAsset.asset`

Outros assets canonicos usados por esses arquivos:
- `SceneRouteDefinitionAsset`
- `LevelDefinitionAsset`
- `SceneTransitionProfile`

## Loading de producao no macro flow

- `LoadingHudScene` continua sendo a HUD canonica de loading do macro flow.
- Ela apresenta barra, porcentagem, etapa e spinner.
- Ela nao decide a navegacao, nao executa reset e nao prepara level.
- O progresso atual e hibrido: parte real de load/unload e parte por marcos.

## Regras de producao

- `SceneFlow` decide a macro transicao.
- `LevelLifecycle` decide prepare, swap, restart, next, exit e intro local.
- `PostRun` decide handoff, ownership, result e presentation do pos-run.
- `GameLoop` decide run state, pause e outcome.
- `SceneResetFacade` e `FilteredEventBus.Legacy` continuam como compat, nao como alvo final.
- `Orchestration/LevelFlow/Runtime` continua de pe por transicao.

## Resumo final

Se voce quer usar os modulos principais em producao hoje:
- configure bootstrap, routes, styles e levels nos assets canonicos
- use `ILevelFlowRuntimeService` para abrir gameplay e trocar level local
- use `IGameNavigationService` para menu e navegacao macro
- use `IGameCommands` e `IPostLevelActionsService` para comandos de run e acoes do contexto atual
- trate `LoadingHudScene` como HUD canonica do macro flow
- use `IActorGroupGameplayResetOrchestrator` para GameplayReset local de atores
- mantenha `IntroStage` level-owned e `PostRun` global
- trate `Save` como rail oficial de hooks e nao como sistema final de progressao/checkpoint
- trate `IManualCheckpointRequestService` como seam reservado, nao como feature pronta

