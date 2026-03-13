# LevelFlow

## Estado atual

- `LevelFlowRuntimeService` e owner do start/restart de gameplay pelo trilho principal.
- `LevelMacroPrepareService` prepara ou limpa o level durante a fase macro.
- `LevelSwapLocalService` faz troca intra-macro sem nova transicao macro.
- `LevelStageOrchestrator` continua owner da orquestracao de `IntroStage`.
- O level atual pode expor:
  - `IntroStage` opcional
  - hook opcional para complementar o `PostGame` global

## Ownership

- `LevelFlowRuntimeService`: start gameplay default e restart da ultima entrada valida.
- `LevelMacroPrepareService`: prepare/clear do level na entrada macro.
- `LevelSwapLocalService`: swap local no gameplay.
- `LevelStageOrchestrator`: trigger e dedupe de intro.
- `ILevelStagePresentationService`: contrato do level atual para intro e hook opcional de post.
- `ILevelPostGameHookService`: reacao opcional do level ao `PostGame` global.

## Regras praticas

- Intro e level-owned, mas a orquestracao continua global no `LevelStageOrchestrator`.
- Se o level nao tiver intro, o fluxo segue sem erro.
- O hook opcional do level nao substitui o `PostGame` global.
- `Restart` nao passa por esse hook.
- `NextLevel` e uma acao de progressao local, nao um post stage generico.

## Leitura cruzada

- `Docs/Modules/GameLoop.md`
- `Docs/Modules/Navigation.md`
- `Docs/Guides/Production-How-To-Use-Core-Modules.md`
