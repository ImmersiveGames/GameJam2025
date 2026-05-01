# ADR-0062 - Session Pipeline canonico e substituicao do conceito de local

## Status
- Estado: Accepted
- Data: 2026-05-01
- Tipo: Direction / Canonical architecture
- Fonte de verdade canonica deste contrato: este ADR.

## Contexto

O conceito historico de `local` misturou decisao semantica da sessao, ativacao de conteudo e comportamento scene-local.
Na Base 1.1, a sessao precisa de um rail proprio e deterministico.

## Decisao

Adota-se o `Session Pipeline` como rail canonico de sessao.

Regras:

- `Session Pipeline` substitui o conceito historico de `local`.
- o pipeline concentra lifecycle de sessao, handoffs locais e regras de ativacao da sessao.
- `IntroStage` e `RunResult` nao decidem a sessao; eles executam partes da politica do pipeline.
- o host local resolve a instancia concreta apenas no momento canonico da pipeline.

## Consequencias

- `local` passa a ser vocabulario historico.
- decisao de sessao deixa de ser espalhada em code paths scene-local sem identidade.
- a ativacao local fica vinculada a contrato e identidade da sessao atual.
- a Base 1.1 separa claramente pipeline de sessao e implementacao concreta.

## Invariantes

- toda sessao relevante possui identidade explicita.
- eventos de outra sessao nao podem alterar o session pipeline ativo.
- a resolucao local concreta so ocorre no momento canonico do pipeline.
- ausencia valida de conteudo gera skip/no-content explicito, nao fallback silencioso.

## Relacao com Base 1.0 e Base 2.0

- Base 1.0 deixou a topologia de `GameplaySessionFlow`, `Session Integration` e `Session Transition` como prova de materializacao.
- Base 1.1 recolhe essa topologia em `Session Pipeline`.
- Base 2.0 futura so pode reorganizar o que o session pipeline provar.

## ADRs historicos relacionados

- `ADR-0045`
- `ADR-0046`
- `ADR-0047`
- `ADR-0052`
- `ADR-0055`
- `ADR-0057`
- `ADR-0050`
