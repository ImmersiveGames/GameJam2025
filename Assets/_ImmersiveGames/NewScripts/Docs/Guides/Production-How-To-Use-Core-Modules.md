# Production How-To Use Core Modules

## Status documental

- Operacional e alinhado ao runtime atual.
- Este guia descreve a superficie publica real, sem reabrir arquitetura.
- `LevelFlow` aparece aqui como seam historico; a camada ativa e `LevelLifecycle` quando o assunto e fluxo de level.

## Regra base

- Declaracao: asset authoring.
- Runtime: service que resolve, valida e compoe.
- Operacional: UI, bridges e controllers que emitem intent ou chamam o runtime.

## Backbone atual

| Camada | Responsabilidade atual | Limite pratico |
|---|---|---|
| `SceneFlow` | rota macro, transition e loading | nao decide conteudo de level |
| `LevelLifecycle` | entrada do level, swap local, intro, restart, next e exit | nao reabre backbone |
| `GameLoop` | run state, pause e outcome | nao assume ownership de conteudo |
| `PostRun` | handoff, ownership, result e presentation do pos-run | nao faz spawn de gameplay |
| `Save` | orquestracao concreta de preferences, progression e checkpoint | nao e placeholder estreito |
| `WorldReset` | reset do mundo | nao substitui level flow |

## Onde encaixar cada coisa

| Quero fazer isto | Use isto | Camada ativa |
|---|---|---|
| Abrir o gameplay padrao | `ILevelFlowRuntimeService.StartGameplayDefaultAsync(reason, ct)` | `LevelLifecycle` |
| Trocar o level local | `ILevelFlowRuntimeService.SwapLevelLocalAsync(levelRef, reason, ct)` | `LevelLifecycle` |
| Reiniciar a run | `IPostLevelActionsService.RestartLevelAsync(reason, ct)` ou `IGameCommands.RequestRestart(reason)` | `LevelLifecycle` / `GameLoop` |
| Ir para o menu | `IGameNavigationService.GoToMenuAsync(reason)` ou `IPostLevelActionsService.ExitToMenuAsync(reason, ct)` | `Navigation` / `LevelLifecycle` |
| Ir para o proximo level | `IPostLevelActionsService.NextLevelAsync(reason, ct)` | `LevelLifecycle` |
| Fechar a intro atual | `IIntroStageControlService.CompleteIntroStage(reason)` | `LevelLifecycle` / `GameLoop` |
| Validar o PostStage atual | `IPostStageControlService.TryComplete(reason)` / `TrySkip(reason)` | `PostRun` |
| Atualizar loading | `ILoadingPresentationService.SetProgress(signature, snapshot)` | `SceneFlow` |
| Reset local de atores | `IActorGroupGameplayResetOrchestrator.RequestResetAsync(request)` | `GameplayReset` |
| Consumir save atual | `ISaveOrchestrationService.TryHandleGameRunEnded(...)`, `TryHandleWorldResetCompleted(...)`, `TryHandleSceneTransitionCompleted(...)` | `Save` |

## Hooks canonicos

- `BootStartPlanRequestedEvent` vem do bootstrap de cena e inicia o handshake entre `SceneFlow` e `GameLoop`.
- `GamePlayRequestedEvent` vem do frontend e expressa a intent do usuario para entrar em gameplay.
- `SceneTransitionCompletedEvent` indica que a rota macro terminou de aplicar.
- `WorldResetCompletedEvent` indica que o reset do mundo terminou.
- `GameRunStartedEvent` indica que a run ficou ativa.
- `GameRunEndedEvent` indica que o fim terminal da run foi aceito.
- `LevelSelectedEvent` indica que um `LevelDefinitionAsset` foi selecionado para o fluxo atual.
- `LevelSwapLocalAppliedEvent` indica que o swap local ja foi aplicado.
- `LevelEnteredEvent` indica que o level esta ativo.
- `LevelIntroCompletedEvent` indica que a intro do level terminou ou foi pulada.
- `PostStageStartRequestedEvent`, `PostStageStartedEvent` e `PostStageCompletedEvent` cobrem o handoff do pos-run.
- `PostRunEnteredEvent` indica entrada formal no pos-run.
- `GameRunEndRequestedEvent`, `GameResetRequestedEvent` e `GameExitToMenuRequestedEvent` cobrem intencoes de fim, restart e saida.

## Manifesto declarativo por level

- `LevelDefinitionAsset` e o owner autoral do level.
- `GameplayContentManifest` e a declaracao level-scoped.
- `GameplayContentEntry` e a unidade declarativa minima.
- `LevelFlowContentService` resolve e valida o manifesto no boundary canonico.
- `Level1` ja tem authoring real de teste com `player_main`, `eater_aux` e `dummy_prototype`.
- A observabilidade minima do manifesto e o log de aceitacao com `levelRef`, quantidade de entries, ids, roles e estado vazio.

## O que esta fora dessa camada

- spawn.
- registry.
- reset e reconstituicao.
- materializacao operacional.
- taxonomia final.

## Assets canonicos atuais

- `Assets/Resources/BootstrapConfig.asset`
- `Assets/Resources/Navigation/GameNavigationCatalog.asset`
- `Assets/Resources/SceneFlow/SceneRouteCatalog.asset`
- `Assets/Resources/SceneFlow/LevelCollectionAsset.asset`
- `Assets/Resources/SceneFlow/Level1.asset`
- `Assets/Resources/SceneFlow/Styles/TransitionStyle_Startup.asset`
- `Assets/Resources/SceneFlow/Styles/TransitionStyle_Frontend.asset`
- `Assets/Resources/SceneFlow/Styles/TransitionStyle_FrontendNoFade.asset`
- `Assets/Resources/SceneFlow/Styles/TransitionStyle_Gameplay.asset`
- `Assets/Resources/SceneFlow/Styles/TransitionStyle_GameplayNoFade.asset`

## Resumo de uso

- Use `LevelLifecycle` para o fluxo de level ativo.
- Use `SceneFlow` para rota macro e loading.
- Use `GameLoop` para run, pause e outcome.
- Use `PostRun` para handoff final.
- Use `Save` como runtime concreto.
- Trate `LevelFlow` como seam historico, nao como owner principal.
