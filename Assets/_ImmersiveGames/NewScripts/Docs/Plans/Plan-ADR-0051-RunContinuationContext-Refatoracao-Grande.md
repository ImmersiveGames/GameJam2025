# Plan - ADR-0051 RunContinuationContext Refatoracao Grande

## 1. Objetivo

Implementar o ADR-0051 com desenho canonico correto, substituindo contratos estreitos e separando de forma clara:

- fechamento semantico da run
- contexto canônico de continuidade
- decisao/apresentacao opcional de continuidade
- execucao downstream da continuidade
- navegacao pura de phase

## 2. Direcao Arquitetural

O desenho alvo exige:

- `RunContinuationContext` como contrato central
- ownership em `GameplaySessionFlow`
- `RunResultStage` e `RunDecision` como consumidores
- `PhaseNavigation` fora do rail de continuidade
- `GameplaySessionFlowContinuityService` apenas como executor downstream resolvido
- `PostRunOverlayController` como presenter puro

## 3. Fases de Implementacao

### Fase 0 - Congelamento de contrato e naming canonicos

Objetivo:

- congelar a linguagem canonica antes de qualquer implementacao estrutural
- evitar churn desnecessario nas fases seguintes

Arquivos/componentes:

- `ADR-0051`
- `GameRunEndedEventBridge`
- contratos centrais de continuidade e fechamento
- `GameplaySessionFlow` como owner alvo

Contratos novos ou nomes congelados:

- owner canônico recomendado: `RunContinuationOwnershipService`
- contexto canônico recomendado: `RunContinuationContext`
- enum mínimo recomendado: `RunContinuationKind`
- continuidades de v1: `AdvancePhase`, `RestartCurrentPhase`, `ExitToMenu`, `EndRun`

Contratos/nomes a evitar:

- `PostRunResult`
- `PostRun`
- `Exit`
- `RunOutcome` como semântica de continuidade
- qualquer nome que misture fechamento, decisão e execução downstream

Decisão de seam atual:

- `GameRunEndedEventBridge` permanece temporariamente como seam fino no início
- ele deve sobreviver apenas como ponte operacional transitória até a materialização canônica ficar no owner novo
- não deve continuar como owner semântico

Confirmação de boundary:

- contexto semântico: `RunContinuationContext`
- decisao: `RunDecision`
- execucao downstream: executor resolvido separado
- phase navigation: `PhaseNavigation`

Contratos que esta fase nao implementa:

- comportamento
- dispatch
- presenter
- navigation

Riscos:

- congelar nome errado e espalhar colisao semântica pelas fases seguintes
- tratar seam transitório como owner final
- misturar `allowed continuations` com `selected continuation`

Pronto quando:

- nomes centrais estao congelados
- o papel de cada boundary esta explicitado
- o seam atual foi classificado como transitório, nao semântico

### Fase 1 - Fundacao do contexto canonico

Objetivo:

- criar o novo contrato de continuidade e o owner semantico central

Arquivos/componentes:

- `Experience/PostRun`
- `GameplaySessionFlow` / `GameplayPhaseFlowService`
- contratos novos de continuidade

Contratos novos:

- `RunContinuationContext`
- contratos de continuidades minimas de v1
- owner canônico de continuidade

Contratos a substituir:

- `PostRunResult`
- `RunDecision` estreito baseado apenas em `RunEndIntent` + `RunResult`

Contratos a remover ao final:

- `PostRunResult` como contrato central

Riscos:

- duplicar fechamento entre contexto novo e contratos antigos
- misturar continuidade com navigation

Pronto quando:

- o contexto nasce e e exposto por owner canonico unico
- o payload minimo de continuidade existe e e legivel

### Fase 2 - Seam de fechamento e materializacao

Objetivo:

- mover a materializacao do contexto para o seam canonico de fechamento

Arquivos/componentes:

- `GameRunEndedEventBridge`
- `GameRunOutcomeService`
- owner novo de continuidade

Contratos novos:

- evento/contrato de materializacao de `RunContinuationContext`

Contratos a substituir:

- fluxo que vai de outcome direto para stage/decision sem contexto canônico

Contratos a remover ao final:

- dependência operacional de `IPostRunResultService` como fonte canônica

Riscos:

- manter o bridge como owner semantico oculto
- materializar o contexto tarde demais

