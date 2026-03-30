# GameLoop

## Status documental

- Parcial / leitura junto do runtime atual.
- Owner principal do loop de run e do estado `Paused`.
- Este documento expõe o backbone real do loop e os acoplamentos residuais que ainda existem no runtime.

## Objetivo

- Coordenar o ciclo `Boot -> Ready -> Playing -> Paused -> terminal da run`.
- Publicar o estado terminal da run via `GameRunOutcomeService`.
- Consumir handoffs de `IntroStage`, `LevelFlow`, `SceneFlow` e `PostGame` sem assumir ownership deles.

## Estado atual

- `GameLoopService` coordena ready, playing, pause e terminal técnico da run.
- `GameRunOutcomeService` publica `GameRunEndedEvent` e fecha o resultado terminal.
- `GameLoopSceneFlowSyncCoordinator` sincroniza `SceneFlow` com readiness e start plan.
- `IntroStage` é opcional por level; o `GameLoop` apenas consome o handoff final.
- `PostGame` é o estágio pós-run global, mas a entrada nele depende de bridge externa.
- O documento expõe fronteiras residuais em `GameRunEndedEventBridge`, `PostGameOwnershipService`, `PostGameResultService` e `GamePauseOverlayController`.

## Dependências e acoplamentos atuais

- `GameRunEndedEventBridge` faz a ponte entre `GameLoop` e `PostGame`.
- `PostGameOwnershipService` aplica gate e elegibilidade do pós-run.
- `PostGameResultService` formaliza o snapshot do resultado.
- `GamePauseOverlayController` reage ao estado de pause, mas não é owner dele.
- `LevelIntroCompletedEvent` conclui a intro level-owned e libera `Playing`.
- `SceneFlow` e `InputModes` continuam acoplamentos reais do loop.

## PostStage em runtime

- O owner do `PostStage` é `Modules/PostGame`.
- `GameLoop` não é owner do post-outcome; ele consome apenas o handoff final após `PostStageCompletedEvent`.
- `RequestRunEnd()` continua como comando de entrada para `RunEnd/PostGame`, mas não define o stage.
- O `GameLoop` não conhece presenter, UI ou contrato de cena do `PostStage`.
- O contrato oficial esta em `Docs/ADRs/ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md`.

## Ownership

- `GameLoopService`: coordenacao do loop (ready, playing, pause e terminal tecnico da run) e reflexo de atividade.
- `GameRunOutcomeService`: owner terminal do fim de run e publish de `GameRunEndedEvent`.
- `IntroStageCoordinator`: executor da IntroStage do level atual.
- `LevelStageOrchestrator`: trigger level-owned da intro via `LevelEnteredEvent`.
- `LevelIntroCompletedEvent`: handoff nivel->loop para sair de `Ready` e entrar em `Playing`.
- `PostGameOwnershipService`: gate e elegibilidade contextual do pos-run.
- `PostGameResultService`: resultado formal do post global.
- `GameLoop` não é owner semântico de `PostRunMenu`, `Restart` ou `ExitToMenu`.

## Limites conhecidos

- `PostGame` ainda depende do bridge de outcome para existir no fluxo final.
- O pause continua tendo overlay reativo e hooks oficiais, mas o `GameLoop` ainda é owner do estado `Paused`.
- `PostPlay` é termo histórico; o runtime presente usa `PostGame`.

## Contrato de post atual

- Resultados formais: `Victory`, `Defeat` e `Exit`.
- `Victory` e `Defeat` entram pelo fim de run.
- `Exit` é formalizado na saida para menu a partir de `PostGame`; `RunEnded` é estado terminal técnico do `GameLoop`.
- `Restart` e `ExitToMenu` não são owner do `GameLoop`; o dispatch canônico fica em `LevelFlow` e `Navigation`.
- O `PostStage` acontece antes de `PostGame`; `RunEnded` é terminal técnico do `GameLoop` e não deve ser lido como ownership de `PostRunMenu`.
- Default operacional: ausencia de presenter implica `PostStageSkipped reason='PostStage/NoPresenter'`.
- Presenter explicito da cena/conteudo executa GUI minima com `Continue` e `Skip` one-shot.
- `IntroStage` não depende de `Ready`/`IntroStage` do `GameLoop` para existir; o `GameLoop` apenas reflete o estado alto nível depois.
- Quando `LevelIntroCompletedEvent` chega, o GameLoop faz apenas o handoff para `Playing`.
- O timing e ownership da intro ficam em `LevelFlow`; o `GameLoop` só consome o handoff final.

## Hooks / contratos públicos

- `GameRunStartedEvent`
- `GameRunEndedEvent`
- `PauseWillEnterEvent`
- `PauseWillExitEvent`
- `PauseStateChangedEvent`
- `LevelIntroCompletedEvent`

## Leitura cruzada

- `Docs/Modules/PostGame.md`
- `Docs/Modules/LevelFlow.md`
- `Docs/Modules/Navigation.md`
- `Docs/Guides/Event-Hooks-Reference.md`
