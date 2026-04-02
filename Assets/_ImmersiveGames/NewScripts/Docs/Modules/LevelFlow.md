# LevelFlow

## Status documental

- Este arquivo mantem o nome historico `LevelFlow`, mas o owner operacional atual e `Orchestration/LevelLifecycle`.
- `Game/Content/Definitions/Levels` e o owner de definitions/content de level.
- O seam principal entre os dois continua sendo `ILevelFlowContentService`.

## Estrutura atual

- `LevelLifecycle`: prepare, swap, restart, next, exit e intro local.
- `Game/Content/Definitions/Levels`: `LevelDefinitionAsset`, `LevelCollectionAsset` e contratos de conteudo.
- `LevelFlowRuntimeService`, `LevelMacroPrepareService`, `LevelSwapLocalService`, `PostLevelActionsService` e `LevelStageOrchestrator` continuam como runtime de compat.

## Responsabilidades atuais

- `LevelLifecycle` executa start/restart de gameplay e restauracao de nivel.
- `LevelMacroPrepareService` prepara ou limpa o level na entrada macro.
- `LevelSwapLocalService` faz troca local sem nova transicao macro.
- `LevelEnteredEvent` e o hook canonico pos-aplicacao do level.
- `LevelIntroCompletedEvent` e o handoff canonico para `Playing`.
- `LevelStageOrchestrator` dispara e deduplica a `IntroStage`.
- `LevelPostRunHookService` executa o rail local de `PostRun` da cena atual.
- `PostLevelActionsService` executa restart, next-level e exit-to-menu a partir do contexto atual.

## Dependências e limites

- `SceneFlow` aplica a rota macro.
- `Navigation` resolve e despacha a rota macro de saida.
- `GameLoop` consome o handoff final da intro e reflete o estado alto nivel.
- `PostRun` fornece o resultado global e o rail local de nivel antes de `RunDecision`.
- `LevelLifecycle` complementa com hook opcional de nivel, nao com ownership global.
- `ILevelIntroStagePresenterRegistry` e `ILevelIntroStagePresenterScopeResolver` resolvem o presenter da intro.

## Handoff e ownership

- `LevelFlow` nao e owner do `PostRun` nem do `RunDecision`.
- O papel daqui e fornecer contrato/conteudo da cena atual quando houver presenter explicito.
- O hook opcional de nivel e complementar, nao orchestrador.
- `Restart` e `ExitToMenu` continuam sendo acoes de contexto, nao ownership de `Navigation`.

## Compatibilidade temporaria

- `Orchestration/LevelFlow/Runtime` continua de pe por transicao.
- `PostGame`, `GameOver` e `PostPlay` sao termos historicos; o runtime presente usa `RunOutcome`, `PostRun` e `RunDecision`.
- Namespaces antigos podem permanecer para seguranca ate a limpeza final.

## Hooks / contratos publicos

- `LevelEnteredEvent`
- `LevelIntroCompletedEvent`
- `ILevelStagePresentationService`
- `ILevelPostRunHookService`
- `ILevelPostRunHookPresenter`
- `ILevelIntroStagePresenterRegistry`
- `ILevelIntroStagePresenterScopeResolver`

## Regras praticas

- Intro e `level-owned` e dispara pelo `LevelEnteredEvent`.
- Se o level nao tiver intro, o fluxo segue sem pendencia.
- Se o level nao expuser presenter de `PostRun`, o fluxo faz skip observavel explicito.
- `RestartFromFirstLevel` e contrato distinto de `RestartLastGameplay`.
- `StartGameplayRouteAsync` pertence ao dispatch macro; a selecao de level nao acontece em `Navigation`.

## Leitura cruzada

- `Docs/Modules/PostRun.md`
- `Docs/Modules/GameLoop.md`
- `Docs/Modules/Navigation.md`
- `Docs/Guides/Production-How-To-Use-Core-Modules.md`
