# ADR-0063 - Modules produzem fatos ou comandos e adapters executam side-effects

## Status
- Estado: Accepted
- Data: 2026-05-01
- Tipo: Direction / Canonical architecture
- Fonte de verdade canônica deste contrato: este ADR.

## Contexto

A Base 1.0 separou semântica, seam e execução de forma útil, mas ainda permitiu leituras ambíguas sobre quem decide e quem apenas executa.
Na Base 1.1 essa separação precisa virar regra de sistema.

## Decisão

Adota-se a regra:

- modules produzem `Pipeline Facts` ou `Pipeline Commands`;
- pipelines decidem ordem, lifecycle, `Pipeline Policies` e `Pipeline Handoffs`;
- `Pipeline Adapters` executam side-effects.

Regra complementar:

- nenhum `Pipeline Adapter` cria política própria;
- nenhum módulo operacional reescreve a identidade do ciclo;
- nenhum pipeline depende de leitura foreign/stale para decidir o ativo.

## Consequências

- A responsabilidade de decisão fica concentrada no pipeline.
- O lado executor deixa de ser confundido com o lado decisor.
- Integração externa fica por adaptação, não por ownership oculto.
- Falhas de side-effect não podem ser mascaradas como decisão de lifecycle.

## Invariantes

- `Pipeline Command` não é efeito.
- `Pipeline Fact` não é decisão.
- `Pipeline Adapter` não é owner de semântica.
- side-effect executa o que foi comandado, não inventa o que deve ser feito.

## Relação com Base 1.0 e Base 2.0

- Base 1.0 estabeleceu os blocos e rails que permitiram enxergar a separação.
- Base 1.1 transforma essa separação em contrato normativo central.
- Base 2.0 futura só deve reutilizar a regra se ela continuar provada por evidências de runtime.

## ADRs históricos relacionados

- `ADR-0055`
- `ADR-0056`
- `ADR-0057`
- `ADR-0058`
- `ADR-0059`
- `ADR-0040`
- `ADR-0038`

## Materializacao Base11Sandbox - checkpoint congelado

O checkpoint validado confirmou a regra deste ADR sem ambiguidade:

- `SessionOperationalPipeline` emite `OperationalRouteCommand` e `OperationalRouteCompleted`;
- `Base11SandboxOperationalRouteTransitionAdapter` so executa `load/unload/set-active`;
- `SessionActivityEntryHandoff` e produzido pelo pipeline, nao pelo adapter;
- `SceneCompositionExecutor` permanece estritamente executor fisico;
- `SessionActivityPipeline` decide a entrada de activity e o controle interno do ciclo.

Leitura congelada:

- comando nao e efeito;
- fato nao e decisao;
- adapter nao e owner de semantica;
- policy continua concentrada no pipeline.
