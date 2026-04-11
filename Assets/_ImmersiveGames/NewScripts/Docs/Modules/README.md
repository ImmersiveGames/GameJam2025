# Modules

## Status documental

- Index ativo de `Docs/Modules/**`.
- Os roots fisicos atuais sao `Core`, `Orchestration`, `Game`, `Experience` e `Docs`.
- A documentacao ativa deve refletir o canon vivo; nomes historicos aparecem apenas como aliases ou notas de migracao.

## Leitura primaria

- `Gameplay.md`
- `SceneFlow.md`
- `WorldReset.md`
- `ResetInterop.md`
- `GameLoop.md`
- `Navigation.md`
- `Save.md`
- `SceneReset.md`
- `InputModes.md`

## Owners atuais

- `Gameplay/PhaseDefinition`: composicao semantica do gameplay, selecao de phase, runtime materializado e handoff de fase.
- `Orchestration/SceneFlow`: transicao macro, route policy, loading e completion gate.
- `Orchestration/Navigation`: dispatch macro de intents.
- `Orchestration/GameLoop`: loop, pause, intro handoff e outcome terminal.
- `Experience/PostRun`: rail de `RunResultStage` e `RunDecision` com presentation local.
- `Game/Gameplay/State`: `Core`, `RuntimeSignals` e `Gate`.
- `Game/Gameplay/GameplayReset`: `Coordination`, `Policy`, `Discovery` e `Execution`.
- `Experience/Audio`: `Runtime`, `Context`, `Semantics` e `Bridges`.
- `Experience/Save`: hook surface oficial, orchestration placeholder, `Progression`, `Checkpoint` e `Models` como placeholders de integracao.
- `Experience/GameplayCamera`: fronteira de camera fora de `Gameplay`.

## Historico e compatibilidade temporaria

- `LevelFlow`, `LevelLifecycle`, `LevelManager` e `ContentSwap` sao nomes historicos.
- `SceneResetFacade` e `FilteredEventBus.Legacy` continuam como compat historica quando o runtime ainda precisa deles.
- `Experience/Save` continua como superficie de hooks e contratos estaveis; `Progression` e `Checkpoint` ainda nao sao features finais.

## Historico fisico separado

- `Archive/Modules/LevelFlow.md`
- `Archive/Modules/PostRun.md`

## Normalizacao terminologica

- `WorldLifecycle` -> `WorldReset` + `SceneReset`
- `PostPlay` -> `PostRun`
- `LevelManager` -> historico / residual
- `LevelLifecycle` -> historico / seam operacional
- `ContentSwap` -> historico / residual
- `LevelFlow` -> nome historico da fronteira local de lifecycle
