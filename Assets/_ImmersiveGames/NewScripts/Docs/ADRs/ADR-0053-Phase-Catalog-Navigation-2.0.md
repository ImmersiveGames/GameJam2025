# ADR-0053 - Phase Catalog Navigation 2.0

## Status
- Estado: Aceito
- Data: 2026-04-14
- Tipo: Direction / Canonical architecture

## 1. Contexto

O `Phase Catalog` e a camada canonica de ordem e navegacao ordinal das phases.

Ele resolve `CurrentCommitted`, `PendingTarget`, `ResolveNext`, `ResolvePrevious`, `ResolveSpecificPhase`, `NextPhaseAsync` e `RestartCatalogAsync` sobre uma ordem fixa inicial.

Esta camada nao e owner do conteudo runtime da phase, nem absorve continuidade macro, reset de phase atual ou branching por condicao.

Estado final implementado:

- `PhaseCatalogNavigationService` e owner da resolucao ordinal e do commit
- `PhaseCatalogRuntimeStateService` e writer unico de estado
- `PhaseNextPhaseService` ficou como handoff/materializacao phase-local
- `GameplaySessionFlowContinuityService` permanece apenas como bridge

## 2. Decisao

O catalogo 2.0 nasce com ordem fixa e navegacao ordinal runtime.

Branching condicional fica fora da primeira versao e, se existir no futuro, deve viver acima do catalogo como politica externa.

O catalogo controla o destino canonico da navegacao, mas nao o conteudo ativo da phase.
`NextPhaseAsync` e `RestartCatalogAsync` sao operacoes canonicas da superficie final; isso nao transforma o catalogo em owner da continuidade macro.

## 3. Responsabilidades

### Fica no Phase Catalog

- ordem canonica das phases
- navegacao ordinal runtime
- resolucao de `CurrentCommitted`
- manutencao de `PendingTarget`
- looping ao fim da lista
- contagem de loop em runtime
- confirmacao canonica da mudanca de phase
- observabilidade minima de estado do catalogo

### Fica fora do Phase Catalog

- `RestartCurrentPhase`
- `RunDecision`
- `RunContinuation`
- eligibility / availability
- progressao persistida
- dificuldade por volta
- mutacao dinamica do catalogo em runtime
- branching por condicao

## 4. Shape inicial

O shape inicial deve ser pequeno e explicito:

- `PhaseCatalog`
- `PhaseCatalogEntry`
- `PhaseCatalogState`
- `CurrentCommitted`
- `PendingTarget`
- `Looping`
- `LoopCount`
- `ResolveNext`
- `ResolvePrevious`
- `ResolveSpecificPhase`
- `NextPhaseAsync`
- `RestartCatalogAsync`

### 4.1 Por que `CurrentCommitted` e `PendingTarget`

`CurrentCommitted` e o destino canonico confirmado.

Ele muda quando a navegacao foi aceita como proxima phase canonica, antes de aplicar reset ou conteudo.

`PendingTarget` e o alvo ainda nao aplicado.

Ele existe para separar clique bruto, resolucao de destino e confirmacao operacional.

### 4.2 Por que `Looping` e `LoopCount` ficam dentro

`Looping` pertence ao catalogo porque a politica de fim de lista e parte da ordem canonica.

`LoopCount` nasce no runtime do catalogo porque e um efeito interno da navegacao ordinal.

Isso nao transforma o catalogo em sistema de dificuldade ou progressao.

### 4.3 Por que `RestartCurrentPhase` fica fora

`RestartCurrentPhase` reinicia a phase ja resolvida no runtime.

Ele depende do destino canonico ja conhecido, mas nao altera a ordem do catalogo.

Por isso ele pertence ao fluxo de phase/runtime, nao ao owner da navegacao ordinal.

Na implementacao final, esse compat rail foi removido da superficie publica.

## 5. Eventos canonicos minimos iniciais

O catalogo deve prever poucos eventos canonicamente, de forma intencionalmente pequena nesta etapa:

