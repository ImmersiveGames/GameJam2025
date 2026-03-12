# LevelFlow

## Estado atual

- `LevelFlowRuntimeService` e owner do start/restart gameplay default.
- `LevelMacroPrepareService` prepara o level durante a fase macro do SceneFlow.
- `LevelSwapLocalService` faz troca intra-macro sem transicao macro.
- `LevelStageOrchestrator` coordena IntroStage e encaixe com o level.

## Ownership

- `LevelFlowRuntimeService`: start/restart default.
- `LevelMacroPrepareService`: prepare/clear de level na entrada macro.
- `LevelSwapLocalService`: swap local durante gameplay.
- `LevelStageOrchestrator`: trigger/dedupe de IntroStage.

## Semantica de fluxo

- `frontend/gameplay` chegam do `RouteKind` da rota ativa.
- O level nao depende de style/profile ids para decidir comportamento.
