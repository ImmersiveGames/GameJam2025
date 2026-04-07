# Plan - ADR-0049 PostRun Boundary Rebuild

## 1. Objetivo

Reconstruir o boundary de post-run como um fluxo canonico, tipado e deterministico, alinhado ao `ADR-0049`.

O post-run atual sera tratado como centro semantico parcialmente descartavel: o que servir como base/upstream permanece; o que mistura ownership, compat lane, heuristica ou legado residual sai do caminho canonico.

## 2. Principios de execucao

- O fluxo canonico e sempre: `Playing -> RunEndIntent(reason) -> RunResultStage -> Continue -> RunDecision -> acao final downstream`.
- `RunResultStage` e `phase-owned`.
- `RunDecision` e `macro-owned`.
- `RunResultStage` termina por acao explicita de `Continue`.
- `RunResult` e `RunDecision` possuem contratos tipados separados.
- A resolucao e deterministica por tipagem e registro.
- Existe no maximo 1 presenter valido/adotavel por tipo no escopo correto.

Fora de escopo desta reconstrucao:

- qualquer mudanca de `IntroStage`
- qualquer edicao de cena
- qualquer trilho paralelo ou compat lane no caminho canonico
- qualquer descoberta por busca, varredura de cena ou filtro heuristico
- qualquer solucao por patch local em vez de separacao estrutural

Regra de aceite do plano:

- o caminho canonico nao depende de `PostRun` legado, `Level` legado, heuristica de busca ou owner monolitico
- cada fase entrega um corte verificavel e acumulativo

## 3. Inventario operacional resumido

### Base / upstream a preservar

- `Orchestration/GameLoop/RunOutcome/GameRunPlayingStateGuard.cs`
- `Orchestration/GameLoop/RunOutcome/GameRunOutcomeService.cs`
- `Orchestration/GameLoop/RunOutcome/GameRunEndRequestService.cs`

### Centro semantico atual a desmontar

- `Orchestration/GameLoop/Bridges/GameRunEndedEventBridge.cs`
- `Experience/PostRun/Handoff/PostRunHandoffService.cs`
- `Experience/PostRun/Ownership/PostRunOwnershipService.cs`
- `Experience/PostRun/Handoff/PostStageContracts.cs`
- `Experience/PostRun/Result/PostRunResultContracts.cs`
- `Experience/PostRun/Presentation/PostStagePresenterScopeResolver.cs`
- `Experience/PostRun/Handoff/LevelPostRunHookService.cs`

### Pecas reaproveitaveis com refatoracao forte

- `Experience/PostRun/Handoff/PostStageControlService.cs`
- `Experience/PostRun/Presentation/PostStagePresenterRegistry.cs`
- `Experience/PostRun/Presentation/RunResultStageMockPresenter.cs`
- `Experience/PostRun/Presentation/Bindings/PostRunOverlayController.cs`
- `Experience/PostRun/Result/PostRunResultService.cs`
- `Orchestration/GameLoop/Bridges/GameRunEndedEventBridge.cs`

### Pecas a remover do caminho canonico

- `Experience/PostRun/Handoff/PostRunHandoffService.cs`
- `Experience/PostRun/Ownership/PostRunOwnershipService.cs`
- `Experience/PostRun/Handoff/PostStageContracts.cs`
- `Experience/PostRun/Handoff/LevelPostRunHookContracts.cs`
- `Experience/PostRun/Handoff/LevelPostRunHookService.cs`
- `Experience/PostRun/Presentation/Compat/LevelPostStageMockPresenter.cs`
- `Experience/PostRun/Presentation/PostStagePresenterScopeResolver.cs`
- `Experience/PostRun/Result/PostRunResultContracts.cs`
- alias `PostRun = 1` em `Experience/PostRun/Ownership/PostRunFlowEvents.cs`

## 4. Fases F0-F6

### F0 - Freeze e isolamento do boundary

Objetivo:

- congelar o recorte canonico do post-run e separar o que e upstream do que e legado de boundary

Resultado esperado:

- um boundary fechado, com lista unica de entradas, saidas, owners e presenters

Arquivos principais:

- `Orchestration/GameLoop/Bridges/GameRunEndedEventBridge.cs`
- `Experience/PostRun/Handoff/PostRunHandoffService.cs`
- `Experience/PostRun/Ownership/PostRunOwnershipService.cs`
- `Experience/PostRun/Handoff/PostStageContracts.cs`
- `Experience/PostRun/Result/PostRunResultContracts.cs`

