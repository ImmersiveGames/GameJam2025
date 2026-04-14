# ADR-0051 - Fluxo canonico de continuidade apos o fechamento de run

## Status
- Estado: Aceito
- Data: 2026-04-12
- Tipo: Direction / Canonical architecture

## 1. Contexto

Os ADRs canonicamente relevantes ja separam:

- `ADR-0001`: vocabulario fundamental de contexto, rota, resultado e intencao
- `ADR-0044`: centro semantico do gameplay, resultado e continuidade
- `ADR-0045`: `Gameplay Runtime Composition` como centro semantico do gameplay
- `ADR-0046`: `GameplaySessionFlow` como primeiro bloco do runtime
- `ADR-0047`: pipeline canonico da phase dentro de `GameplaySessionFlow`
- `ADR-0048`: `PhaseDefinition` como fonte de verdade autoral da fase jogavel
- `ADR-0049`: fim de run canonico com `RunEndIntent`, `RunResultStage` e `RunDecision`
- `ADR-0050`: `IntroStage` como rail local de entrada da phase

A frente de reset/restart ja foi separada em outra linha para manter o reset previsivel. Este ADR nao pode reabrir a mistura anterior entre fechamento, continuidade, restart/reset e navegacao.

O problema que ainda precisa ser formalizado e a continuidade canonica apos o fechamento da run, com owner concreto no nivel de `GameplaySessionFlow`.

## 2. Problema

Hoje a continuidade apos o fechamento ainda pode ser lida por contratos estreitos demais ou por seams transitarios demais.

Sem um owner concreto e um contexto canonico explicitos, o sistema tende a misturar:

- fechamento semantico da run
- contextualizacao da continuidade
- selecao/confirmacao da continuidade
- execucao downstream da continuidade
- navegacao pura de phase

Essa mistura reabre exatamente o tipo de confusao que ja impediu restart/reset previsiveis em tentativas anteriores de unificacao.

## 3. Decisao

Este ADR congela `RunContinuationContext` como contrato central de continuidade apos o fechamento da run.

### Owner concreto

- `RunContinuationOwnershipService` e o owner concreto do contexto de continuidade
- ele vive no boundary de `GameplaySessionFlow`
- ele materializa, guarda e expoe o contexto canonico

### Nascimento canonico

`RunContinuationContext` nasce logo apos a consolidacao do fechamento semantico da run, quando o owner concreto recebe o evento/registro terminal da run e materializa o contexto de continuidade.

`GameRunEndedEventBridge` nao e owner semantico. Ele sobrevive apenas como transporte fino do fato terminal ate o owner concreto.

### Payload minimo de `RunContinuationContext`

O contexto carrega apenas:

- contexto consolidado da run/sessao
- continuidades validas
- metadados minimos necessarios para expor e confirmar a selecao

Ele nao carrega semantica de execucao.
Ele nao carrega comando de restart.
Ele nao carrega navegacao editorial.
Ele nao carrega state de presenter.
Ele nao carrega bootstrap/start-plan.

Uma continuidade selecionada pode ser registrada como resolucao derivada, mas a selecao nao vira execucao e nao mora no contexto como comando.

## 4. Boundaries e ownership

### Ownership canonico

- `RunContinuationOwnershipService` e o owner canonico do contexto
- `GameplaySessionFlow` e o boundary macro de ownership da continuidade
- `RunResultStage` e `RunDecision` sao consumidores, nunca owners da semantica

### Boundaries proibidos

Nao e permitido colocar a semantica de continuidade em:

- `GameRunEndedEventBridge`
- `PostRunOverlayController`
- `PhaseNavigation`
- catalogo de phase
- executor downstream
- presenter local

### Separacao obrigatoria

O desenho canonico deve manter separados:

1. contexto de continuidade
2. selecao/confirmacao da continuidade
3. execucao downstream da continuidade
4. navegacao pura de phase

## 5. Fluxo canonico

