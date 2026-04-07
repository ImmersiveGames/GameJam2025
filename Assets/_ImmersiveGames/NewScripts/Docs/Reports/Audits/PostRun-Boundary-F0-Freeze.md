# PostRun Boundary F0 Freeze

## 1. Boundary canonico congelado

Fonte canonica: `Docs/ADRs/ADR-0049-Fluxo-Canonico-de-Fim-de-Run-e-PostRun.md`.

Boundary canonicamente congelado:

- Entrada oficial: `Playing -> RunEndIntent(reason)`
- Stages oficiais: `RunResultStage`, `RunDecision`
- Saida oficial: `acao final downstream`
- Owners oficiais:
  - `RunResultStage` = `phase-owned`
  - `RunDecision` = `macro-owned`

Semantica operacional congelada:

- `Skip` encerra `RunResultStage` e volta ao jogo
- `Continue` encerra `RunResultStage` e segue para `RunDecision`
- `RunResult` e `RunDecision` possuem contratos tipados separados
- a resolucao e deterministica por tipagem e registro
- existe no maximo 1 presenter valido/adotavel por tipo no escopo correto

## 2. Upstream preservado

Arquivos mantidos como base upstream:

- `Orchestration/GameLoop/RunOutcome/GameRunPlayingStateGuard.cs`
- `Orchestration/GameLoop/RunOutcome/GameRunOutcomeService.cs`
- `Orchestration/GameLoop/RunOutcome/GameRunEndRequestService.cs`

Papel deles no freeze:

- validam a entrada em `Playing`
- consolidam o termino terminal da run
- publicam o pedido/aceite de fim de run como upstream do post-run

## 3. Centro semantico condenado

Arquivos e servicos que deixam de ser fonte de verdade do post-run:

- `Orchestration/GameLoop/Bridges/GameRunEndedEventBridge.cs`
- `Experience/PostRun/Handoff/PostRunHandoffService.cs`
- `Experience/PostRun/Ownership/PostRunOwnershipService.cs`
- `Experience/PostRun/Handoff/PostStageContracts.cs`
- `Experience/PostRun/Result/PostRunResultContracts.cs`
- `Experience/PostRun/Presentation/PostStagePresenterScopeResolver.cs`
- `Experience/PostRun/Handoff/LevelPostRunHookService.cs`
- `Experience/PostRun/Handoff/LevelPostRunHookContracts.cs`
- `Experience/PostRun/Presentation/Compat/LevelPostStageMockPresenter.cs`
- alias `PostRun = 1` em `Experience/PostRun/Ownership/PostRunFlowEvents.cs`

Leitura do freeze:

- o centro semantico antigo ainda existe fisicamente
- ele nao define mais o contrato canonico
- ele fica condenado como legado a ser removido do caminho critico

## 4. Remocao do caminho canonico

Arquivos que, a partir de agora, nao devem participar do rail canonico:

- `Experience/PostRun/Handoff/PostRunHandoffService.cs`
- `Experience/PostRun/Ownership/PostRunOwnershipService.cs`
- `Experience/PostRun/Handoff/PostStageContracts.cs`
- `Experience/PostRun/Result/PostRunResultContracts.cs`
- `Experience/PostRun/Presentation/PostStagePresenterScopeResolver.cs`
- `Experience/PostRun/Handoff/LevelPostRunHookService.cs`
- `Experience/PostRun/Handoff/LevelPostRunHookContracts.cs`
- `Experience/PostRun/Presentation/Compat/LevelPostStageMockPresenter.cs`
- alias `PostRun = 1` em `Experience/PostRun/Ownership/PostRunFlowEvents.cs`

Classificacao de demolicao:

- Remover:
  - `PostRunHandoffService`
  - `PostRunOwnershipService`
  - `PostStageContracts`
  - `PostRunResultContracts`
  - `PostStagePresenterScopeResolver`
  - `LevelPostRunHookService`
  - `LevelPostRunHookContracts`
  - `LevelPostStageMockPresenter`
  - `PostRun = 1`
- Substituir:
  - `GameRunEndedEventBridge`
  - `PostStageControlService`
  - `PostStagePresenterRegistry`
  - `RunResultStageMockPresenter`
  - `PostRunOverlayController`
  - `PostRunResultService`
- Manter como upstream:
  - `GameRunPlayingStateGuard`
  - `GameRunOutcomeService`
  - `GameRunEndRequestService`

## 5. Arquivos bloqueados

Arquivos que podem continuar existindo temporariamente, mas ficam congelados para novas regras de negocio do post-run:

