# ADR-0051 - Fase 0: congelamento de naming e contrato

## 1. Nomes finais aprovados

- Owner canonico: `RunContinuationOwnershipService`
- Contexto canonico: `RunContinuationContext`
- Enum minimo de continuidade: `RunContinuationKind`

## 2. Payload minimo de `RunContinuationContext`

- `closing reason`
- `allowed continuations`
- `requires player decision`
- `selected continuation` opcional

## 3. Distincao obrigatoria de boundary

- `allowed continuations`: conjunto canônico de continuidades permitidas para a run encerrada
- `selected continuation`: continuidade final ja resolvida, quando existir

Regra:

- o contexto carrega o conjunto permitido
- a selecao final e um estado derivado, nao o conjunto valido em si

## 4. Boundaries por responsabilidade

- Contexto semantico: `RunContinuationContext`
- Decisao: `RunDecision`
- Execucao downstream: executor resolvido separado
- Phase navigation: `PhaseNavigation`

## 5. Seam transitório

- `GameRunEndedEventBridge` permanece temporariamente como seam fino
- ele nao e owner semantico
- ele sobrevive apenas ate a materializacao canonica migrar para `RunContinuationOwnershipService`
- ele nao deve ganhar nova semantica de continuidade

## 6. Nomes e contratos a evitar

- `PostRunResult` como contrato central
- `PostRun`
- `Exit`
- `RunOutcome` como semantica de continuidade
- qualquer nome que misture fechamento, decisao e execucao downstream

## 7. Checklist de pronto da Fase 0

- nomes finais congelados
- payload minimo do contexto explicitado
- distincao entre `allowed continuations` e `selected continuation` congelada
- seam atual classificado como transitório e nao semantico
- boundaries por responsabilidade explicitados
- nomes proibidos registrados

## 8. Pronto para iniciar a Fase 1?

- Sim, desde que a Fase 1 use exatamente estes nomes e boundaries.
- A Fase 0 nao implementa comportamento, apenas congela linguagem, ownership e contratos.