O fluxo canonico passa a ser:

1. a run fecha semanticamente
2. `GameRunEndedEventBridge` transporta o fato terminal ao owner concreto
3. `RunContinuationOwnershipService` materializa `RunContinuationContext`
4. `RunResultStage`, quando existir, consome o contexto
5. `RunDecision`, quando existir ou quando necessario, consome o contexto e escolhe/confirma uma continuidade
6. a continuidade escolhida e resolvida como resultado de continuidade, nao como execucao
7. o executor downstream executa a continuidade resolvida
8. `AdvancePhase` entra em tratamento editorial explicito e so entao pode acionar navegacao pura
9. `RestartCurrentPhase` resolve para o rail proprio de phase reset
10. `PhaseNavigation` permanece fora do rail semantico de continuidade

Este fluxo continua valido quando `RunResultStage` e pulado.
O contexto existe mesmo no caminho `skip/no-content`.

## 5.1 Handoff macro para pipeline local

A continuidade macro nao executa `IntroStage` nem `RunResultStage`.
Ela resolve a continuidade e publica o handoff explicito para o pipeline local da phase.

Handoff canonico:

- `SessionTransitionOrchestrator` publica `SessionTransitionPhaseLocalEntryReadyEvent`
- `GameplayPhaseFlowService` consome o handoff como owner phase-side
- a phase local segue então pelo seu pipeline canonico ja existente
- a identidade local de reentrada da `IntroStage` e um `PhaseLocalEntrySequence` monotônico produzido no phase-side, para nao colidir com a entrada inicial nem com restarts subsequentes

Leitura pratica:

- `RestartCurrentPhase` entrega o reset ao rail local de phase
- `AdvancePhase` entrega a troca ao rail local de navigation/intro da phase
- `RunResultStage` permanece phase-local
- `RunDecision` permanece macro

### 5.2 Handoff da saida local para a continuidade macro

Quando `RunResultStage` conclui, o owner local publica um handoff estreito e tipado para `RunDecision`.

- `RunResultStageOwnershipService` emite `RunResultStageToRunDecisionHandoff`
- `RunDecisionOwnershipService` consome esse handoff e materializa a continuidade macro
- `RunResultStage` nao executa continuidade por conta propria
- `RunDecision` nao volta a ser stage local da phase

## 6. Responsabilidades por componente

### `GameplaySessionFlow`

- boundary macro de continuidade do gameplay
- abriga o owner concreto da continuidade
- garante que o contexto nasca logo apos o fechamento

### `RunContinuationOwnershipService`

- materializa `RunContinuationContext`
- mantem o contexto canonico de continuidade
- fornece o contexto aos consumidores corretos

### `RunEndIntent`

- continua sendo o ato de encerramento da run atual
- nao carrega o contrato completo de continuidade

### `RunResultStage`

- permanece um stage opcional e phase-owned
- consome o contexto
- nao decide a semantica de continuidade
- nao materializa contexto

### `RunDecision`

- consome `RunContinuationContext`
- escolhe ou confirma uma continuidade ja canonizada
- nao infere
- nao classifica
- nao monta execucao

### `GameRunEndedEventBridge`

- permanece apenas como transporte fino do evento terminal
- nao e owner semantico
- nao materializa o contexto canonico

### Execucao downstream

- executa apenas a continuidade resolvida
- nao define o contrato semantico
- nao substitui o contexto

### `GameplaySessionFlowContinuityService`

- executor downstream resolvido
- recebe a continuidade resolvida
- nao e hub de continuidade, navegacao, restart/reset e presenter

### `PhaseNavigation`

- continua sendo navegacao pura e editorial
- fica fora do rail semantico de continuidade
- so entra quando a continuidade escolhida for explicitamente `AdvancePhase`

### `PostRunOverlayController`

- presenter puro
- exibe e emite escolha/confirmacao downstream
- nao decide semantica

