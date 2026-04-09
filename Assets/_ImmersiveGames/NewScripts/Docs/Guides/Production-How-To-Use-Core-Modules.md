# Production How-To Use Core Modules

## Status documental

- Superficie operacional atual: `phase-first`.
- `LevelFlow` permanece como nome historico da area, mas o fluxo ativo e `LevelLifecycle`/`PhaseDefinition`.
- Este guia descreve o uso atual, sem reabrir a arquitetura legada.

## Regra base

- Declaracao: asset de authoring phase-first.
- Runtime: service que resolve, valida e compoe.
- Operacional: UI, bridges e controllers que emitem intent ou chamam o runtime.

## Backbone atual

| Camada | Responsabilidade atual | Limite pratico |
|---|---|---|
| `SceneFlow` | rota macro, transition e loading | nao decide conteudo de phase |
| `LevelLifecycle` | prepare phase-first, swap local, intro, restart, next e exit | nao reabre backbone legado |
| `GameLoop` | run state, pause e outcome | nao assume ownership de conteúdo |
| `PostRun` | handoff, ownership, result e presentation do pos-run | nao faz spawn de gameplay |
| `Save` | orquestracao concreta de preferences, progression e checkpoint | nao e placeholder estreito |
| `WorldReset` | reset do mundo | nao substitui phase flow |

## Onde encaixar cada coisa

| Quero fazer isto | Use isto | Camada ativa |
|---|---|---|
| Abrir o gameplay padrao | `ILevelFlowRuntimeService.StartGameplayDefaultAsync(reason, ct)` | `LevelLifecycle` |
| Trocar o conteudo local da phase | `ILevelFlowRuntimeService.SwapLevelLocalAsync(PhaseDefinitionSelectedEvent, reason, ct)` | `LevelLifecycle` |
| Reiniciar a run | `IPostLevelActionsService.RestartLevelAsync(reason, ct)` ou `IGameCommands.RequestRestart(reason)` | `LevelLifecycle` / `GameLoop` |
| Ir para o menu | `IGameNavigationService.GoToMenuAsync(reason)` ou `IPostLevelActionsService.ExitToMenuAsync(reason, ct)` | `Navigation` / `LevelLifecycle` |
| Ir para a proxima phase | `IPostLevelActionsService.NextLevelAsync(reason, ct)` | `LevelLifecycle` |
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
- `PhaseDefinitionSelectedEvent` indica a phase selecionada para o fluxo atual.
- `PhaseResetContext` carrega a identidade phase-first do reset corrente.
- `PhaseIntroStageEntryEvent` dispara a entrada da intro phase-first.
- `PhaseIntroCompletedEvent` indica que a intro da phase terminou ou foi pulada.
- `PostStageStartRequestedEvent`, `PostStageStartedEvent` e `PostStageCompletedEvent` cobrem o handoff do pos-run.
- `PostRunEnteredEvent` indica entrada formal no pos-run.
- `GameRunEndRequestedEvent`, `GameResetRequestedEvent` e `GameExitToMenuRequestedEvent` cobrem intencoes de fim, restart e saida.

## Authoring phase-first

- `PhaseDefinitionAsset` e o owner autoral do fluxo atual.
- `PhaseDefinitionCatalog` resolve a phase canonica da rota.
- `PhaseDefinitionContentManifest` e a declaracao de content da phase.
- `PhaseDefinitionAsset.Swap` define o payload operacional de swap local.
- `PhaseDefinitionAsset.Continuity` define a proxima phase.
- `LevelDefinitionAsset`, `LevelCollectionAsset`, `LevelSelectedEvent` e `LevelEnteredEvent` permanecem apenas como historia/compat em docs antigos e reports arquivados.
- A observabilidade minima do manifesto e o log de aceitacao com `phaseRef`, quantidade de entries, ids, roles e estado vazio.

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
- `Assets/Resources/SceneFlow/Styles/TransitionStyle_Startup.asset`
- `Assets/Resources/SceneFlow/Styles/TransitionStyle_Frontend.asset`
- `Assets/Resources/SceneFlow/Styles/TransitionStyle_FrontendNoFade.asset`
- `Assets/Resources/SceneFlow/Styles/TransitionStyle_Gameplay.asset`
- `Assets/Resources/SceneFlow/Styles/TransitionStyle_GameplayNoFade.asset`

## Resumo de uso

- Use `LevelLifecycle` para o fluxo ativo phase-first.
- Use `SceneFlow` para rota macro e loading.
- Use `GameLoop` para run, pause e outcome.
- Use `PostRun` para handoff final.
- Use `Save` como runtime concreto.
- Trate `LevelFlow` como nome historico da area, nao como owner principal do modelo legado.
