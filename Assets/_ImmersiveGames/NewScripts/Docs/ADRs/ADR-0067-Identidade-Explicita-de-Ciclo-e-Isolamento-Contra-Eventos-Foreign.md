# ADR-0067 - Identidade explícita de ciclo e isolamento contra foreign/stale events

## Status
- Estado: Accepted
- Data: 2026-05-01
- Tipo: Direction / Canonical architecture
- Fonte de verdade canônica deste contrato: este ADR.

## Contexto

A Base 1.0 ainda permitiu, em vários pontos, que eventos antigos, tardios ou de outra identidade competissem com o pipeline ativo por causa de vocabulário compartilhado e fronteiras demasiado difusas.
Base 1.1 fecha essa brecha.

## Decisão

Todo ciclo relevante deve ter identidade explícita.

Regras:

- run, session, activation e deactivation precisam de identidade válida.
- foreign/stale events não podem alterar o pipeline ativo.
- identidade válida é requisito de leitura, não detalhe opcional.
- qualquer `Pipeline Handoff` deve carregar o contexto mínimo para validar a origem.

## Consequências

- o pipeline ativo passa a ser protegido contra ruído temporal.
- replays, atrasos e ecos de ciclo antigo deixam de competir com a verdade atual.
- a depuração fica mais previsível porque cada `Pipeline Fact` pode ser comparado com a identidade correta.
- o contrato deixa de depender de heurísticas de tempo ou ordem por conveniência.

## Invariantes

- identidade explícita antes de decisão.
- foreign/stale event é inerte para o pipeline ativo.
- nenhuma etapa regride para um ciclo antigo sem revalidação canônica.
- identidade não é inferida por nome de classe, cena ou timing.

## Relação com Base 1.0 e Base 2.0

- Base 1.0 foi a base de derivação dos contratos de identidade e signature.
- Base 1.1 exige identidade explícita em todo ciclo relevante.
- Base 2.0 futura só deve reutilizar esse princípio se ele continuar válido na prática.

## ADRs históricos relacionados

- `ADR-0013`
- `ADR-0049`
- `ADR-0051`
- `ADR-0052`
- `ADR-0055`
- `ADR-0057`
- `ADR-0059`
