# Plan - GameplaySessionFlow Phase 1

## 1. Objetivo do plano

Introduzir o primeiro bloco interno do `Gameplay Runtime Composition`, chamado `GameplaySessionFlow`, sem mexer em `SceneFlow`, `WorldReset`, `Navigation`, `Loading/Fade` ou no core de estado do `RuntimeFlow`.

## 2. Escopo da Fase 1

Entra no corte:

- `LevelMacroPrepareService`
- `GameRunOutcomeService`
- `PostRunHandoffService`
- `PostRunOwnershipService`
- `RestartContextService`
- `LevelStageOrchestrator` como ponte de sequencing

Ficam fora:

- `SceneFlow`
- `WorldReset`
- `Navigation`
- `Loading/Fade`
- `GameLoopService`
- `GameLoopStateMachine`

Continuam como bridges:

- `GameLoopSceneFlowSyncCoordinator`
- `GameLoopInputCommandBridge`
- `GameRunEndedEventBridge`
- `GameLoopCommands`
- `GameRunOutcomeRequestBridge`
- `GameLoopStartRequestEmitter`

## 3. Slices da Fase 1

### Slice 1 - Borda de ownership

Objetivo: declarar `GameplaySessionFlow` como owner da faixa jogavel, sem mudar comportamento.

Arquivos/areas provaveis:

- `LevelMacroPrepareService`
- `RestartContextService`
- composicao/instalacao do subsistema de gameplay

Mudanca arquitetural:

- separa ownership do fluxo jogavel do backbone

O que continua igual:

- rotas, eventos e transicoes atuais

Bridges temporarias:

- nenhuma nova

Criterios de conclusao:

- o novo owner fica explicito e o comportamento continua igual

Riscos:

- renomear sem mudar ownership real

Reversibilidade:

- alta

### Slice 2 - Entrada e intro

Objetivo: fazer o comeco da sessao e a intro serem lidos como parte do `GameplaySessionFlow`.

Arquivos/areas provaveis:

- `LevelMacroPrepareService`
- `LevelStageOrchestrator`
- `GameLoopInputCommandBridge`

Mudanca arquitetural:

- a entrada da experiencia jogavel passa a pertencer ao novo bloco

O que continua igual:

- `GameLoopService` ainda libera `Playing`

Bridges temporarias:

- `GameLoopInputCommandBridge`
- `LevelStageOrchestrator`

Criterios de conclusao:

- o inicio da sessao nao depende de `GameLoop` como dono semantico

Riscos:

- deixar a ponte virar owner

Reversibilidade:

- media-alta

### Slice 3 - Outcome e fim da run

Objetivo: transferir outcome para `GameplaySessionFlow`.

Arquivos/areas provaveis:

- `GameRunOutcomeService`
- `GameRunEndedEventBridge`
- `GameLoopCommands` apenas para evitar crescimento

Mudanca arquitetural:

- victory/defeat deixam de ser lidos como responsabilidade do backbone

O que continua igual:

- `GameLoopService` continua fazendo a transicao terminal

Bridges temporarias:

- `GameRunEndedEventBridge`

Criterios de conclusao:

- fim da run tem owner jogavel claro

Riscos:

- colocar regra de outcome dentro da bridge

Reversibilidade:

- media

### Slice 4 - Post-run

Objetivo: concentrar handoff, ownership e resultado final no novo bloco.

Arquivos/areas provaveis:

- `PostRunHandoffService`
- `PostRunOwnershipService`
- `PostRunResultService`

Mudanca arquitetural:

- o post-run passa a ser parte da composicao jogavel

O que continua igual:

- menu e navegacao permanecem fora desta fase

Bridges temporarias:

- `GameRunEndedEventBridge`

Criterios de conclusao:

- existe um dono unico para o estado de post-run

Riscos:

- misturar UI, resultado e handoff em um unico bridge

Reversibilidade:

- media-alta

### Slice 5 - Retry, restart e advance

Objetivo: fazer retry/restart/advance pertencerem a sessao jogavel.

Arquivos/areas provaveis:

- `RestartContextService`
- `PostLevelActionsService`
- `GameLoopCommands`

Mudanca arquitetural:

- retry/restart/advance deixam de parecer comportamento do loop macro

O que continua igual:

- `GameLoopCommands` segue existindo, mas mais fino

Bridges temporarias:

- `GameLoopCommands`

Criterios de conclusao:

- a sessao jogavel consegue se remontar sem reintroduzir ownership no backbone

Riscos:

- expandir `GameLoopCommands`

Reversibilidade:

- media

## 4. Ordem recomendada

1. Slice 1 - Borda de ownership
2. Slice 2 - Entrada e intro
3. Slice 3 - Outcome e fim da run
4. Slice 4 - Post-run
5. Slice 5 - Retry, restart e advance

## 5. Pontos de validacao entre slices

- o backbone continua sem dependencia nova de gameplay
- `RuntimeFlow` segue apenas como estado macro e execucao segura
- as bridges continuam finas
- o novo owner ja explica a sessao jogavel sem `GameLoop` como centro semantico

## 6. Resultado esperado ao final da Fase 1

`GameplaySessionFlow` passa a ser reconhecivel como dono da experiencia jogavel, cobrindo entrada, intro, outcome, post-run e retry/restart/advance. O backbone continua intacto. Os nomes historicos ainda coexistem, mas nao definem a arquitetura alvo.

## 7. Arquivos mais importantes para abrir primeiro

1. `Orchestration/LevelLifecycle/Runtime/LevelMacroPrepareService.cs`
2. `Orchestration/LevelLifecycle/Runtime/RestartContextService.cs`
3. `Orchestration/GameLoop/RunOutcome/GameRunOutcomeService.cs`
4. `Experience/PostRun/Handoff/PostRunHandoffService.cs`
5. `Experience/PostRun/Ownership/PostRunOwnershipService.cs`
6. `Orchestration/LevelLifecycle/Runtime/LevelStageOrchestrator.cs`
7. `Orchestration/GameLoop/Bridges/GameRunEndedEventBridge.cs`
8. `Orchestration/GameLoop/Bridges/GameLoopSceneFlowSyncCoordinator.cs`

## 8. Riscos principais

- expandir bridges em vez de reduzi-las
- tocar em `SceneFlow` ou `WorldReset` antes da hora
- manter `GameLoopCommands` como hub de gameplay
- usar nomes historicos como justificativa de ownership final

