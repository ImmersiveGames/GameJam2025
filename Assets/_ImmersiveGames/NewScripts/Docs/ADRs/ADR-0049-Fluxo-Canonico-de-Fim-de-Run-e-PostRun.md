# ADR-0049 - Fluxo canonico de fim de run e postrun

## Status
- Estado: Aceito
- Data: 2026-04-06
- Tipo: Direction / Canonical architecture

## 1. Objetivo

O termo `PostRun` neste ADR e historico/compatibilidade de nomenclatura. O contrato canonico atual separa `RunResultStage` como saida local da phase e `RunDecision` como continuidade macro/gameplay.

`RunEndIntent(reason)` marca a entrada da run em termino.

`RunResultStage` e `RunDecision` sao os dois estagios canonicos do fim de run.

Este ADR congela o contrato pos-`Playing` como fluxo positivo, tipado e operacional, separando o fim de run da IntroStage e da montagem inicial da fase.

`RunResultStage` e o espelho de saida de `IntroStage`: um stage local `phase-owned`, quando presente, recebe a `reason`, executa o fechamento local da phase e encerra por acao explicita de `Continue`.

`RunDecision` e `macro-owned` e decide a acao downstream final, sem absorver `RunRestart` como ownership semantico.

`RunContinuation` pertence ao fluxo macro de continuidade apos o fechamento local da phase, nao ao "post-run local".

Este ADR nao trata navegacao ordinal phase-local. `NextPhaseAsync` e `RestartCatalogAsync` pertencem ao ADR-0053 e nao substituem `RunResultStage` nem `RunDecision`.

## 2. Escopo

Este ADR cobre:

- a entrada da run em `RunEndIntent(reason)`
- o processamento local de `RunResultStage`
- a continuidade macro em `RunDecision`
- a saida final para a acao downstream

## 3. Estrutura do fim de run

O fim de run e estruturado em tres pontos canonicos:

1. `RunEndIntent(reason)`
2. `RunResultStage`
3. `RunDecision`

O fluxo se completa quando `RunResultStage` entrega `Continue` e a decisao macro escolhe a acao final downstream.
Quando a phase nao fornecer `RunResultStage`, o lifecycle deve registrar `skip/no-content` explicito e seguir para `RunDecision`.

## 4. Ownership

`RunResultStage` e `phase-owned`.

`RunDecision` e `macro-owned`.

O owner local fecha o resultado da phase, e o owner macro decide a continuidade final da run.

## 5. Fluxo canonico

O fluxo canonico ocorre assim:

1. `Playing`
2. `RunEndIntent(reason)`
3. `RunResultStage`
4. `Continue`
5. `RunDecision`
6. acao final downstream

Esse fluxo preserva a transicao entre o encerramento local da phase e a decisao macro de continuidade.

## 6. Lifecycle de `RunResultStage`

`RunResultStage` recebe a reason do fim de run, executa o fechamento local da phase e termina por `Continue`.

O presenter local de `RunResultStage`, quando presente, existe como conteudo local da phase/cena e e adotado pelo host tipado no escopo correto. Ele e a projecao concreta do stage, nao a prova da existencia do stage.

Lifecycle:

1. recebe `RunEndIntent(reason)`
2. adota a `reason` como contrato de entrada
3. executa o conteudo local da phase
4. apresenta o resultado local da phase por presenter tipado, quando presente
5. conclui o stage por `Continue`

Se nao houver presenter/conteudo local de `RunResultStage`, o lifecycle faz `skip/no-content` explicito e segue o contrato canonico sem fatal.

Semantica:

- `Continue` encerra o stage e segue para `RunDecision`

## 7. Lifecycle de `RunDecision`

`RunDecision` recebe a continuidade apos `RunResultStage` e materializa a decisao macro final.

Lifecycle:

1. recebe a continuidade apos `RunResultStage`
2. adota o contexto final da continuidade da run
3. resolve o presenter tipado de decisao no escopo macro correto
4. apresenta restart, menu ou acao macro equivalente
5. conclui o stage com a acao downstream escolhida

## 8. Tipagem, registro e cardinalidade

Os contratos sao tipados separadamente:

- `RunResult`
- `RunDecision`
- `RunRestart` como intencao downstream separada do overlay final

Cada tipo possui infraestrutura propria, registro proprio e resolucao deterministica.

Cardinalidade:

- existe no maximo 1 presenter valido para `RunResult` no escopo correto, quando o stage estiver presente
- existe no maximo 1 presenter valido para `RunDecision` no escopo correto
- a resolucao por tipo e registro e deterministica

## 9. Hand-offs canonicos

Os hand-offs canonicos sao:

- `Playing` -> `RunEndIntent(reason)`
- `RunEndIntent(reason)` -> `RunResultStage`
- `RunResultStage` -> `Continue`
- `skip/no-content` -> `RunDecision` quando `RunResultStage` nao estiver presente
- `Continue` -> `RunDecision`
- `RunDecision` -> acao final downstream

## 10. Consequencias arquiteturais

Este contrato estabiliza o fim de run como uma sequencia clara entre phase e macro.

Consequencias principais:

- `IntroStage` e o espelho de entrada da phase; `RunResultStage` e o espelho de saida
- o resultado local da run permanece phase-owned
- o fechamento local da run termina por `Continue` e segue para a decisao macro
- a decisao final permanece macro-owned
- o fluxo pos-run fica separado da IntroStage
- a tipagem de resultado e decisao fica isolada por contrato
- o fim de run pode evoluir sem reabrir a entrada da phase