- phase confirmada no catalogo
- target pendente alterado
- loop concluido
- loop count atualizado

Isso nao congela a evolucao futura dos eventos.

Eventos de tooling QA podem existir, mas nao definem o contrato canonico.

## 6. Relacao com phase/runtime

O catalogo e owner da ordem e da navegacao ordinal runtime.

`GameplayPhaseFlow` continua owner do conteudo/runtime ativo da phase.

O catalogo resolve o proximo destino; o gameplay flow aplica o conteudo ativo daquela phase.

`PhaseNextPhaseService` permanece como handoff/materializacao phase-local e nao como owner semantico da ordem.

## 7. Relacao com macro continuity

O catalogo nao conhece `RunDecision` como regra de negocio interna.

O catalogo nao conhece `RunContinuation` como contrato semantico central.

Ele apenas resolve navegacao quando solicitado.

## 8. Tooling e QA

Tooling QA e contrato canonico sao separados.

Ferramentas podem usar as mesmas capacidades reais do catalogo, mas nao sao a fonte de verdade do contrato nem owner do estado canonico do catalogo.

Isso permite expor `Previous` e `GoToSpecificPhase` primeiro por painel ou tooling, sem reduzir o contrato ao caso de teste.

Na implementacao final, o `PhaseNavigationQaPanel` foi rebaixado para consumidor do rail canonico e nao e mais owner semantico de `GameRunEndRequested` ou `RunDecision`.

## 9. Fora de escopo da primeira versao

Ficam explicitamente fora:

- branching por condicao
- phase eligibility / availability
- progressao persistida
- dificuldade por volta
- mutacao dinamica do catalogo em runtime
- acoplamento com `RunDecision`
- `RestartCurrentPhase`

### 9.1 Fora de escopo da implementacao concluida

Este ADR nao cobre o fechamento de resultado final de phase ao avancar de phase, nem a semantica de `RunResultStage` antes da decisao do proximo destino.

Esse tema depende de auditoria/plano proprio de `PostRun` e de continuidade macro, e permanece explicitamente fora do contrato deste ADR.

## 10. Extensoes futuras

Este contrato deixa abertas, de forma explicita, estas evolucoes:

- elegibilidade e bloqueio de phases
- progressao persistida
- dificuldade baseada em `LoopCount`
- override ou mutacao de catalogo runtime
- navegacao condicional acima do catalogo

## 11. Consequencias

O catalogo passa a ter um shape minimo e legivel para uso real, sem invadir responsabilidade de phase runtime nem de macro continuity.

Consequencias principais:

- a ordem deixa de ser implícita
- o destino canonico fica separado do alvo pendente
- looping fica modelado como responsabilidade do catalogo
- o runtime da phase continua isolado do owner de navegacao
- o contrato nasce pronto para uso real, mas sem absorver branching, progressao ou dificuldade

## 12. Estado final implementado

Depois das Etapas 1-5 do cleanup, o trilho ordinal phase-local ficou consolidado assim:

- `NextPhaseAsync` e o rail canonico principal de avancar
- `RestartCatalogAsync` e o rail canonico de reiniciar o catalogo
- looping continua permitido
- finite bloqueia antecipadamente no fim do catalogo antes de abrir trilho macro
- `LoopCount` continua modelado no runtime do catalogo
- `AdvancePhaseAsync` e `RestartCurrentPhaseAsync` foram removidos
- `PhaseCatalogNavigationRequestedEvent` e `PhaseCatalogNavigationCompletedEvent` foram removidos
- o leak macro por `RunDecision/*` no factory de composicao local foi removido
- o papel macro indevido do `PhaseNavigationQaPanel` foi removido

## 13. Fora de escopo da implementacao concluida

Este ADR nao resolve o tema de "phase terminou e precisa mostrar result/final stage antes de decidir o proximo destino".

Esse fechamento pertence a `PostRun` e sera tratado em auditoria/plano proprio.