Pronto quando:

- o contexto e materializado antes de `RunResultStage` e `RunDecision`
- `Victory/Defeat/ExitToMenu` já viram semântica de continuidade

### Fase 3 - Reescrita do RunResultStage

Objetivo:

- fazer `RunResultStage` consumir o contexto canônico

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

- o stage é opcional
- o stage consome contexto, mas nao decide continuidade

### Fase 4 - Reescrita do RunDecision

Objetivo:

- fazer `RunDecision` consumir o contexto canônico e remover a decisão local estreita

Arquivos/componentes:

- `RunDecisionContracts`
- `RunDecisionOwnershipService`
- `PostRunOverlayController`

Contratos novos:

- `RunDecision` baseado em `RunContinuationContext`
- contrato de selecao/confirmacao de continuidade

Contratos a substituir:

- `RunDecision(RunEndIntent, RunResult)`
- dependência do overlay em resultado estreito

Contratos a remover ao final:

- `RunDecision` antigo como contrato central

Riscos:

- deixar o presenter decidir semântica
- reintroduzir destino automático de `ExitToMenu`

Pronto quando:

- `RunDecision` consome `RunContinuationContext`
- pode escolher ou confirmar continuidade
- nao define o conjunto canonico de continuidades validas

### Fase 5 - Separacao de PhaseNavigation e executor downstream

Objetivo:

- tirar navegação pura do rail de continuidade

Arquivos/componentes:

- `GameplaySessionFlowContinuityService`
- contratos de navegacao
- `PhaseNavigation`

Contratos novos:

- executor downstream de continuidade resolvida

Contratos a substituir:

- `GameplaySessionFlowContinuityService` como mistura de continuidade e navegacao

Contratos a remover ao final:

- navegação editorial dentro do rail de continuidade

Riscos:

- quebrar restart/menu ao extrair navegação
- duplicar roteamento entre executor e navigation

Pronto quando:

- `GameplaySessionFlowContinuityService` executa apenas continuidade resolvida
- `PhaseNavigation` so entra quando a continuidade for `AdvancePhase`

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
- botões que disparam execução downstream sem mediação canônica

Contratos a remover ao final:

- `PostRunResultService`
- `PostRunResult`

Riscos:

- manter contrato estreito por compatibilidade interna
- deixar o presenter com poder de decisão

Pronto quando:

- o overlay so apresenta e emite intenção/seleção
- o owner correto já possui a continuidade canônica

## 4. Ordem Recomendada

1. Fase 0
2. Fase 1
3. Fase 2
4. Fase 3
5. Fase 4
6. Fase 5
7. Fase 6

## 5. Dependencias

- Fase 1 depende da Fase 0
- Fase 2 depende da Fase 0 e da Fase 1
- Fase 3 depende da Fase 0, da Fase 1 e da Fase 2
- Fase 4 depende da Fase 0, da Fase 1 e da Fase 2
- Fase 5 depende da Fase 4
- Fase 6 depende da Fase 4 e da Fase 5

## 6. Desenho Final

### Deve morrer

- `PostRunResult` como contrato central
- `IPostRunResultService` como fonte canonica de semantica
- `RunDecision` estreito baseado apenas em `RunEndIntent` + `RunResult`
- `GameplaySessionFlowContinuityService` como lugar de navegação pura
- `PostRunOverlayController` como decisor de continuidade
- qualquer bridge que invente semântica de continuidade
- qualquer uso de `PhaseNavigation` como parte do rail de fechamento

### Permanece

- `GameRunOutcomeService`
- `RunEndIntent`
- `GameplaySessionFlow` como owner canônico do contexto de continuidade
- `RunResultStage` como stage opcional consumidor
- `RunDecision` como consumidor/confirmador do contexto
- `PhaseNavigation` como domínio neutro
- `GameplaySessionFlowContinuityService` como executor downstream resolvido
- `PostRunOverlayController` como presenter puro
- `PhaseDefinitionAsset` como authoring de closure e policy

## 7. Tipo de Refatoracao

Recomendacao final: **refatoracao grande**.

Justificativa:

- o desenho ideal exige criar novo owner canonico
- contratos estreitos precisam ser substituidos
- navegação pura deve sair do rail de continuidade
- o overlay precisa perder ownership semantico
- o executor downstream precisa ser separado do contrato semantico
