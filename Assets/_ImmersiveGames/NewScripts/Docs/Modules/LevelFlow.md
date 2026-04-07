# LevelFlow

## Status documental

- Este arquivo mantem o nome historico `LevelFlow`, mas o owner operacional atual e `Orchestration/LevelLifecycle`.
- `Game/Content/Definitions/Levels` e o owner de definitions/content de level.
- O seam principal entre os dois continua sendo `ILevelFlowContentService`.

## Estrutura atual

- `LevelLifecycle`: prepare, swap, restart, next, exit e intro local.
- `Game/Content/Definitions/Levels`: `LevelDefinitionAsset`, `LevelCollectionAsset` e contratos de conteudo.
- `LevelFlowRuntimeService`, `LevelMacroPrepareService`, `LevelSwapLocalService`, `PostLevelActionsService` e `LevelStageOrchestrator` compoem o runtime de `LevelLifecycle`.

## Responsabilidades atuais

- `LevelLifecycle` executa start/restart de gameplay e restauracao de nivel.
- `LevelMacroPrepareService` prepara ou limpa o level na entrada macro.
- `LevelSwapLocalService` faz troca local sem nova transicao macro.
- `LevelEnteredEvent` e o hook de aplicacao/ativacao do level.
- `SceneTransitionCompletedEvent` e o gate macro de entrada da `IntroStage`.
- `LevelIntroStagePresenterHost` resolve, adota, ativa e descarta o presenter local.
- `LevelLifecycleStageOrchestrator` inicia a `IntroStage` somente depois do gate macro e publica o handoff para `IntroStageCoordinator`.
- `IntroStageControlService` conclui ou pula a intro.
- `IntroStageCoordinator` bloqueia a simulacao, aguarda confirmacao e libera `Playing`.
- `LevelIntroCompletedEvent` e o handoff canonico para `Playing`.
- `LevelPostRunHookService` executa o rail local historico do fim de run da cena atual.
- `PostLevelActionsService` executa restart, next-level e exit-to-menu a partir do contexto atual.

## Handoff e ownership

- `LevelFlow` nao e owner do `PostRun` nem do `RunDecision`.
- `LevelFlow` nao e owner do `RunResultStage` nem do `RunDecision`.
- O papel daqui e fornecer contrato/conteudo da cena atual quando houver presenter explicito.
- O hook opcional de nivel e complementar, nao orchestrador.
- `Restart` e `ExitToMenu` continuam sendo acoes de contexto, nao ownership de `Navigation`.

## Dependencias e limites

- `SceneFlow` aplica a rota macro.
- `Navigation` resolve e despacha a rota macro de saida.
- `GameLoop` consome o handoff final da intro e reflete o estado alto nivel.
- `RunResultStage` e o rail local canonical opcional do fim de run; `PostRun` permanece apenas como alias historico.
- `LevelLifecycle` complementa com hook opcional de nivel, nao com ownership global.
- `ILevelIntroStagePresenterRegistry` e `ILevelIntroStagePresenterScopeResolver` servem a descoberta tecnica; o attach/ativacao pertence ao host.

## Regras praticas

- A IntroStage e level-owned, mas o gatilho canonico de entrada e `SceneTransitionCompletedEvent`.
- Se o level nao tiver intro, o fluxo segue sem pendencia.
- Se o level nao expuser presenter de `RunResultStage`, o fluxo faz skip observavel explicito.
- `RestartFromFirstLevel` e contrato distinto de `RestartLastGameplay`.
- `StartGameplayRouteAsync` pertence ao dispatch macro; a selecao de level nao acontece em `Navigation`.

## Leitura cruzada

- `Docs/Modules/PostRun.md`
- `Docs/Modules/GameLoop.md`
- `Docs/Modules/Navigation.md`
- `Docs/Guides/Production-How-To-Use-Core-Modules.md`
