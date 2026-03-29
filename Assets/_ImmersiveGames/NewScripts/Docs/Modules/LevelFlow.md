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

## PostStage em runtime

- `LevelFlow` nao e owner do `PostStage`.
- O papel de `LevelFlow` e fornecer contrato/conteudo da cena atual para o `PostStage` quando existir presenter explicito no level.
- O hook opcional de nivel continua complementar, nao orquestrador.
- O contrato completo fica definido em `Docs/ADRs/ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md`.

## Ownership

- `LevelFlowRuntimeService`: start gameplay default, restart da ultima entrada valida e restart from first level quando o intent exigir o primeiro level canonico.
- `LevelMacroPrepareService`: prepare/clear do level na entrada macro.
- `LevelSwapLocalService`: swap local no gameplay.
- `IPostLevelActionsService` / `PostLevelActionsService`: execucao canonica de restart e exit-to-menu no trilho pos-level/post-run, delegando a semântica concreta de restart para `LevelFlow`.
- `LevelEnteredEvent`: hook canonico para level aplicado/ativo.
- `LevelIntroCompletedEvent`: handoff canonico de fim da intro.
- `LevelStageOrchestrator`: trigger e dedupe de intro.
- `ILevelStagePresentationService`: contrato do level atual para intro e hook opcional de post.
- `ILevelPostGameHookService`: reacao opcional do level ao resultado global.
- `ILevelIntroStagePresenterRegistry`: contrato atomico de adotar/validar o presenter de intro.
- `ILevelIntroStagePresenterScopeResolver`: abstracao de escopo/candidatos validos de presenter do level atual.
- No runtime validado de `PostStage`, o level continua apenas como provedor de contrato/conteudo/presenter, sem ownership da orquestracao.

## Regras praticas

- Intro e level-owned, disparada pelo hook canonico `LevelEnteredEvent`.
- Quando a intro conclui ou e pulada, `LevelIntroCompletedEvent` faz o handoff para o GameLoop seguir para `Playing`.
- Se o level nao tiver intro, o fluxo segue sem erro e sem pendencia.
- Se o level nao expuser presenter de `PostStage`, o fluxo faz skip automatico.
- O hook opcional do level nao substitui o resultado global.
- `Restart` nao passa por esse hook; a execucao canônica sai de `PostLevelActionsService` para `LevelFlowRuntimeService.RestartLastGameplayAsync`.
- `RestartFromFirstLevel` e contrato distinto: o owner de `LevelFlow` resolve o primeiro level canonico do catalogo e nao reaproveita o contexto atual.
- A decisão entre `Restart` e `RestartFromFirstLevel` pertence ao owner de `LevelFlow`, nao ao `PostGame`.
- `NextLevel` e uma acao de progressao local, nao um post stage generico.
- O host de presenter nao conhece o tipo concreto do mock e nao conhece a topologia de carregamento; ele consome contratos do LevelFlow.

## Leitura cruzada

- `Docs/Modules/PostGame.md`
- `Docs/Modules/GameLoop.md`
- `Docs/Modules/Navigation.md`
- `Docs/Guides/Production-How-To-Use-Core-Modules.md`
