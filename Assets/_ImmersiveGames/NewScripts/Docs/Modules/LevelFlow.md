# LevelFlow

## Estado atual

- `LevelFlowRuntimeService` e owner do start/restart de gameplay pelo trilho principal.
- `LevelMacroPrepareService` prepara ou limpa o level durante a fase macro.
- `LevelSwapLocalService` faz troca intra-macro sem nova transicao macro.
- `LevelEnteredEvent` e o hook canonico pos-level-applied para seams owned pelo level.
- `LevelIntroCompletedEvent` e o handoff canonico para gameplay depois da intro.
- `LevelStageOrchestrator` e seam fino de disparo da `IntroStage`.
- `ILevelIntroStagePresenterRegistry` + `ILevelIntroStagePresenterScopeResolver` resolvem o presenter canonico da intro sem prender o host ao mock concreto.
- O level atual pode expor:
  - `IntroStage` opcional
  - hook opcional para complementar a resposta ao resultado global

## Ownership

- `LevelFlowRuntimeService`: start gameplay default e restart da ultima entrada valida.
- `LevelMacroPrepareService`: prepare/clear do level na entrada macro.
- `LevelSwapLocalService`: swap local no gameplay.
- `LevelEnteredEvent`: hook canonico para level aplicado/ativo.
- `LevelIntroCompletedEvent`: handoff canonico de fim da intro.
- `LevelStageOrchestrator`: trigger e dedupe de intro.
- `ILevelStagePresentationService`: contrato do level atual para intro e hook opcional de post.
- `ILevelPostGameHookService`: reacao opcional do level ao resultado global.
- `ILevelIntroStagePresenterRegistry`: contrato atômico de adotar/validar o presenter de intro.
- `ILevelIntroStagePresenterScopeResolver`: abstracao de escopo/candidatos validos de presenter do level atual.

## Regras praticas

- Intro e level-owned, disparada pelo hook canonico `LevelEnteredEvent`.
- Quando a intro conclui ou e pulada, `LevelIntroCompletedEvent` faz o handoff para o GameLoop seguir para `Playing`.
- Se o level nao tiver intro, o fluxo segue sem erro e sem pendencia.
- O hook opcional do level nao substitui o resultado global.
- `Restart` nao passa por esse hook.
- `NextLevel` e uma acao de progressao local, nao um post stage generico.
- O host de presenter nao conhece o tipo concreto do mock e nao conhece a topologia de carregamento; ele consome contratos do LevelFlow.

## Leitura cruzada

- `Docs/Modules/GameLoop.md`
- `Docs/Modules/Navigation.md`
- `Docs/Guides/Production-How-To-Use-Core-Modules.md`
