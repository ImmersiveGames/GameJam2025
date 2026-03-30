# Modules

## Status documental

- Index ativo de `Docs/Modules/**`.
- Os roots fisicos atuais sao `Core`, `Orchestration`, `Game`, `Experience` e `Docs`.
- A documentacao mantem alguns nomes historicos, mas o owner real e o runtime atual.

## Leitura primaria

- `SceneFlow.md`
- `WorldReset.md`
- `ResetInterop.md`
- `LevelFlow.md`
- `GameLoop.md`
- `PostRun.md`
- `Gameplay.md`
- `Save.md`
- `Navigation.md`
- `SceneReset.md`
- `InputModes.md`

## Owners e seams atuais

- `Orchestration/LevelLifecycle`: owner operacional do lifecycle local.
- `Game/Content/Definitions/Levels`: owner de definitions/content de level.
- `Orchestration/GameLoop`: runtime core, outcome, pause, intro, commands e bridges.
- `Experience/PostRun`: handoff, ownership, result e presentation do pos-run.
- `Game/Gameplay/State`: `Core`, `RuntimeSignals` e `Gate`.
- `Game/Gameplay/GameplayReset`: `Coordination`, `Policy`, `Discovery` e `Execution`.
- `Experience/Audio`: `Runtime`, `Context`, `Semantics` e `Bridges`.
- `Experience/Save`: hook surface oficial, orchestration placeholder, `Progression`, `Checkpoint` e `Models` como placeholders de integracao.
- `Experience/GameplayCamera`: fronteira de camera fora de `Gameplay`.
- `Experience/Save` reserva `IManualCheckpointRequestService` como seam oficial para checkpoint manual futuro.

## Compatibilidade temporaria

- `Orchestration/LevelFlow/Runtime` continua vivo por transicao.
- `SceneResetFacade` e `FilteredEventBus.Legacy` continuam como compat, nao como alvo final.
- `Experience/Save` continua como superficie de hooks e contratos estaveis; `Progression` e `Checkpoint` ainda nao sao features finais.
- Namespaces antigos podem permanecer por seguranca ate a limpeza final.

## Normalizacao terminologica

- `WorldLifecycle` -> `WorldReset` + `SceneReset`
- `PostPlay` -> `PostRun`
- `LevelManager` -> `LevelLifecycle`
- `ContentSwap` -> historico / residual
- `LevelFlow` -> nome historico da fronteira local de lifecycle

