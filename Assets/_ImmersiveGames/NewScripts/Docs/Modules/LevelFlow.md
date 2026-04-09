# LevelFlow

## Status documental

- O nome `LevelFlow` e historico.
- O owner operacional atual e `Orchestration/LevelLifecycle`.
- O fluxo canônico e `phase-first`.
- `SceneTransitionCompletedEvent` continua sendo o gate macro da entrada.

## Estrutura atual

- `LevelLifecycle`: prepare phase-first, swap local, restart, next, exit e intro local.
- `PhaseDefinitionAsset`: authoring da phase, payload de swap e continuidade.
- `PhaseDefinitionCatalog`: resolucao da phase canonica da rota.
- `PhaseDefinitionSelectedEvent`: selecao da phase para o fluxo atual.
- `PhaseIntroStageEntryEvent`: disparo da intro phase-first.
- `PhaseResetContext`: identidade phase-first do reset corrente.
- `LevelLifecycleRuntimeService`, `LevelMacroPrepareService`, `LevelSwapLocalService`, `PostLevelActionsService` e `LevelStageOrchestrator` compoem o runtime de `LevelLifecycle`.

## Responsabilidades atuais

- `LevelLifecycle` executa start/restart de gameplay e restauracao de phase.
- `LevelMacroPrepareService` prepara ou limpa a phase na entrada macro.
- `LevelSwapLocalService` faz troca local sem nova transicao macro.
- `SceneTransitionCompletedEvent` e o gate macro de entrada da `IntroStage`.
- `LevelIntroStagePresenterHost` resolve, adota, ativa e descarta o presenter local.
- `LevelLifecycleStageOrchestrator` inicia a `IntroStage` somente depois do gate macro e publica o handoff para `IntroStageCoordinator`.
- `IntroStageControlService` conclui ou pula a intro.
- `IntroStageCoordinator` bloqueia a simulacao, aguarda confirmacao e libera `Playing`.
- `PhaseIntroCompletedEvent` e o handoff canonico para `Playing`.
- `LevelPostRunHookService` permanece como bridge historica do fim de run da cena atual.
- `PostLevelActionsService` executa restart, next-level e exit-to-menu a partir do contexto atual.

## Handoff e ownership

- `LevelFlow` nao e owner do `PostRun` nem do `RunDecision`.
- `LevelFlow` nao e owner do `RunResultStage` nem do `RunDecision`.
- O papel daqui e fornecer contrato e content da phase atual quando houver presenter explicito.
- O hook opcional de nivel e complementar, nao orchestrador.
- `Restart` e `ExitToMenu` continuam sendo acoes de contexto, nao ownership de `Navigation`.

## Dependencias e limites

- `SceneFlow` aplica a rota macro.
- `Navigation` resolve e despacha a rota macro de saida.
- `GameLoop` consome o handoff final da intro e reflete o estado alto nivel.
- `RunResultStage` e o rail local canonico opcional do fim de run; `PostRun` permanece apenas como alias historico.
- `LevelLifecycle` complementa com hook opcional historico, nao com ownership global.
- `ILevelIntroStagePresenterRegistry` e `ILevelIntroStagePresenterScopeResolver` servem a descoberta tecnica; o attach/ativacao pertence ao host.

## Regras praticas

- A IntroStage e acionada por `PhaseDefinitionSelectedEvent` e pelo gate `SceneTransitionCompletedEvent`.
- Se a phase nao tiver intro, o fluxo segue sem pendencia.
- Se a phase nao expuser presenter de `RunResultStage`, o fluxo faz skip observavel explicito.
- `RestartFromFirstPhase` e contrato distinto de `RestartLastGameplay`.
- `StartGameplayRouteAsync` pertence ao dispatch macro; a selecao da phase nao acontece em `Navigation`.

## Leitura cruzada

- `Docs/Modules/PostRun.md`
- `Docs/Modules/GameLoop.md`
- `Docs/Modules/Navigation.md`
- `Docs/Guides/Production-How-To-Use-Core-Modules.md`
