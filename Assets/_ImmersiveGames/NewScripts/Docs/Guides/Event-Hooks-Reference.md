# Event Hooks Reference

## Status documental

- Referencia operacional dos hooks ativos.
- O contrato canonico vigente do gameplay esta em `Docs/ADRs/ADR-0045-Gameplay-Runtime-Composition-Centro-Semantico-do-Gameplay.md`, `Docs/ADRs/ADR-0046-GameplaySessionFlow-como-primeiro-bloco-interno-do-Gameplay-Runtime-Composition.md` e `Docs/ADRs/ADR-0047-Gameplay-Phase-Construction-Pipeline-dentro-do-GameplaySessionFlow.md`.
- O fim de run canonico e `RunEndIntent -> RunResultStage` opcional -> `RunDecision -> Overlay`; `PostRun*` e alias historico da ponte legada.
- `LevelFlow` e nome historico de fronteira; a camada ativa e `LevelLifecycle`.
- Use hooks operacionais primeiro e hooks tecnicos so quando o ponto do pipeline realmente importar.

## Regra curta

- Declaracao nao e runtime.
- Runtime nao e operacao.
- Operacional e o que UI, bridges e systems usam para reagir ou pedir acao.

## Hooks canonicos e aplicacao

| Se voce quer... | Use este hook | Publisher atual | Aplicacao pratica |
|---|---|---|---|
| iniciar o handshake de start-plan do bootstrap | `BootStartPlanRequestedEvent` | `Orchestration/GameLoop/Bridges` | ligar `SceneFlow` e `GameLoop` no arranque |
| expressar Play do usuario | `GamePlayRequestedEvent` | `Experience/Frontend/UI` | pedir entrada em gameplay via backbone |
| saber que a transicao macro terminou | `SceneTransitionCompletedEvent` | `Orchestration/SceneFlow` | UI e systems que dependem da rota aplicada |
| saber que o reset do mundo terminou | `WorldResetCompletedEvent` | `Orchestration/WorldReset` | systems que dependem do mundo pronto |
| saber que a run ficou ativa | `GameRunStartedEvent` | `Orchestration/GameLoop/RunLifecycle` | ligar comportamento de gameplay ativo |
| saber que a run terminou | `GameRunEndedEvent` | `Orchestration/GameLoop/RunOutcome` | iniciar o handoff de pos-run e save |
| observar pedido de fim de run | `GameRunEndRequestedEvent` | `Orchestration/GameLoop/RunOutcome` | auditoria, telemetria e bridges |
| observar pedido de restart | `GameResetRequestedEvent` | `Orchestration/GameLoop/Commands` | reagir a intencao de restart |
| observar saida para menu | `GameExitToMenuRequestedEvent` | `Orchestration/GameLoop/Commands` | reagir a intencao de exit |
| saber que um level foi selecionado | `LevelSelectedEvent` | `Orchestration/LevelLifecycle` | atualizar estado do level atual |
| saber que o swap local foi aplicado | `LevelSwapLocalAppliedEvent` | `Orchestration/LevelLifecycle` | atualizar HUD, cameras e dependentes |
| saber que o level entrou no fluxo | `LevelEnteredEvent` | `Orchestration/LevelLifecycle` | seams level-owned, incluindo intro |
| saber que a intro terminou | `LevelIntroCompletedEvent` | `Orchestration/LevelLifecycle` e `Orchestration/GameLoop/IntroStage` | handoff de level para gameplay |
| saber que o pause vai entrar | `PauseWillEnterEvent` | `Orchestration/GameLoop/Pause` | reagir antes da entrada final em pause |
| saber que o pause vai sair | `PauseWillExitEvent` | `Orchestration/GameLoop/Pause` | reagir antes da saida final de pause |
| saber que o pause mudou de estado | `PauseStateChangedEvent` | `Orchestration/GameLoop/Pause` | observar o estado final de pause |
| iniciar o PostStage | `PostStageStartRequestedEvent` | `Experience/PostRun/Handoff` | validar o pos-run legado depois do outcome |
| assumir o PostStage | `PostStageStartedEvent` | `Experience/PostRun/Handoff` | mostrar presenter opcional da cena atual |
| concluir o PostStage | `PostStageCompletedEvent` | `Experience/PostRun/Handoff` | liberar o handoff final do rail legado |
| entrar no PostRun | `PostRunEnteredEvent` | `Experience/PostRun/Ownership` | alias historico do rail final; leitura canônica atual e `RunResultStage` opcional |
| concluir o PostRun | `PostRunCompletedEvent` | `Experience/PostRun/Ownership` | liberar a entrada semantica em `RunDecision` |
| entrar em RunDecision | `RunDecisionEnteredEvent` | `Experience/PostRun/Ownership` | permitir a abertura do overlay final de decisao |
| integrar save no fim da run | `ISaveOrchestrationService.TryHandleGameRunEnded(...)` | `Experience/Save` | persistir progression e preferences quando cabivel |
| integrar save apos reset do mundo | `ISaveOrchestrationService.TryHandleWorldResetCompleted(...)` | `Experience/Save` | atualizar rail de save apos reset completo |
| integrar save apos transicao concluida | `ISaveOrchestrationService.TryHandleSceneTransitionCompleted(...)` | `Experience/Save` | registrar save quando a transicao macro fecha |

## Regra de separacao entre PostRun e RunDecision

- `PostRunEnteredEvent` inicia o rail local de `PostRun`.
- `PostRunCompleted` e a fronteira que libera a entrada em `RunDecision`.
- O overlay final e consequencia de `RunDecisionEntered`.
- Nao trate `PostRunEnteredEvent` como gatilho visual do overlay.
- A leitura canônica atual do final da run e `RunEndIntent -> RunResultStage` opcional -> `RunDecision -> Overlay`.

## Hooks tecnicos do pipeline

- `SceneTransitionStartedEvent`
- `SceneTransitionFadeInCompletedEvent`
- `SceneTransitionScenesReadyEvent`
- `SceneTransitionBeforeFadeOutEvent`
- `InputModeRequestEvent`

Use estes apenas quando o caso depender do ponto tecnico exato da pipeline.

## O que nao usar como contrato principal

- Nao use `LevelFlow` como owner principal do level ativo.
- Nao use hooks tecnicos para integrar UI, gameplay ou systems por padrao.
- Nao trate `Save` como placeholder estreito; ele ja e runtime concreto.
- Nao invente hook operacional para spawn, registry, reset ou materializacao de gameplay. Isso nao pertence ao manifesto por level.