Dependencias:

- `ADR-0049`
- inventario consolidado da auditoria

Criterio de aceite:

- nenhuma decisao de reconstrucao depende de heuristica, compat lane ou owner misto
- o boundary canonico esta explicitamente separado do legado

Risco principal:

- deixar a fronteira ainda ambigua e reabrir a mistura entre entrada, ownership e presentation

### F1 - Contratos tipados separados

Objetivo:

- separar os contratos de `RunEndIntent`, `RunResultStage` e `RunDecision`

Resultado esperado:

- tipos distintos para entrada, resultado local e decisao macro

Arquivos principais:

- `Experience/PostRun/Result/PostRunResultContracts.cs`
- `Experience/PostRun/Handoff/PostStageContracts.cs`
- `Experience/PostRun/Ownership/PostRunFlowEvents.cs`

Dependencias:

- F0 concluido

Criterio de aceite:

- `RunResult` e `RunDecision` deixam de compartilhar contrato generico
- `RunEndIntent` vira o primeiro contrato operacional do boundary

Risco principal:

- perpetuar tipos genericos e apenas renomear conceitos

### F2 - Separacao de ownership

Objetivo:

- separar `RunResultStage` como `phase-owned` e `RunDecision` como `macro-owned`

Resultado esperado:

- owner unico por etapa, sem monolito que acumula os dois ciclos

Arquivos principais:

- `Experience/PostRun/Ownership/PostRunOwnershipService.cs`
- `Experience/PostRun/Handoff/PostRunHandoffService.cs`
- `Orchestration/GameLoop/Bridges/GameRunEndedEventBridge.cs`

Dependencias:

- F1 concluido

Criterio de aceite:

- `RunResultStage` nao decide navegacao macro
- `RunDecision` nao e governado como stage local

Risco principal:

- separar apenas interface e manter a mesma aglomeracao interna

### F3 - Resolucao deterministica por registro tipado

Objetivo:

- substituir descoberta por busca e varredura por registro deterministico por tipo

Resultado esperado:

- cada presenter e resolvido por contrato tipado, host e cardinalidade

Arquivos principais:

- `Experience/PostRun/Presentation/PostStagePresenterRegistry.cs`
- `Experience/PostRun/Presentation/PostStagePresenterScopeResolver.cs`
- `Experience/PostRun/Presentation/RunResultStageMockPresenter.cs`
- `Experience/PostRun/Presentation/Bindings/PostRunOverlayController.cs`

Dependencias:

- F1 concluido
- F2 concluido

Criterio de aceite:

- no maximo 1 presenter adotavel por tipo no escopo correto
- nao existe varredura de cena como shape final

Risco principal:

- manter a heuristica por baixo de uma interface aparentemente limpa

### F4 - Presenter local do `RunResultStage`

Objetivo:

- consolidar o presenter passive/local do `RunResultStage` como owner visual da phase

Resultado esperado:

- o result stage possui presenter local claro, tipado e adotado pelo escopo correto
- o presenter local existe previamente como conteudo da phase/cena e nao e criado automaticamente pelo rail

Arquivos principais:

- `Experience/PostRun/Presentation/RunResultStageMockPresenter.cs`
- `Experience/PostRun/Presentation/PostStagePresenterRegistry.cs`
- `Experience/PostRun/Handoff/PostStageControlService.cs`

Dependencias:

- F3 concluido

Criterio de aceite:

- `Continue` encerra o stage e abre caminho para `RunDecision`
- o presenter local nao toma decisao macro
- o stage nao oferece retorno ao jogo a partir de `RunResultStage`

Risco principal:

- transformar o presenter em coordenador ou duplicar a semantica de decisao

### F5 - Presenter macro do `RunDecision`

Objetivo:

- consolidar o presenter macro de `RunDecision` como overlay/acao final downstream

Resultado esperado:

- o downstream de restart/menu fica centralizado e macro-owned
- `RunDecision` recebe sempre o handoff unico vindo de `RunResultStage`

Arquivos principais:

- `Experience/PostRun/Presentation/Bindings/PostRunOverlayController.cs`
- `Experience/PostRun/Ownership/PostRunOwnershipService.cs`

Dependencias:

- F2 concluido

Criterio de aceite:

- `RunDecision` recebe continuidade apos `RunResultStage`
- restart/menu ficam no boundary macro, sem retorno de escopo local
- nao existe handoff alternativo vindo de `Skip`

