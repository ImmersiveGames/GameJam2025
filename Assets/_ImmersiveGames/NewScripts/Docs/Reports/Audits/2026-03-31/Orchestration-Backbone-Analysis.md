# Orchestration Backbone Analysis

**Data:** 31 de marco de 2026  
**Escopo:** `Assets/_ImmersiveGames/NewScripts/Orchestration/**` cruzado com `Game/**`  
**Objetivo:** registrar o funcionamento real do backbone operacional, com foco em fluxo, acessos, restart, spawn e papel de `SceneReset`.

## Resumo executivo

- O backbone real hoje e formado por `GameLoop`, `SceneFlow`, `ResetInterop`, `WorldReset` e `LevelLifecycle`.
- `SceneFlow` cuida da transicao macro de cena e da decisao de reset por rota.
- `ResetInterop` e a ponte que liga o fim do carregamento de cena ao `WorldReset`.
- `WorldReset` e o orquestrador canonico do reset macro/level.
- `SceneReset` nao e o owner do reset macro; ele executa o reset local/material dentro da cena.
- O acoplamento mais forte com `Game` esta em `LevelFlowContentService`, `LevelCollectionAsset`, `LevelDefinitionAsset`, `PlayerSpawnService`, `EaterSpawnService`, `GameplayStateGate` e `PlayerActorGroupGameplayResetWorldParticipant`.
- Leitura honesta: `SceneReset` hoje parece mais um `local world reset executor/pipeline` do que um simples reset de cena.

## Metodologia

- Foram priorizados os modulos:
  - `Orchestration/SceneFlow`
  - `Orchestration/WorldReset`
  - `Orchestration/SceneReset`
  - `Orchestration/ResetInterop`
  - `Orchestration/Navigation`
  - `Orchestration/LevelLifecycle`
  - `Orchestration/GameLoop`
  - `Orchestration/SceneComposition`
- O cruzamento com `Game` foi feito em:
  - `Game/Content/Definitions/Levels`
  - `Game/Gameplay/Spawn`
  - `Game/Gameplay/State`
  - `Game/Gameplay/GameplayReset`

## Evidencias principais

- `Orchestration/SceneFlow/Bootstrap/SceneFlowBootstrap.cs`
- `Orchestration/SceneFlow/Transition/Runtime/SceneTransitionService.cs`
- `Orchestration/SceneFlow/Readiness/Runtime/GameReadinessService.cs`
- `Orchestration/Navigation/GameNavigationService.cs`
- `Orchestration/LevelLifecycle/Bootstrap/LevelFlowBootstrap.cs`
- `Orchestration/LevelLifecycle/Runtime/LevelFlowRuntimeService.cs`
- `Orchestration/LevelLifecycle/Runtime/LevelMacroPrepareService.cs`
- `Orchestration/LevelLifecycle/Runtime/LevelSwapLocalService.cs`
- `Orchestration/LevelLifecycle/Runtime/PostLevelActionsService.cs`
- `Orchestration/LevelLifecycle/Runtime/RestartContextService.cs`
- `Orchestration/LevelLifecycle/Interop/LevelSelectedRestartSnapshotBridge.cs`
- `Orchestration/WorldReset/Application/WorldResetService.cs`
- `Orchestration/WorldReset/Application/WorldResetOrchestrator.cs`
- `Orchestration/WorldReset/Runtime/WorldResetCommands.cs`
- `Orchestration/WorldReset/Runtime/WorldResetRequestService.cs`
- `Orchestration/WorldReset/Policies/SceneRouteResetPolicy.cs`
- `Orchestration/ResetInterop/Runtime/SceneFlowWorldResetDriver.cs`
- `Orchestration/ResetInterop/Runtime/WorldResetCompletionGate.cs`
- `Orchestration/SceneReset/Runtime/SceneResetFacade.cs`
- `Orchestration/SceneReset/Runtime/SceneResetPipeline.cs`
- `Orchestration/SceneReset/Bindings/SceneResetController.cs`
- `Orchestration/SceneReset/Bindings/SceneResetRunner.cs`
- `Orchestration/SceneReset/Bindings/SceneResetRuntimeFactory.cs`
- `Orchestration/SceneReset/Bindings/SceneResetRequestQueue.cs`
- `Orchestration/SceneReset/Hooks/SceneResetHookRegistry.cs`
- `Orchestration/SceneReset/Spawn/IWorldSpawnService.cs`
- `Game/Content/Definitions/Levels/Config/LevelCollectionAsset.cs`
- `Game/Content/Definitions/Levels/Config/LevelDefinitionAsset.cs`
- `Game/Content/Definitions/Levels/Runtime/LevelDefinition.cs`
- `Game/Gameplay/Spawn/PlayerSpawnService.cs`
- `Game/Gameplay/Spawn/EaterSpawnService.cs`
- `Game/Gameplay/State/Gate/GameplayStateGate.cs`
- `Game/Gameplay/GameplayReset/Integration/PlayerActorGroupGameplayResetWorldParticipant.cs`
- `Game/Gameplay/GameplayReset/Coordination/ActorGroupGameplayResetOrchestrator.cs`

