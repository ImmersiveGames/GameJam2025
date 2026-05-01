# ADR-0067 - Identidade explicita de ciclo e isolamento contra eventos foreign

## Status
- Estado: Accepted
- Data: 2026-05-01
- Tipo: Direction / Canonical architecture
- Fonte de verdade canonica deste contrato: este ADR.

## Contexto

A Base 1.0 ainda permitiu, em varios pontos, que eventos antigos, tardios ou de outra identidade competissem com o pipeline ativo por causa de vocabulario compartilhado e fronteiras demasiado difusas.
Base 1.1 fecha essa brecha.

## Decisao

Todo ciclo relevante deve ter identidade explicita.

Regras:

- run, session, activation e deactivation precisam de identidade valida.
- eventos foreign ou stale nao podem alterar o pipeline ativo.
- identidade valida e requisito de leitura, nao detalhe opcional.
- qualquer handoff deve carregar o contexto minimo para validar a origem.

## Consequencias

- o pipeline ativo passa a ser protegido contra ruido temporal.
- replays, atrasos e ecos de ciclo antigo deixam de competir com a verdade atual.
- a depuracao fica mais previsivel porque cada fato pode ser comparado com a identidade correta.
- o contrato deixa de depender de heuristicas de tempo ou ordem por conveniencia.

## Invariantes

- identidade explicita antes de decisao.
- foreign/stale event e inerte para o pipeline ativo.
- nenhuma etapa regride para um ciclo antigo sem revalidacao canonica.
- identidade nao e inferida por nome de classe, cena ou timing.

## Relacao com Base 1.0 e Base 2.0

- Base 1.0 foi a base de derivacao dos contratos de identidade e signature.
- Base 1.1 exige identidade explicita em todo ciclo relevante.
- Base 2.0 futura so deve reutilizar esse principio se ele continuar valido na pratica.

## ADRs historicos relacionados

- `ADR-0013`
- `ADR-0049`
- `ADR-0051`
- `ADR-0052`
- `ADR-0055`
- `ADR-0057`
- `ADR-0059`