## 7. Continuidade minima de v1

As continuidades minimas de v1 sao:

- `AdvancePhase`
- `RestartCurrentPhase`
- `ExitToMenu`
- `EndRun`

Leitura canonica:

- `Victory`, `Defeat` e `ExitToMenu` continuam sendo motivos de fechamento, nao destinos automaticos
- o destino da continuidade e decidido depois do fechamento, por contrato de continuidade
- `EndRun` representa o encerramento terminal da continuidade, nao um atalho de `PhaseNavigation`
- `RestartCurrentPhase` pode existir como continuidade valida, mas sua execucao resolve para o rail proprio de phase reset
- `AdvancePhase` pode existir como continuidade valida, mas seu tratamento e editorial explicito e nao vira navegacao pura escondida no contexto amplo

## 8. Regras e invariantes

- `RunContinuationContext` nasce apos o fechamento semantico da run
- `RunContinuationOwnershipService` e o owner concreto do contexto
- `RunResultStage` e `RunDecision` consomem o contexto, nao o definem
- `RunDecision` escolhe ou confirma uma continuidade ja canonizada
- `RunDecision` nao define o conjunto canonico de continuidades validas
- o contexto deve suportar continuidades automaticas e continuidades com confirmacao
- `RunResultStage` continua opcional e sua ausencia nao invalida o contexto
- `PhaseNavigation` nao carrega semantica de fechamento de run
- `RestartCurrentPhase` nao e absorvido por um contexto amplo de continuidade
- `GameplaySessionFlowContinuityService` nao volta a misturar continuidade, navigation e reset/restart
- a decisao de continuidade nao mora em presenter local
- a decisao de continuidade nao mora em catalogo de phase

## 9. Consequencias

- o fim de run passa a ter um contrato explicito entre fechamento, selecao e execucao
- `RunDecision` deixa de depender de payload estreito para inferir significado
- continuidades automaticas e com escolha ficam no mesmo modelo sem colapsar semantica
- `RunResultStage` continua opcional sem enfraquecer o boundary
- `PhaseNavigation` permanece limpa e neutra
- restart/reset continuam fora do contexto amplo de continuidade

## 10. Nao objetivos

Este ADR nao define:

- implementacao concreta de presenter
- UI especifica de decisao
- mecanica de animacao ou overlay
- codigo de navegacao
- regras detalhadas de dispatch
- mapping de asset ou catalogo
- bootstrap/start-plan para phase restart

Este ADR tambem nao reabre o contrato de `IntroStage`.
Ele apenas fecha o boundary de continuidade apos o fechamento da run.

## 11. Relacao com ADRs existentes

- `ADR-0001`: vocabulario fundamental para resultado, intencao e contexto
- `ADR-0044`: separacao entre resultado, continuidade e navegacao
- `ADR-0045`: centro semantico do gameplay
- `ADR-0046`: `GameplaySessionFlow` como primeiro bloco do runtime
- `ADR-0047`: pipeline canonico da phase dentro de `GameplaySessionFlow`
- `ADR-0048`: rail canonico de routing gameplay-side
- `ADR-0049`: fim de run canonico com `RunEndIntent`, `RunResultStage` e `RunDecision`
- `ADR-0050`: `IntroStage` canonica e separacao da entrada da phase

Este ADR complementa `ADR-0049` sem substitui-lo.
Ele formaliza o que acontece depois do fechamento semantico da run e antes da execucao downstream da continuidade.

## 12. Proximos passos

1. Introduzir o contrato concreto de `RunContinuationOwnershipService` no dominio de `GameplaySessionFlow`.
2. Materializar `RunContinuationContext` logo apos o fechamento semantico da run.
3. Manter `GameRunEndedEventBridge` apenas como transporte fino.
4. Separar claramente contexto, selecao, execucao downstream e phase navigation.
5. Garantir que `RestartCurrentPhase` siga resolvendo para o rail proprio de phase reset.