## Mapa dos modulos de Orchestration

| modulo | o que faz hoje | como entra no fluxo | como sai do fluxo | quem chama | quem ele chama | eventos/hooks/bridges relevantes | relacao com `Game` |
|---|---|---|---|---|---|---|---|
| `SceneFlow` | transicao macro de cena, loading, active scene, fade e readiness gate | entra por `SceneTransitionRequest` vindo de `Navigation` | conclui em `SceneTransitionCompletedEvent` | `GameNavigationService` | `ISceneCompositionExecutor`, `IFadeService`, `IRouteResetPolicy`, `WorldResetCompletionGate` | `SceneTransitionStartedEvent`, `SceneTransitionScenesReadyEvent`, `SceneTransitionCompletedEvent`, `SceneFlowRouteLoadingProgressEvent` | decide se a rota gameplay exige reset |
| `WorldReset` | orquestracao canonica do reset macro/level com policy, guard, discovery, executor e validator | entra por `WorldResetRequest` | termina em `WorldResetCompletedEvent` | `ResetInterop`, `WorldResetCommands`, `WorldResetRequestService` | `WorldResetOrchestrator`, executores locais, validators | `WorldResetStartedEvent`, `WorldResetCompletedEvent`, `IWorldResetLocalExecutor` | conecta transicao com reconstituicao da gameplay |
| `SceneReset` | reset local/material: gate, hooks, despawn, scoped reset, spawn | entra como executor local de `WorldReset` | sai depois de despawn/spawn/hooks | `WorldResetLocalExecutorLocator`, `SceneResetController` | `IWorldSpawnService`, hooks, `IActorGroupGameplayResetWorldParticipant` | `SceneResetPipeline`, `SceneResetHookRegistry`, gate lease | executa o lado material dos spawns de `Game` |
| `ResetInterop` | ponte entre `SceneFlow` e `WorldReset` | entra quando `SceneTransitionScenesReadyEvent` dispara | libera completion gate ou encaminha reset | `SceneTransitionService` | `WorldResetService`, `WorldResetCompletionGate` | `SceneFlowWorldResetDriver`, `WorldResetTokens` | faz a costura entre scene transition e reset |
| `Navigation` | despacho de rotas de menu e gameplay | entra por intents de UI/loop/level lifecycle | sai em `SceneTransitionRequest` | UI, `LevelLifecycle`, `GameLoopCommands` | `SceneFlow` | valida `RouteKind`, `LevelCollection` | usa `Game/Content/Definitions/Levels` para validar gameplay |
| `LevelLifecycle` | selecao de level, snapshot de restart, swap local, restart e post-level actions | entra apos escolha de rota gameplay ou actions de restart | sai em `StartGameplayRouteAsync`, `SwapLocalAsync`, `GoToMenuAsync` | `GameLoopCommands`, `SceneFlow`, UI de nivel | `Navigation`, `WorldResetCommands`, `SceneComposition`, `RestartContextService` | `LevelSelectedEvent`, `LevelEnteredEvent`, `GameplayStartSnapshot`, bridge de snapshot | e a ponte mais direta entre conteudo e runtime |
| `GameLoop` | lifecycle da run: start, pause, ready, reset, outcome e sync com SceneFlow | entra por start request, intro completed e outcome/reset request | termina em `GameRunStartedEvent` / `GameRunEndedEvent` | bootstrap e bridges | `IPostLevelActionsService`, `IGameNavigationService`, `IGameRunEndRequestService`, `IGameRunOutcomeService` | `GameStartRequestedEvent`, `GameRunStartedEvent`, `GameRunEndedEvent`, `PauseStateChangedEvent` | espinha da run acima do fluxo de level |
| `SceneComposition` | aplica ou limpa composicao additive do level | entra por `LevelLifecycle` | sai apos apply/clear | `LevelMacroPrepareService`, `LevelSwapLocalService` | `ISceneCompositionExecutor` | request factories de apply/clear | consome `LevelCollection` e `LevelDefinition` de `Game` |

## Fluxo macro atual

