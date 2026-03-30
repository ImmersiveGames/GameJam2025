# Event Hooks Reference

## Status documental

- Parcial / leitura junto do runtime atual.
- Esta e a referencia operacional dos hooks ativos.
- Nomes residuais antigos continuam marcados como historicos quando aparecerem.

## Regra simples

- hooks operacionais: primeira escolha para UI, gameplay e systems
- hooks tecnicos: existem no runtime, mas nao sao a primeira escolha de integracao
- `Exit` continua resultado formal do `PostRun` global, mas nao tem evento operacional promovido dedicado
- `Restart` nao passa por post hook
- `LevelFlow` aqui significa a fronteira historica; o owner atual e `Orchestration/LevelLifecycle`

## Mapa rapido

| Se voce quer... | Use este hook | Publisher atual | Use para |
|---|---|---|---|
| saber que a troca de rota terminou | `SceneTransitionCompletedEvent` | `Orchestration/SceneFlow` | UI e systems que dependem da rota ja aplicada |
| saber que o reset completo terminou | `WorldResetCompletedEvent` | `Orchestration/WorldReset` | systems que precisam do mundo pronto |
| saber que a run comecou | `GameRunStartedEvent` | `Orchestration/GameLoop/RunLifecycle` | ligar comportamento de gameplay ativo |
| saber que a run terminou | `GameRunEndedEvent` | `Orchestration/GameLoop/RunOutcome` | iniciar o `PostStage` antes do handoff final |
| saber que um level entrou no fluxo | `LevelSelectedEvent` | `Orchestration/LevelLifecycle` | UI e systems ligados ao level atual |
| saber que a troca local terminou | `LevelSwapLocalAppliedEvent` | `Orchestration/LevelLifecycle` | atualizar HUD e cameras apos swap |
| saber que o level ja foi aplicado e esta ativo | `LevelEnteredEvent` | `Orchestration/LevelLifecycle` | seams level-owned, incluindo IntroStage |
| saber que a intro do level terminou | `LevelIntroCompletedEvent` | `Orchestration/LevelLifecycle` e `Orchestration/GameLoop/IntroStage` | handoff level->gameplay apos intro |
| saber que o pause vai entrar | `PauseWillEnterEvent` | `Orchestration/GameLoop/Pause` | reagir cedo antes da entrada final em pause |
| saber que o pause vai sair | `PauseWillExitEvent` | `Orchestration/GameLoop/Pause` | reagir cedo antes da saida final de pause |
| saber que o estado de pause mudou | `PauseStateChangedEvent` | `Orchestration/GameLoop/Pause` | tratar o estado final de pause sem depender do overlay |
| saber que o PostStage foi pedido | `PostStageStartRequestedEvent` | `Experience/PostRun/Handoff` | iniciar fase de validacao pos-outcome |
| saber que o PostStage foi assumido | `PostStageStartedEvent` | `Experience/PostRun/Handoff` | mostrar presenter opcional da cena atual |
| saber que o PostStage terminou | `PostStageCompletedEvent` | `Experience/PostRun/Handoff` | liberar o handoff final para `IPostRunHandoffService` |
| saber que o `PostRun` entrou | `PostRunEnteredEvent` | `Experience/PostRun/Ownership` | abrir overlay e aplicar ownership do pos-game |
| persistir progresso no boot | `SaveInstaller.Install` + `IProgressionSaveService.TryLoad(...)` | `Experience/Save` | restaurar ou seedar `ProgressionSnapshot` no bootstrap |
| reservar checkpoint manual | `IManualCheckpointRequestService` | `Experience/Save` | seam oficial para futura integracao manual |
| observar pedido de fim de run | `GameRunEndRequestedEvent` | `Orchestration/GameLoop/RunOutcome` | auditoria, telemetria e bridges |
| observar restart macro | `GameResetRequestedEvent` | `Orchestration/GameLoop/Commands` | ouvir intencao de restart |
| observar saida para menu | `GameExitToMenuRequestedEvent` | `Orchestration/GameLoop/Commands` | ouvir intencao de exit |

## Hooks operacionais recomendados

### `SceneTransitionCompletedEvent`

Quem publica: `Orchestration/SceneFlow`.

Quando dispara: no fim da transicao macro, com a rota ja aplicada.

### `WorldResetCompletedEvent`

Quem publica: `Orchestration/WorldReset`.

Quando dispara: quando o reset deterministico do mundo concluiu.

### `GameRunEndedEvent`

Quem publica: `Orchestration/GameLoop/RunOutcome`.

Quando dispara: quando o fim de run terminal foi aceito em `Playing`.

### `LevelSelectedEvent`

Quem publica: `Orchestration/LevelLifecycle`.

Quando dispara: quando um `LevelDefinitionAsset` e selecionado para o fluxo atual.

### `LevelIntroCompletedEvent`

Quem publica: `Orchestration/LevelLifecycle` e `Orchestration/GameLoop/IntroStage`.

Quando dispara: quando a intro conclui ou e pulada de forma canonica.

### `GameRunEndRequestedEvent`

Quem publica: `Orchestration/GameLoop/RunOutcome`.

Quando dispara: quando alguem pede `Victory` ou `Defeat`.

### `SaveInstaller.Install` + `IProgressionSaveService.TryLoad(...)`

Quem publica: `Experience/Save`.

Quando dispara: no bootstrap, para restaurar `ProgressionSnapshot` se houver snapshot salvo.

### `IManualCheckpointRequestService`

Quem publica: `Experience/Save`.

Quando dispara: reservado; nao esta wireado no runtime atual.

### `GameResetRequestedEvent` / `GameExitToMenuRequestedEvent`

Quem publica: `Orchestration/GameLoop/Commands`.

Quando dispara: quando alguem pede restart ou saida para menu.

## Hooks tecnicos do pipeline

- `SceneTransitionStartedEvent`
- `SceneTransitionFadeInCompletedEvent`
- `SceneTransitionScenesReadyEvent`
- `SceneTransitionBeforeFadeOutEvent`
- `WorldResetResetStartedEvent`
- `InputModeRequestEvent`

Use esses hooks apenas quando o caso realmente depender do ponto tecnico do pipeline.

## O que nao existe como hook operacional principal

- nao existe hook publico promovido para o hook opcional de post por level
- nao existe post stage generico por level
- `Restart` nao passa por post hook
- nao existe checkpoint funcional de jogo finalizado; o seam de checkpoint e reservado como placeholder

