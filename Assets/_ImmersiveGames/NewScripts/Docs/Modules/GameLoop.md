# GameLoop

## Estado atual

- `GameLoopService` e coordenador do loop; o owner terminal da run e `GameRunOutcomeService`.
- `GameLoopSceneFlowSyncCoordinator` sincroniza start plan e readiness com SceneFlow.
- `IntroStage` e opcional por level, mas nao e gate canonico do GameLoop.
- `PostGame` e global no runtime atual.

## PostStage em runtime

- O owner do `PostStage` e `Modules/PostGame`.
- `GameLoop` nao e owner do post-outcome; ele consome apenas o handoff final apos `PostStageCompletedEvent`.
- `RequestRunEnd()` continua como comando de entrada para `RunEnd/PostGame`, mas nao define o stage.
- O `GameLoop` nao conhece presenter, UI ou contrato de cena do `PostStage`.
- O contrato oficial esta em `Docs/ADRs/ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md`.

## Ownership

- `GameLoopService`: coordenacao do loop (ready, playing, pause e terminal tecnico da run) e reflexo de atividade.
- `GameRunOutcomeService`: owner terminal do fim de run e publish de `GameRunEndedEvent`.
- `IntroStageCoordinator`: executor da IntroStage do level atual.
- `LevelStageOrchestrator`: trigger level-owned da intro via `LevelEnteredEvent`.
- `LevelIntroCompletedEvent`: handoff nivel->loop para sair de `Ready` e entrar em `Playing`.
- `PostGameOwnershipService`: gate e elegibilidade contextual do pos-run.
- `PostGameResultService`: resultado formal do post global.

## Contrato de post atual

- Resultados formais: `Victory`, `Defeat` e `Exit`.
- `Victory` e `Defeat` entram pelo fim de run.
- `Exit` e formalizado na saida para menu a partir de `PostGame`; `RunEnded` e estado terminal tecnico do GameLoop.
- `Restart` e `ExitToMenu` nao sao owner do GameLoop; o dispatch canonico fica em `LevelFlow` e `Navigation`.
- O `PostStage` acontece antes de `PostGame`; `RunEnded` e terminal tecnico do GameLoop e nao deve ser lido como ownership de `PostRunMenu`.
- Default operacional: ausencia de presenter implica `PostStageSkipped reason='PostStage/NoPresenter'`.
- Presenter explicito da cena/conteudo executa GUI minima com `Continue` e `Skip` one-shot.
- `IntroStage` nao depende de `Ready`/`IntroStage` do GameLoop para existir; o GameLoop apenas reflete o estado alto nivel depois.
- Quando `LevelIntroCompletedEvent` chega, o GameLoop faz apenas o handoff para `Playing`.
- O timing e ownership da intro ficam em `LevelFlow`; o GameLoop so consome o handoff final.

## Leitura cruzada

- `Docs/Modules/PostGame.md`
- `Docs/Modules/LevelFlow.md`
- `Docs/Modules/Navigation.md`
- `Docs/Guides/Event-Hooks-Reference.md`