1. `GameLoopBootstrap` compoe o runtime da run.
2. `GameLoopService` recebe start/ready/pause/reset/end.
3. `GameNavigationService` envia a rota gameplay para `SceneFlow`.
4. `SceneTransitionService` carrega a cena e publica `SceneTransitionScenesReadyEvent`.
5. `ResetInterop/SceneFlowWorldResetDriver` decide se a rota exige reset.
6. Se exigir, `WorldResetService` executa o reset.
7. `WorldReset` encontra o executor local, que e o `SceneResetController`.
8. `SceneReset` despawna, roda hooks, executa resets de participantes e faz spawn.
9. `WorldResetCompletedEvent` libera o `WorldResetCompletionGate`.
10. `SceneFlow` completa a transicao.
11. `GameLoop` entra em estado jogavel e o `GameplayStateGate` libera as acoes.

## Fluxo de restart

| origem | caminho real | entra em `WorldReset`? | entra em `SceneReset`? | entra em `LevelLifecycle`? | entra em `GameLoop`? |
|---|---|---|---|---|---|
| `GameLoopCommands.RequestRestart()` | `GameLoopCommands` -> `PostLevelActionsService.RestartLevelAsync()` -> `LevelFlowRuntimeService.RestartLastGameplayAsync()` -> `Navigation.StartGameplayRouteAsync()` -> `SceneFlow` -> `ResetInterop` -> `WorldReset` -> `SceneReset` | sim | sim | sim | indireto |
| `RestartFromFirstLevelAsync()` | `PostLevelActionsService` -> `LevelFlowRuntimeService.RestartFromFirstLevelAsync()` -> limpa `RestartContextService` -> start gameplay default | sim, se a rota exigir reset | sim | sim | indireto |
| `ResetCurrentLevelAsync()` | `LevelFlowRuntimeService.ResetCurrentLevelAsync()` -> `LevelSwapLocalService.SwapLocalAsync()` -> `WorldResetCommands.ResetLevelAsync()` | sim, via command local | sim | sim | nao diretamente |
| `SceneFlow` para rota gameplay | `SceneTransitionScenesReadyEvent` -> `ResetInterop` -> `WorldResetService` | sim, quando a policy pede | sim | sim | nao diretamente |

### Diagrama de restart

```mermaid
flowchart LR
  A[Restart request] --> B[GameLoopCommands / PostLevelActions]
  B --> C[LevelFlowRuntimeService]
  C --> D[Navigation start gameplay]
  D --> E[SceneFlow transition]
  E --> F[ResetInterop]
  F --> G[WorldReset]
  G --> H[SceneReset local executor]
  H --> I[Spawn / rebind / hooks]
  I --> J[WorldResetCompletedEvent]
  J --> K[SceneTransitionCompletedEvent]
  K --> L[GameLoop ready]
```

## Fluxo de spawn e reconstituicao

| etapa | o que acontece | quem faz | relacao com `Game` |
|---|---|---|---|
| resolucao de conteudo | `LevelFlowContentService` resolve `LevelCollectionAsset` e `LevelDefinitionAsset` | `LevelLifecycle` | consome `Game/Content/Definitions/Levels` |
| selecao de level | escolhe level default ou snapshot salvo | `LevelMacroPrepareService` | define o level real que vai existir na run |
| snapshot de restart | grava `GameplayStartSnapshot` a partir de `LevelSelectedEvent` | `LevelSelectedRestartSnapshotBridge` + `RestartContextService` | preserva o ponto canonico de restart |
| reset do nivel | chama `WorldResetCommands.ResetLevelAsync(...)` | `LevelMacroPrepareService` / `LevelSwapLocalService` | prepara o mundo para o nivel selecionado |
| composicao de cena | aplica additive scenes do level | `SceneCompositionExecutor` via `LevelLifecycle` | usa definicoes de level |
| spawn material | executa `IWorldSpawnService.SpawnAsync()` | `SceneReset` | aqui entram `PlayerSpawnService`, `EaterSpawnService` e outros spawn services de `Game` |
| reset de gameplay por ator | executa cleanup/restore/rebind em participantes | `SceneReset` + `GameplayReset` | `PlayerActorGroupGameplayResetWorldParticipant` entra aqui |

## Analise especifica de `SceneReset`

### O que o nome sugere

- Parece um reset de cena simples.
- Sugere algo local, direto e limitado a scene.

### O que o conteudo faz de verdade

- Mantem um pipeline local com gate, hooks, despawn, scoped reset e spawn.
- Controla serializacao de requests.
- Integra `IWorldSpawnService`, `SceneResetHookRegistry` e participantes de gameplay reset.
- E o executor material que o `WorldReset` encontra e usa.

### Onde entra no fluxo

- Entra depois que `WorldReset` decide executar o reset.
- Tambem pode ser acionado diretamente pelo `SceneResetController`.
- E parte do lado operacional que reconstroi o mundo vivo.

### Que papel ele cumpre

- Nao e owner do reset macro.
- Nao e apenas um helper.
- E o executor local do reset que materializa a troca de estado.

