# ADR-0051 - Fase 0: congelamento de naming e boundary

## 1. Objetivo

Congelar a linguagem canonica e o boundary estrutural antes de qualquer implementacao.

O desenho desta frente exige evitar a repeticao do problema historico de restart/reset: contexto amplo demais, bridge gordo demais e execucao misturada com selecao.

## 2. Nomes finais aprovados

- Owner canonico: `RunContinuationOwnershipService`
- Contexto canonico: `RunContinuationContext`
- Enum minimo de continuidade: `RunContinuationKind`

## 3. Payload minimo de `RunContinuationContext`

O contexto deve carregar apenas:

- contexto consolidado da run/sessao
- continuidades validas
- metadados minimos para exposicao e confirmacao

Nao deve carregar:

- comando de execucao
- navigator/editorial routing
- state de presenter
- bootstrap/start-plan
- payload de reset/restart

## 4. Distincao obrigatoria de boundary

- contexto semantico: `RunContinuationContext`
- selecao/confirmacao: `RunDecision`
- execucao downstream: executor resolvido separado
- phase navigation: `PhaseNavigation`

Regra:

- o contexto carrega o conjunto valido
- a selecao final e uma resolucao derivada, nao o contrato de execucao

## 5. Seam transitario

- `GameRunEndedEventBridge` permanece apenas como transporte fino
- ele nao e owner semantico
- ele nao materializa o contexto canonico
- ele nao decide continuidade

## 6. Nomes e contratos a evitar

- `PostRunResult` como contrato central
- `PostRun`
- `Exit`
- `RunOutcome` como semantica de continuidade
- qualquer nome que misture fechamento, selecao e execucao downstream

## 7. Checklist de pronto da Fase 0

- nomes centrais congelados
- owner concreto definido
- payload minimo explicitado
- selecao separada de execucao
- bridge classificado como transporte fino
- `RestartCurrentPhase` protegido como continuidade valida que resolve para rail proprio de phase reset
- `PhaseNavigation` fora do rail semantico de continuidade

## 8. Pronto para iniciar a Fase 1?

- Sim, desde que a Fase 1 use exatamente estes nomes, boundaries e separacoes.
- A Fase 0 nao implementa comportamento, apenas congela linguagem, ownership e contratos.
