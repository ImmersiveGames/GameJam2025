# Plan - RunEnd Rail Refactor F0 Freeze

## 1. Resumo executivo

O rail final de gameplay continua carregando heranca conceitual de `PostRun` e bridges legadas de `Level`.
Isso nao e um bug local de overlay ou de timing: e uma refatoracao de modelo e ownership.

Este plano congela o canon antes da migracao completa para evitar regressao semantica nas proximas etapas.

## 2. Canon congelado

- `RunEndIntent`
- `RunResultStage` opcional
- `RunDecision`
- `Overlay`
- `PostRun` apenas como alias historico e documental

## 3. Tese estrutural

- `RunResultStage` e simetrico ao `IntroStage`.
- `PhaseDefinition` decide se `RunResultStage` existe.
- `GameplaySessionFlow` e o owner semantico do rail final.
- `Level` nao pode permanecer no caminho critico.
- `Overlay` e somente projecao visual downstream de `RunDecision`.

## 4. Invariantes anti-regressao

- `Overlay` nunca abre antes de `RunDecisionEntered`.
- `Overlay` nunca abre em `RunEndIntentAccepted`.
- `Overlay` nunca abre em `RunResultStageEntered`.
- Se nao houver `RunResultStage`, o fluxo vai direto para `RunDecision`.
- `Level` e `HasPostRunReactionHook` nao podem decidir o rail.
- `RunDecisionEntered` so ocorre depois de:
  - `RunEndIntentAccepted`
  - `RunResultStageCompleted`, quando houver stage
- `PostRun` nao pode voltar a ser tratado como conceito central em novos cortes.

## 5. Plano macro

1. F0 freeze
2. F1 introduzir contratos e eventos novos
3. F2 ligar `PhaseDefinition` ao `RunResultStage`
4. F3 migrar o caminho critico do runtime
5. F4 expurgar `Level`
6. F5 limpar nomes e aliases historicos

## 6. Criterios de pronto globais

- rail final controlado por `GameplaySessionFlow`
- `PhaseDefinition` decide `RunResultStage`
- `Level` fora do caminho critico
- `Overlay` apenas downstream de `RunDecision`
- sem caminhos paralelos

## 7. Fora de escopo do F0

- nenhuma mudanca de runtime
- nenhuma renomeacao de codigo ainda
- nenhuma migracao parcial neste corte

## 8. Fontes de verdade

Este plano se apoia e deve ser lido junto de:

- `Docs/ADRs/ADR-0001-Glossario-Fundamental-Contextos-e-Rotas-v2.md`
- `Docs/ADRs/ADR-0044-Baseline-4.0-Ideal-Architecture-Canon.md`
- `Docs/ADRs/ADR-0046-GameplaySessionFlow-como-primeiro-bloco-interno-do-Gameplay-Runtime-Composition.md`
- `Docs/ADRs/ADR-0047-Gameplay-Phase-Construction-Pipeline-dentro-do-GameplaySessionFlow.md`
- os docs normalizados do rail final

Se houver conflito com docs legados de `PostRun`, este F0 prevalece para a refatoracao do rail final.

## 9. Referencia obrigatoria para F1-F5

Este documento e a referencia formal obrigatoria para os proximos cortes F1-F5.
Nenhuma implementacao posterior deve reabrir o canon congelado aqui.
