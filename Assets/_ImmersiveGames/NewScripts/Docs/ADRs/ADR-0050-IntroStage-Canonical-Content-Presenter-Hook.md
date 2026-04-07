# ADR-0050 - IntroStage canonica com presenter/hook de conteudo

## Status
- Estado: Aceito
- Data: 2026-04-06
- Tipo: Direction / Canonical architecture
- Fonte de verdade canonica da IntroStage: este ADR.

## 1. Objetivo

`IntroStage` e a etapa canonica de entrada da phase.

Este ADR congela o contrato positivo da IntroStage como rail de entrada, apresentacao local e liberacao operacional para `Playing`.

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

## 5. Fluxo canonico

O fluxo canonico ocorre assim:

1. `SceneTransitionCompleted`
2. `IntroStage`
3. `Playing`

Esse fluxo descreve a entrada post-reveal da phase e a liberacao operacional para gameplay.

## 6. Lifecycle da IntroStage

`IntroStage` recebe a transicao da cena, resolve o presenter local e conclui a entrada para `Playing`.

Lifecycle:

1. recebe `SceneTransitionCompleted`
2. adota o contrato tipado da intro da phase
3. resolve o presenter valido no escopo correto
4. executa a apresentacao local da entrada
5. confirma a liberacao operacional para `Playing`
6. encerra o stage quando a entrada foi concluida

## 7. Presenter/hook local da phase

O presenter/hook da `IntroStage` e fornecido pela scene de conteudo e adotado pelo host local.

Leitura canonica:

- o presenter e scene-local e tipado
- o host resolve e adota uma instancia valida
- o presenter renderiza e emite intencao de complete/skip
- a assinatura canonica da session orienta a resolucao

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

- `SceneTransitionCompleted` -> `IntroStage`
- `IntroStage` -> `Playing`

## 11. Consequencias arquiteturais

Este contrato estabiliza a IntroStage como entrada canonica da phase.

Consequencias principais:

- a entrada da phase fica explicita e tipada
- o presenter local fica separado do rail macro
- o restart preserva coerencia operacional
- a resolucao por session fica deterministica
- a IntroStage pode evoluir sem reabrir a fronteira da entrada
