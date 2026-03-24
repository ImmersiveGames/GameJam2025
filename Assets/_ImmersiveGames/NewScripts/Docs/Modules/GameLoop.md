# GameLoop

## Estado atual

- `GameLoopService` e coordenador do loop; o owner terminal da run e `GameRunOutcomeService`.
- `GameLoopSceneFlowSyncCoordinator` sincroniza start plan e readiness com SceneFlow.
- `IntroStage` e opcional por level.
- `PostGame` e global.

## Ownership

- `GameLoopService`: coordenacao do loop (ready, intro, playing, pause e post game).
- `GameRunOutcomeService`: owner terminal do fim de run e publish de `GameRunEndedEvent`.
- `GameRunResultSnapshotService`: projecao/snapshot do resultado atual da run.
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
