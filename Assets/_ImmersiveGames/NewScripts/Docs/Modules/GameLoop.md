# GameLoop

## Status documental

- Parcial / leitura junto do runtime atual.
- O root fisico atual e `Orchestration/GameLoop`.
- O loop continua sendo owner do estado `Paused`, mas nao do `PostRun`.

## Estrutura atual

- `RunLifecycle/Core`: miolo do loop, state machine, eventos base e transicoes.
- `RunOutcome`: outcome terminal da run e request de fim.
- `Commands`: comandos explicitos do loop.
- `Bridges`: adaptadores entre `GameLoop`, `SceneFlow`, `PostRun` e input.
- `Pause`: overlay reativo e hooks de pausa.
- `IntroStage`: handoff da intro level-owned.

## Objetivo

- Coordenar `Boot -> Ready -> Playing -> Paused -> terminal da run`.
- Publicar o estado terminal da run sem assumir ownership de `PostRun`.
- Consumir handoffs de `LevelLifecycle`, `SceneFlow` e `PostRun` sem inverter ownership.

## Ownership atual

- `GameLoopService`: coordenacao do loop e reflexo de atividade.
- `GameLoopStateMachine`: transicoes de estado do loop.
- `GameRunOutcomeService`: fim terminal da run e `GameRunEndedEvent`.
- `GameLoopCommands`: request de pause, resume, victory, defeat, restart e exit.
- `GamePauseOverlayController`: apresentacao reativa do pause.
- `IntroStageControlService`: conclusao/pulo da intro do level atual.
- `GameRunEndedEventBridge`: seam explicito para `Experience/PostRun/Handoff`.

## Handoff e limites

- `IPostRunHandoffService` e a fronteira principal com `Experience/PostRun`.
- `PostRun` e o owner do rail local de pos-run; `RunDecision` e o overlay final.
- `GameLoop` consome o handoff final, mas nao conhece presenter ou overlay de `PostRun` / `RunDecision`.
- `LevelIntroCompletedEvent` libera a passagem para `Playing`; o timing da intro continua level-owned.
- `Restart` e `ExitToMenu` seguem intencao de contexto; a execucao concreta fica em `LevelLifecycle` e `Navigation`.

## Compatibilidade temporaria

- Namespaces antigos ainda podem existir por seguranca.
- Os shells antigos de `Core`, `Flow`, `Input`, `Interop`, `Run` e `EndConditions` ja foram podados.
- `PostGame` e `PostPlay` sao nomes historicos; o runtime atual usa `RunOutcome`, `PostRun` e `RunDecision`.

## Hooks / contratos publicos

- `GameRunStartedEvent`
- `GameRunEndedEvent`
- `PauseWillEnterEvent`
- `PauseWillExitEvent`
- `PauseStateChangedEvent`
- `LevelIntroCompletedEvent`

## Leitura cruzada

- `Docs/Modules/PostRun.md`
- `Docs/Modules/LevelFlow.md`
- `Docs/Modules/Navigation.md`
- `Docs/Guides/Event-Hooks-Reference.md`