- `Orchestration/GameLoop/Bridges/GameRunEndedEventBridge.cs`
- `Experience/PostRun/Handoff/PostRunHandoffService.cs`
- `Experience/PostRun/Ownership/PostRunOwnershipService.cs`
- `Experience/PostRun/Handoff/PostStageContracts.cs`
- `Experience/PostRun/Result/PostRunResultContracts.cs`
- `Experience/PostRun/Presentation/PostStagePresenterScopeResolver.cs`
- `Experience/PostRun/Presentation/Bindings/PostRunOverlayController.cs`
- `Experience/PostRun/Presentation/RunResultStageMockPresenter.cs`
- `Experience/PostRun/Presentation/Compat/LevelPostStageMockPresenter.cs`
- `Experience/PostRun/Handoff/LevelPostRunHookService.cs`
- `Experience/PostRun/Handoff/LevelPostRunHookContracts.cs`
- `Experience/PostRun/Ownership/PostRunFlowEvents.cs`

Regra para os bloqueados:

- podem receber apenas mudancas mecanicas de freeze, inventario e corte
- nao podem receber nova semantica de negocio do post-run
- nao podem virar destino de uma nova regra de ownership ou fluxo

## 6. Mapa de dependencias

### Dependencias que ainda existem hoje

- `GameRunOutcomeService` publica o termino terminal da run
- `GameRunEndedEventBridge` observa o termino e empurra o handoff do post-run
- `PostRunHandoffService` depende de `IGameplayPhaseRuntimeService`, `IPostStageCoordinator`, `IPostRunResultService`, `IPostRunOwnershipService` e `ILevelPostRunHookService`
- `PostRunOwnershipService` depende de `IRunDecisionPresenterHost` e de eventos do rail legado
- `PostStagePresenterRegistry` depende de `IPostStagePresenterScopeResolver`
- `PostStagePresenterScopeResolver` depende de varredura de cenas carregadas
- `PostRunOverlayController` depende de `IRunDecisionPresenter` e de servicos de acao de level

### Cortes obrigatorios nas proximas fases

- cortar a dependencia de resolucao por busca no `PostStagePresenterScopeResolver`
- cortar a dependencia de `PostRunOwnershipService` como owner monolitico de `RunResultStage` e `RunDecision`
- cortar o lane legado de `LevelPostRunHookService`
- cortar o uso de `PostRun` como fonte semantica no caminho canonic
- cortar a mistura entre `RunResultStage` e `RunDecision` em contratos genericos

## 7. Lista de demolicao

### Remover do caminho canonico

- `PostRunHandoffService`
- `PostRunOwnershipService`
- `PostStageContracts`
- `PostRunResultContracts`
- `PostStagePresenterScopeResolver`
- `LevelPostRunHookService`
- `LevelPostRunHookContracts`
- `LevelPostStageMockPresenter`
- alias `PostRun = 1`

### Substituir

- `GameRunEndedEventBridge`
- `PostStageControlService`
- `PostStagePresenterRegistry`
- `RunResultStageMockPresenter`
- `PostRunOverlayController`
- `PostRunResultService`

### Manter como upstream

- `GameRunPlayingStateGuard`
- `GameRunOutcomeService`
- `GameRunEndRequestService`

## 8. Lista de nao tocar

Arquivos que nao devem mais receber novas regras de negocio do post-run:

- `Orchestration/GameLoop/RunOutcome/GameRunPlayingStateGuard.cs`
- `Orchestration/GameLoop/RunOutcome/GameRunOutcomeService.cs`
- `Orchestration/GameLoop/RunOutcome/GameRunEndRequestService.cs`
- `Experience/PostRun/Handoff/LevelPostRunHookService.cs`
- `Experience/PostRun/Handoff/LevelPostRunHookContracts.cs`
- `Experience/PostRun/Presentation/Compat/LevelPostStageMockPresenter.cs`
- `Experience/PostRun/Ownership/PostRunFlowEvents.cs`

Esses arquivos podem existir no freeze, mas nao sao mais o local para introduzir regra nova do contrato canonicamente congelado.

## 9. Criterio de transicao para F1

F1 pode abrir somente quando todo isto estiver fechado:

- boundary canonico e lista de owners estao congelados neste relatorio
- upstream preservado foi separado do centro semantico condenado
- lista de arquivos bloqueados foi aceita como congelamento temporario
- a demolicao foi classificada em remover, substituir e manter
- nao existe mais ambiguidade sobre qual arquivo define o contrato canonico do post-run
- nao ha intencao de reabrir compat lane, trilho paralelo ou busca heuristica antes da F1

## 10. Conclusao

Esta F0 nao altera o fluxo, nao cria contratos novos e nao implementa o novo boundary.

Ela apenas congela o recorte, isola o centro semantico antigo e prepara a reconstrucao controlada das fases seguintes.
