# Modules

## Status documental

- Index ativo de `Docs/Modules/**`.
- Os roots fisicos atuais sao `Core`, `Orchestration`, `Game`, `Experience` e `Docs`.
- A documentacao ativa deve refletir o canon vivo; nomes historicos aparecem apenas como aliases ou notas de migracao.
- A leitura operacional atual do sistema e `Base 1.0`: baseline tecnico fino, `Session Integration` e camadas semanticas acima do baseline.

## Base 1.0 operacional

| Camada | ADR | Papel |
| --- | --- | --- |
| Leitura composta do sistema | `ADR-0057` | referencia operacional primaria |
| Baseline tecnico fino | `ADR-0056` | owner tecnico fino do baseline |
| Session Integration | `ADR-0055` | seam explicito acima do baseline |
| Antecedentes semanticos e de composicao | `ADR-0045`, `ADR-0046`, `ADR-0047`, `ADR-0052` | base de leitura para gameplay composition e session transition |

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
- `Orchestration/SessionIntegration`: seam operacional de sessao, com bridges, translators e request publishers finos.
- `Orchestration/SessionIntegration` tambem e o emissor canonico de intencao para seams adjacentes da sessao, incluindo `InputModes`.
- `Orchestration/SessionIntegration` tambem e o ponto de crescimento preparado para futuros eixos como `Actors`, `BindersAndInteractions`, expansao de `SessionTransition` e novos blocos semanticos acima do baseline.
- `Orchestration/SceneFlow`: transicao macro, route policy, loading e completion gate.
- `Orchestration/Navigation`: dispatch macro de intents.
- `Orchestration/GameLoop`: executor operacional do loop, pause, intro handoff e outcome terminal.
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
- `Session Integration` continua sendo o seam explicito da arquitetura, nao um owner semantico.
- `InputModeCoordinator` e apenas o rail operacional de request/dedupe/apply; a legitimidade de emissao session-side ficou concentrada em `Session Integration`.

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
