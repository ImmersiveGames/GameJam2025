# ADR-0062 - Session Pipeline canônico e substituição do conceito de local

## Status
- Estado: Accepted
- Data: 2026-05-01
- Tipo: Direction / Canonical architecture
- Fonte de verdade canônica deste contrato: este ADR.

## Contexto

O conceito histórico de `local` misturou decisão semântica da sessão, ativação de conteúdo e comportamento scene-local.
Na Base 1.1, a sessão precisa de um rail próprio e determinístico.

## Decisão

Adota-se o `Session Pipeline` como rail canônico de sessão.

Regras:

- `Session Pipeline` substitui o conceito histórico de `local`.
- o pipeline concentra lifecycle de sessão, `Pipeline Handoffs` locais e regras de ativação da sessão.
- `IntroStage` e `RunResult` não decidem a sessão; eles executam partes da `Pipeline Policy`.
- o host local resolve a instância concreta apenas no momento canônico da pipeline.

## Consequências

- `local` passa a ser vocabulário histórico.
- decisão de sessão deixa de ser espalhada em code paths scene-local sem identidade.
- a ativação local fica vinculada a contrato e identidade da sessão atual.
- a Base 1.1 separa claramente pipeline de sessão e implementação concreta.

## Invariantes

- toda sessão relevante possui identidade explícita.
- eventos de outra sessão não podem alterar o `Session Pipeline` ativo.
- a resolução local concreta só ocorre no momento canônico do pipeline.
- ausência válida de conteúdo gera `skip/no-content` explícito, não fallback silencioso.

## Relação com Base 1.0 e Base 2.0

- Base 1.0 deixou a topologia de `GameplaySessionFlow`, `Session Integration` e `Session Transition` como prova de materialização.
- Base 1.1 recolhe essa topologia em `Session Pipeline`.
- Base 2.0 futura só pode reorganizar o que o `Session Pipeline` provar.

## ADRs históricos relacionados

- `ADR-0045`
- `ADR-0046`
- `ADR-0047`
- `ADR-0052`
- `ADR-0055`
- `ADR-0057`
- `ADR-0050`

## Materializacao Base11Sandbox - checkpoint congelado

No checkpoint `Base11Sandbox Minimal Route + Session Activity Cycle - PASS`, a decisao deste ADR foi materializada assim:

- `RuntimeModeConfig.startupRouteDefinition` aponta direto para a rota inicial;
- o caminho `Boot -> Menu -> Sandbox` segue por `SessionOperationalPipeline`;
- `SessionActivityPipeline` aceita o `Pipeline Handoff` e resolve a primeira activity pelo proprio catalogo;
- `DebugDirectStart` continua como QA/tooling e rejeita apos o start canonico;
- `Pause` e `Resume` permanecem no ciclo de activity sem reintroduzir ownership legada.

Consolidacao do checkpoint:

- o host local nao decide a sessao antes do momento canonico;
- a ausencia de presenter ou activity valida continua sendo `skip/no-content` ou `observed_noop`, nao fallback silencioso;
- a sessao relevante continua protegida por identidade explicita.
