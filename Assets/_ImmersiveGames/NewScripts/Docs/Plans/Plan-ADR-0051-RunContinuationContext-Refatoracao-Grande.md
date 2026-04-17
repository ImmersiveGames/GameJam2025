# Plan - ADR-0051 RunContinuationContext Refatoracao Grande

## 1. Objetivo

Implementar o ADR-0051 com shape canonico correto, separando de forma clara:

- fechamento semantico da run
- contexto canonico de continuidade
- selecao/confirmacao da continuidade
- execucao downstream da continuidade
- navegacao pura de phase

O plano nao pode reabrir a confusao historica entre continuity e reset/restart.

## 2. Direcao Arquitetural

O desenho alvo exige:

- `RunContinuationOwnershipService` como owner concreto no nivel de `GameplaySessionFlow`
- `RunContinuationContext` como contrato central
- `RunResultStage` e `RunDecision` como consumidores
- `GameRunEndedEventBridge` como transporte fino
- `GameplaySessionFlowContinuityService` apenas como executor downstream resolvido
- `PhaseNavigation` fora do rail de continuidade
- `PostRunOverlayController` como presenter puro

## 3. Fases de Implementacao

### Fase 0 - Congelamento de contrato e naming canonicos

Objetivo:

- congelar a linguagem canonica antes de qualquer implementacao estrutural
- evitar churn desnecessario nas fases seguintes

Arquivos/componentes:

- `ADR-0051`
- `ADR-0051-Fase-0-Congelamento-Naming-Contrato`
- `RunContinuationOwnershipService`
- `RunContinuationContext`
- `GameRunEndedEventBridge`

Contratos/nomes congelados:

- owner canonico: `RunContinuationOwnershipService`
- contexto canonico: `RunContinuationContext`
- enum minimo: `RunContinuationKind`
- continuidades v1: `AdvancePhase`, `RestartCurrentPhase`, `ExitToMenu`, `TerminateRun`

Regras de boundary:

- `GameRunEndedEventBridge` e apenas transporte fino
- `RunContinuationContext` nao carrega comando de execucao
- `RunDecision` nao classifica nem monta execucao
- `PhaseNavigation` fica fora do rail semantico de continuidade
- `RestartCurrentPhase` deve permanecer protegido e resolver para o rail proprio de phase reset
- `AdvancePhase` pode existir, mas com tratamento editorial explicito, nao como navegacao escondida

Decisao de seam:

- o bridge atual sobrevive apenas como transporte operacional transitorio
- ele nao e owner semantico
- ele nao materializa o contexto canonico

Pronto quando:

- nomes centrais estao congelados
- o owner concreto esta explicito
- o contexto nao mistura selecao e execucao
- o bridge ficou classificado como transporte fino

### Fase 0.5 - Redesenho estrutural do boundary

Objetivo:

- reduzir ambiguidade antes de tocar em comportamento
- deixar o shape canonico legivel para os consumidores

Arquivos/componentes:

- `ADR-0051`
- `Plan-ADR-0051-RunContinuationContext-Refatoracao-Grande`

Saidas esperadas:

- owner concreto unico
- momento de nascimento do contexto
- separacao entre contexto, selecao, execucao e phase navigation
- protecao explicita de `RestartCurrentPhase`
- tratamento editorial explicito de `AdvancePhase`

Pronto quando:

- nao ha mais linguagem que permita bridge transitorio eterno como locus semantico
- a selecao nao e confundida com execucao

### Fase 1 - Fundacao do owner e do contexto canonico

Objetivo:

- materializar o novo owner de continuidade e o contexto canonico

Arquivos/componentes:

- `GameplaySessionFlow`
- `RunContinuationOwnershipService`
- contratos novos de continuidade

Contratos novos:

- `RunContinuationContext`
- contratos de continuidade minima de v1
- owner canonico de continuidade

Contratos a substituir:

- contratos estreitos de pos-run que infiram continuidade sem contexto

Riscos:

- duplicar fechamento entre contexto novo e contratos antigos
- misturar continuidade com navigation

Pronto quando:

- o contexto nasce imediatamente apos o fechamento
- o owner concreto e unico
- o payload minimo e legivel

### Fase 2 - Transporte fino e materializacao canonica

Objetivo:

- manter `GameRunEndedEventBridge` apenas como transporte fino
- materializar o contexto no owner concreto

Arquivos/componentes:

- `GameRunEndedEventBridge`
- `RunContinuationOwnershipService`

Contratos novos:

- evento/contrato de transporte para o owner de continuidade

Contratos a remover ao final:

- qualquer semantica de continuidade dentro do bridge

Riscos:

- manter o bridge como owner semantico oculto
- materializar o contexto tarde demais

Pronto quando:

- o bridge apenas encaminha o fato terminal
- o owner concreto materializa o contexto

### Fase 3 - Reescrita do RunResultStage

Objetivo:

- fazer `RunResultStage` consumir o contexto canonico

Arquivos/componentes:

- `RunResultStageContracts`
- `RunResultStageOwnershipService`
- presenters do `RunResultStage`

Contratos novos:

- entrada baseada em `RunContinuationContext`

