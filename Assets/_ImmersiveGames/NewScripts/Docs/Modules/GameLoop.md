# GameLoop

## Status documental

- O root fisico atual e `Orchestration/GameLoop`.
- O loop continua sendo owner do estado `Paused` e da coordenacao de handoff, mas nao do fim de run semanticamente central.
- `LevelFlow` e `PostRun` sao nomes historicos; o canon atual separa IntroStage, Runtime phase e RunDecision.

## Estrutura atual

- `RunLifecycle/Core`: miolo do loop, state machine, eventos base e transicoes.
- `RunOutcome`: outcome terminal da run e request de fim.
- `Commands`: comandos explicitos do loop.
- `Bridges`: adaptadores entre `GameLoop`, `SceneFlow`, o rail historico do fim de run e input.
- `Pause`: overlay reativo e hooks de pausa.
- `IntroStage`: handoff canonico post-reveal da intro scene-local.

## Objetivo

- Coordenar `Boot -> Ready -> Playing -> Paused -> terminal da run`.
- Publicar o estado terminal da run sem assumir ownership do rail final.
- Consumir handoffs de `SceneFlow`, `GameplaySessionFlow` e do rail historico do fim de run sem inverter ownership.

## Ownership atual

- `GameLoopService`: coordenacao do loop e reflexo de atividade.
- `GameLoopStateMachine`: transicoes de estado do loop.
- `GameRunOutcomeService`: fim terminal da run e `GameRunEndedEvent`.
- `GameLoopCommands`: request de pause, resume, victory, defeat, restart e exit.
- `GamePauseOverlayController`: apresentacao reativa do pause.
- `IntroStageControlService`: conclusao/pulo da intro da phase atual.
- `IntroStageCoordinator`: bloqueio da simulacao, wait de confirmacao e handoff para `Playing`.
- `IntroStagePresenterHost`: attach e detach do presenter local.

## Handoff e limites

- `IPostRunHandoffService` e a fronteira historica com o rail legado de fim de run.
- O fluxo canonico atual e `RunEndIntent -> RunResultStage` opcional -> `RunDecision -> Overlay`.
- `GameLoop` consome o handoff final, mas nao conhece presenter ou overlay do rail final.
- `IntroStageCompletedEvent` libera a passagem para `Playing`; o timing da intro continua scene-local.
- `Restart` e `ExitToMenu` seguem intencao de contexto; a execucao concreta fica em `GameplaySessionFlow`, `Navigation` e `SceneFlow`.

## Compatibilidade historica fora do caminho canonico

- Namespaces antigos ainda podem existir por seguranca.
- Os shells antigos de `Core`, `Flow`, `Input`, `Interop`, `Run` e `EndConditions` ja foram podados.
- `PostGame` e `PostPlay` sao nomes historicos; o runtime atual usa `RunOutcome`, `RunResultStage` e `RunDecision`.
- `PostRun` permanece apenas como alias historico do rail antigo.

## Hooks / contratos publicos

- `GameRunStartedEvent`
- `GameRunEndedEvent`
- `PauseWillEnterEvent`
- `PauseWillExitEvent`
- `PauseStateChangedEvent`
- `IntroStageCompletedEvent`

## Leitura cruzada

- `Docs/Archive/Modules/PostRun.md`
- `Docs/Modules/Gameplay.md`
- `Docs/Modules/Navigation.md`
- `Docs/Guides/Event-Hooks-Reference.md`