### Com quem conversa

- `WorldReset`
- `IWorldResetLocalExecutor`
- `IWorldSpawnServiceRegistry`
- `SceneResetHookRegistry`
- `IActorGroupGameplayResetWorldParticipant`
- `ISimulationGateService`

### Leitura honesta

- O nome atual nao e falso, mas e curto demais para a responsabilidade real.
- A leitura mais fiel e `local world reset executor/pipeline`.
- Se houver rename futuro, essa e a semantica mais honesta.

## Mapa de acessos

| modulo A | modulo B | forma de acesso | motivo | leitura de saude |
|---|---|---|---|---|
| `GameNavigationService` | `SceneFlow` | chamada direta | iniciar rotas | saudavel |
| `SceneFlow` | `WorldReset` | bridge + gate | reset apos load | saudavel |
| `WorldResetService` | `SceneReset` | discovery de executor local | executar reset material | saudavel |
| `LevelMacroPrepareService` | `WorldResetCommands` | chamada direta | reset do level selecionado | coerente |
| `LevelMacroPrepareService` | `SceneComposition` | chamada direta | aplicar composicao additive | saudavel |
| `LevelSwapLocalService` | `WorldResetCommands` + `SceneComposition` | chamada direta | trocar level localmente | coerente, mas concentrado |
| `RestartContextService` | `LevelSelectedRestartSnapshotBridge` | evento/bridge | capturar snapshot canonico | saudavel |
| `SceneResetController` | `IWorldSpawnServiceRegistry` | registry + execucao | spawn/despawn por cena | saudavel |
| `SceneResetController` | `SceneResetHookRegistry` | registry | extensao de reset | saudavel |
| `SceneResetController` | `GameplayReset` participant | interface + execucao | soft reset de escopos | saudavel |
| `GameplayStateGate` | `GameLoop` | eventos + estado | bloquear/liberar gameplay | saudavel |
| `GameLoopSceneFlowSyncCoordinator` | `SceneFlow` + `WorldReset` | bridge de sincronizacao | liberar ready so depois de transicao/reset | saudavel |

## Relacao com `Game`

| modulo de Orchestration | modulo de Game | tipo de relacao | quem depende de quem | ponto do fluxo |
|---|---|---|---|---|
| `LevelFlowContentService` | `Game/Content/Definitions/Levels` | service contract | `Orchestration` depende de `Game` | selecao e validacao de level |
| `LevelMacroPrepareService` | `LevelCollectionAsset` / `LevelDefinitionAsset` | leitura de conteudo | `Orchestration` depende de `Game` | prepare/restart |
| `LevelSwapLocalService` | `LevelCollectionAsset` / `LevelDefinitionAsset` | leitura de conteudo | `Orchestration` depende de `Game` | troca local de level |
| `PlayerSpawnService` | `GameplayStateGate` | acoplamento funcional | `Game` depende do runtime | spawn do player |
| `EaterSpawnService` | `GameplayStateGate` | acoplamento funcional | `Game` depende do runtime | spawn do inimigo/ator |
| `GameplayStateGate` | `GameLoop` | eventos/estado | `Game` depende de `Orchestration` | gate de gameplay |
| `ActorGroupGameplayResetOrchestrator` | `IWorldResetPolicy` | adapter de policy | `Game` reaproveita policy do reset | reset de grupo/ator |
| `PlayerActorGroupGameplayResetWorldParticipant` | `SceneReset` | bridge/participant | `Game` entra no reset local | soft reset de players |
| `GameLoopCommands` | `IPostLevelActionsService` | chamada direta | `GameLoop` depende de `LevelLifecycle` | restart/exit |
| `GameLoopSceneFlowSyncCoordinator` | `SceneFlowWorldResetDriver` / `WorldResetCompletedEvent` | bridge | `GameLoop` depende do fluxo concluir | liberação de ready |

## Conclusao objetiva

- Backbone real de `Orchestration`: `GameLoop` + `SceneFlow` + `ResetInterop` + `WorldReset` + `LevelLifecycle`.
- Papel real de `SceneReset`: executor local/material do reset, nao owner do reset macro.
- `SceneReset` merece revisao de nome em momento futuro.
- Os 3 pontos mais importantes para entender restart/spawn:
  1. `LevelLifecycle` decide level e snapshot.
  2. `ResetInterop` conecta scene transition ao reset.
  3. `SceneReset` executa o material do reset, inclusive spawn.
- Melhor leitura curta do fluxo atual: `GameLoop` dispara, `Navigation` abre a rota, `SceneFlow` carrega, `ResetInterop` decide reset, `WorldReset` orquestra, `SceneReset` materializa, `LevelLifecycle` preserva estado de restart.
