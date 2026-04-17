# ADR-0050 - IntroStage canonica com presenter/hook de conteudo

> STATUS NORMATIVO: HISTORICO - ANTECEDENTE DA BASE 1.0, NAO FONTE NORMATIVA PRIMARIA.
> Em conflito, prevalecem ADR-0057, ADR-0056, ADR-0055, ADR-0058, ADR-0054 e ADR-0052.

## Status
- Estado: Aceito
- Data: 2026-04-06
- Tipo: Direction / Canonical architecture
- Fonte de verdade canonica da IntroStage: este ADR.

## 1. Objetivo

`IntroStage` e a etapa canonica de entrada da phase, depois que ela ja foi montada semanticamente.

Este ADR congela o contrato positivo da IntroStage como rail post-reveal de apresentacao local e liberacao operacional para `Playing`.
`IntroStage` acontece depois de `SceneTransitionCompleted` e antes de `Playing`, quando a phase fornece intro concreta. A ausencia de intro nao invalida a phase nem reclassifica o rail; nesse caso, o lifecycle faz `skip/no-content` explicito.
`IntroStage` e o espelho de entrada da phase; `RunResultStage` e o espelho de saida, conforme o contrato canonico de fim de run.

## 2. Escopo

Este ADR cobre:

- a entrada canonica via `SceneTransitionCompleted`
- a execucao da `IntroStage`
- o presenter/hook local fornecido pela phase
- o rearme operacional de restart
- a tipagem e cardinalidade do contrato de entrada

## 3. Estrutura da IntroStage

`IntroStage` e estruturada como uma etapa canonica de `phase-owned`, com presenter local adotado pela cena de conteudo.

O contrato de entrada e organizado em torno de:

- orchestrator da entrada
- control service da etapa
- presenter host local
- presenter passivo da phase

## 4. Ownership

`IntroStage` e `phase-owned`.

O rail governa o estado canonico da IntroStage, enquanto o presenter local governa apenas a projecao concreta da entrada.

### 4.1 Corte semantico congelado

Este corte congela a separacao entre `phase-local` e `macro/gameplay`:

- `phase-local`: `IntroStage`, `Playing`, `RunResultStage`
- `macro/gameplay`: `RunDecision`, `RunContinuationContext`, `RunContinuationSelection`, `SessionTransition`

Regras de precedencia:

- o baseline nao abre IntroStage
- o baseline apenas reinicia / rearma a phase por completo
- `IntroStage` e a entrada local da phase
- `RunResultStage` e a saida local da phase
- `RunDecision` e `RunContinuation*` pertencem ao boundary macro/gameplay
- o restart da mesma phase devolve o controle ao pipeline local da phase
- a reentrada local usa uma identidade monotonica por entrada valida da phase
- quando houver intro valida, a `IntroStage` volta a abrir na reentrada da phase

`IntroStage` e `RunResultStage` continuam sendo contratos materializados do phase runtime. O uso de `RunResultStage` como fechamento semantico antes de decidir o proximo destino nao foi resolvido pelo ADR-0053 e permanece dependente do plano/auditoria de `PostRun`.

## 5. Fluxo canonico

O fluxo canonico ocorre assim:

1. `PhaseSelected`
2. `ContentApplied`
3. derivacao de `SessionContext`, `PhaseRuntime` e `Players`
4. `SceneTransitionCompleted`
5. `IntroStage`
6. `Playing`

Esse fluxo descreve a montagem semantica protegida da phase antes do reveal e a entrada post-reveal da `IntroStage` para liberacao operacional do gameplay.

`Rules/Objectives` e `InitialState` foram removidos do canônico atual de `Phase` e nao fazem mais parte deste fluxo.

O sucesso da entrada da phase ocorre quando o conteudo local foi aplicado e o runtime minimo ja existe; os contratos de `Intro` e `RunResult`, quando presentes, seguem o rail tipado/canonico.
Por isso, `IntroStage` e parte do lifecycle da phase, mas nao a fronteira unica nem obrigatoria de sucesso.

## 6. Lifecycle da IntroStage

`IntroStage` recebe a transicao apos `SceneTransitionCompleted`, com a phase ja montada semanticamente, resolve o presenter local quando ele existir e conclui a entrada para `Playing`.

Lifecycle:

1. recebe `SceneTransitionCompleted`
2. recebe a phase ja montada semanticamente
3. adota o contrato tipado da intro da phase
4. resolve o presenter valido no escopo correto, quando houver intro concreta
5. executa a apresentacao local da entrada
6. confirma a liberacao operacional para `Playing`
7. encerra o stage quando a entrada foi concluida

Se a phase nao fornecer intro concreta, o lifecycle registra `skip/no-content` explicito e segue para `Playing` sem fatal.

## 7. Presenter/hook local da phase

O presenter/hook da `IntroStage` e fornecido pela scene de conteudo e adotado pelo host local.

Leitura canonica:

- o presenter e scene-local e tipado
- o host resolve e adota uma instancia valida
- o presenter renderiza e emite intencao de complete/skip
- a assinatura canonica da session orienta a resolucao
- o presenter nao aplica conteudo local nem deriva runtime

## 8. Rearme/restart

O restart alimenta uma nova session com contrato coerente de IntroStage.

Leitura canonica:

- o rearme e operacional
- o restart nao altera ownership
- a nova session nasce com contrato completo e surface valida

## 9. Tipagem, registro e cardinalidade

O contrato de entrada possui tipagem separada para `Intro`.

Cardinalidade:

- existe no maximo 1 presenter valido para `Intro` no escopo correto
- a resolucao e deterministica por tipagem e registro
- o presenter adotado deve ser unico para a session ativa

## 10. Hand-offs canonicos

Os hand-offs canonicos sao:

- `PhaseSelected` -> `ContentApplied` -> derivacao runtime -> `SceneTransitionCompleted` -> `IntroStage`
- `IntroStage` -> `Playing`

## 11. Consequencias arquiteturais

Este contrato estabiliza a IntroStage como entrada canonica da phase, sem reatribuir a ela o loading/preparation.

Consequencias principais:

- a entrada da phase fica explicita e tipada
- o presenter local fica separado do rail macro
- o restart preserva coerencia operacional
- a resolucao por session fica deterministica
- a IntroStage pode evoluir sem reabrir a fronteira da entrada
- o loading/preparation semantico fica fora da IntroStage

## 12. Observacao de lifecycle

Se no futuro houver skip ou resumo de intro, o contrato de sucesso da phase continua valido desde que o conteudo local tenha sido aplicado, o runtime minimo exista e os registros de lifecycle tenham sido feitos.
O presenter/hook local continua sendo a projecao concreta da entrada, nao o owner da phase.