Contratos a substituir:

- `RunResultStage(RunEndIntent, RunResult)`

Contratos a remover ao final:

- stage como consumidor apenas de `RunResult`

Riscos:

- transformar o stage em owner de continuidade
- quebrar o caminho `skip/no-content`

Pronto quando:

- o stage e opcional
- o stage consome contexto, mas nao decide continuidade

### Fase 4 - Reescrita do RunDecision

Objetivo:

- fazer `RunDecision` consumir o contexto canonico e remover a decisao local estreita

Arquivos/componentes:

- `RunDecisionContracts`
- `RunDecisionOwnershipService`
- `PostRunOverlayController`

Contratos novos:

- `RunDecision` baseado em `RunContinuationContext`
- contrato de selecao/confirmacao de continuidade

Contratos a substituir:

- `RunDecision(RunEndIntent, RunResult)`
- dependencia do overlay em resultado estreito

Contratos a remover ao final:

- `RunDecision` antigo como contrato central

Riscos:

- deixar o presenter decidir semantica
- reintroduzir destino automatico sem boundary claro

Pronto quando:

- `RunDecision` consome `RunContinuationContext`
- pode escolher ou confirmar continuidade
- nao define o conjunto canonico de continuidades validas

### Fase 5 - Separacao de selecao, execucao e phase navigation

Objetivo:

- tirar navegacao pura do rail de continuidade
- deixar `GameplaySessionFlowContinuityService` apenas como executor downstream resolvido

Arquivos/componentes:

- `GameplaySessionFlowContinuityService`
- contratos de navegacao
- `PhaseNavigation`

Contratos novos:

- executor downstream de continuidade resolvida

Contratos a substituir:

- `GameplaySessionFlowContinuityService` como mistura de continuidade, navigation e reset/restart

Contratos a remover ao final:

- navegacao editorial dentro do rail de continuidade

Riscos:

- quebrar restart/menu ao extrair execucao
- duplicar roteamento entre executor e navigation

Pronto quando:

- `GameplaySessionFlowContinuityService` executa apenas continuidade resolvida
- `RestartCurrentPhase` resolve para o rail proprio de phase reset
- `AdvancePhase` entra em tratamento editorial explicito e so entao aciona `PhaseNavigation`
- `PhaseNavigation` permanece fora do rail semantico de continuidade

### Fase 6 - Presenter puro e limpeza de contratos estreitos

Objetivo:

- reduzir o overlay a presenter puro e remover contratos estreitos remanescentes

Arquivos/componentes:

- `PostRunOverlayController`
- `PostRunResultContracts`
- `RunEndRailInstaller`

Contratos novos:

- presenter puro baseado no contexto canonico

Contratos a substituir:

- `PostRunResultService`
- `PostRunResult`
- botoes que disparam execucao downstream sem mediacao canonica

Contratos a remover ao final:

- `PostRunResultService`
- `PostRunResult`

Riscos:

- manter contrato estreito por compatibilidade interna
- deixar o presenter com poder de decisao

Pronto quando:

- o overlay so apresenta e emite escolha/confirmacao
- o owner correto ja possui a continuidade canonica

## 4. Ordem Recomendada

1. Fase 0
2. Fase 0.5
3. Fase 1
4. Fase 2
5. Fase 3
6. Fase 4
7. Fase 5
8. Fase 6

## 5. Dependencias

- Fase 1 depende da Fase 0 e da Fase 0.5
- Fase 2 depende da Fase 0, da Fase 0.5 e da Fase 1
- Fase 3 depende da Fase 0, da Fase 1 e da Fase 2
- Fase 4 depende da Fase 0, da Fase 1 e da Fase 2
- Fase 5 depende da Fase 4
- Fase 6 depende da Fase 4 e da Fase 5

## 6. Desenho Final

### Deve morrer

- `PostRunResult` como contrato central
- `IPostRunResultService` como fonte canonica de semantica
- `RunDecision` estreito baseado apenas em `RunEndIntent` + `RunResult`
- `GameplaySessionFlowContinuityService` como lugar de navegacao pura ou hub de selecao
- `PostRunOverlayController` como decisor de continuidade
- qualquer bridge que invente semantica de continuidade
- qualquer uso de `PhaseNavigation` como parte do rail de fechamento

### Permanece

- `RunContinuationOwnershipService`
- `RunContinuationContext`
- `RunEndIntent`
- `GameplaySessionFlow` como owner canonico do contexto de continuidade
- `RunResultStage` como stage opcional consumidor
- `RunDecision` como consumidor/confirmador do contexto
- `PhaseNavigation` como dominio neutro e editorial
- `GameplaySessionFlowContinuityService` como executor downstream resolvido
- `PostRunOverlayController` como presenter puro
- `PhaseDefinitionAsset` como authoring de closure e policy

## 7. Tipo de Refatoracao

Recomendacao final: **refatoracao grande**.

Justificativa:

- o desenho ideal exige um owner canonico concreto
- contratos estreitos precisam ser substituidos
- navegacao pura deve sair do rail de continuidade
- o overlay precisa perder ownership semantico
- o executor downstream precisa ser separado do contrato semantico
