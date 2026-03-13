# GameLoop

## Estado atual

- `GameLoopService` e owner do estado da run.
- `GameLoopSceneFlowCoordinator` sincroniza start plan e readiness com SceneFlow.
- `IntroStage` e opcional por level.
- `PostGame` e global.

## Ownership

- `GameLoopService`: estados da run, ready, intro, playing, pause e post game.
- `IntroStageCoordinator` + `LevelStageOrchestrator`: intro do level atual.
- `PostGameOwnershipService`: input mode e gate do post global.
- `PostGameResultService`: resultado formal do post global.

## Contrato de post atual

- Resultados formais: `Victory`, `Defeat` e `Exit`.
- `Victory` e `Defeat` entram pelo fim de run.
- `Exit` e formalizado na saida para menu a partir de `PostPlay`.
- `Restart` segue direto por reset macro e nao entra no post hook do level.

## Leitura cruzada

- `Docs/Modules/LevelFlow.md`
- `Docs/Modules/Navigation.md`
- `Docs/Guides/Event-Hooks-Reference.md`