Risco principal:

- manter o overlay dependente de contexto legado de `PostRun`

### F6 - Validacao end-to-end e purge final do legado residual

Objetivo:

- validar a cadeia completa e remover o restante do caminho canonico legado

Resultado esperado:

- boundary final limpo, verificavel e sem trilho paralelo

Arquivos principais:

- `Orchestration/GameLoop/Bridges/GameRunEndedEventBridge.cs`
- `Experience/PostRun/Handoff/PostRunHandoffService.cs`
- `Experience/PostRun/Ownership/PostRunOwnershipService.cs`
- `Experience/PostRun/Handoff/LevelPostRunHookService.cs`
- `Experience/PostRun/Handoff/LevelPostRunHookContracts.cs`
- `Experience/PostRun/Presentation/Compat/LevelPostStageMockPresenter.cs`
- `Experience/PostRun/Ownership/PostRunFlowEvents.cs`

Dependencias:

- F4 concluido
- F5 concluido

Criterio de aceite:

- os cenarios de validacao passam sem compat lane
- o caminho canonico nao usa busca heuristica, scene owner errado ou legacy residual
- nao existe validacao com `Skip -> Playing`

Risco principal:

- deixar sobras semanticas de `PostRun` em eventos, aliases ou hooks paralelos

## 5. Matriz de impacto por arquivo

### Manter

- `Orchestration/GameLoop/RunOutcome/GameRunPlayingStateGuard.cs`
- `Orchestration/GameLoop/RunOutcome/GameRunOutcomeService.cs`
- `Orchestration/GameLoop/RunOutcome/GameRunEndRequestService.cs`

### Refatorar

- `Orchestration/GameLoop/Bridges/GameRunEndedEventBridge.cs`
- `Experience/PostRun/Handoff/PostStageControlService.cs`
- `Experience/PostRun/Presentation/PostStagePresenterRegistry.cs`
- `Experience/PostRun/Presentation/RunResultStageMockPresenter.cs`
- `Experience/PostRun/Presentation/Bindings/PostRunOverlayController.cs`
- `Experience/PostRun/Result/PostRunResultService.cs`

### Substituir

- `Experience/PostRun/Handoff/PostRunHandoffService.cs`
- `Experience/PostRun/Ownership/PostRunOwnershipService.cs`
- `Experience/PostRun/Handoff/PostStageContracts.cs`
- `Experience/PostRun/Result/PostRunResultContracts.cs`
- `Experience/PostRun/Presentation/PostStagePresenterScopeResolver.cs`

### Remover do caminho canonico

- `Experience/PostRun/Handoff/LevelPostRunHookService.cs`
- `Experience/PostRun/Handoff/LevelPostRunHookContracts.cs`
- `Experience/PostRun/Presentation/Compat/LevelPostStageMockPresenter.cs`
- alias `PostRun = 1` em `Experience/PostRun/Ownership/PostRunFlowEvents.cs`

## 6. Invariantes finais

- `RunResultStage` nunca decide navegacao macro.
- `RunDecision` nunca e phase-owned.
- `Continue` sempre e o unico handoff para `RunDecision`.
- nao existe busca por candidatos como resolucao canonica.
- existe no maximo 1 presenter adotavel por tipo no escopo correto.
- nao existe trilho paralelo canonico.
- nao existe compat lane canonica.
- nao existe `Task` como semantica de negocio do stage local.
- o boundary canonico nao depende de `PostRun` legado como centro semantico.
- `Skip -> Playing` nao faz parte do contrato canonico nem do plano.

## 7. Validacao final prevista

- `Victory -> Continue -> RunDecision -> downstream`
- `Defeat -> Continue -> RunDecision -> downstream`

As duas validacoes devem confirmar:

- a saida de `RunResultStage` respeita o handoff unico por `Continue`
- `RunDecision` so aparece apos `Continue`
- o downstream final e macro-owned
- nao ha fallback para trilho paralelo, compat lane ou heuristica de resolucao

## 8. Criterio de sucesso da reconstrucao

Ao final da reconstrucao:

- o post-run e um boundary unico, tipado e deterministico
- `RunResultStage` e `RunDecision` estao separados por ownership e contrato
- o caminho canonico nao preserva heranca estrutural de `PostRun` como centro
- o restante legado fica fora do caminho critico e nao participa da resolucao canonica
- `Skip -> Playing` foi removido do contrato e do plano
