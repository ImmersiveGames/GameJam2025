# ADR-0056 - Baseline 4.0 como executor tecnico fino e fronteira com `GameplaySessionFlow`

## Status
- Estado: Aceito
- Data: 2026-04-17
- Tipo: Direction / Canonical architecture
- Fonte de verdade canonica deste contrato: este ADR.
- Nota de fechamento: implementacao principal concluida e validada por smoke/runtime no ciclo incremental F1-F7.

## 1. Contexto

`ADR-0044` congela o Baseline 4.0 ideal como canon guarda-chuva.
`ADR-0045` congela `Gameplay Runtime Composition` como centro semantico do gameplay.
`ADR-0046` e `ADR-0047` colocam `GameplaySessionFlow` acima do backbone como bloco semantico da sessao.
`ADR-0052` congela a camada acima do baseline para `Session Transition`.
`ADR-0055` congela o seam de integracao semantica de sessao acima do baseline.

A auditoria ampla do baseline mostrou um quadro estabilizado:

- o baseline e majoritariamente saudavel como executor tecnico
- o problema real e de shape e integracao, nao de conceito
- alguns pontos ainda hospedam costura semantica por oportunidade

Este ADR congela o papel do baseline para evitar que futuras refatoracoes reintroduzam semantica de gameplay no lugar errado.

## 2. Problema

O baseline ainda sofre com tres desvios recorrentes:

- bootstrap bloat
- costura semantica por oportunidade
- bucket files misturando contracts, runtime, compat e adapters

Isso aparece quando `SceneFlowBootstrap`, `NavigationBootstrap`, `GameLoopBootstrap` e `GameplaySessionContextService.cs` passam a absorver responsabilidade acima do baseline.

Sem um congelamento explicito, o baseline tende a virar o lugar conveniente para:

- ownership semantico da sessao
- selecao semanticamente relevante de phase
- continuidade e reset de sessao como politica semantica
- participation ownership
- emissao de intencao sem emissor canonico claro

## 3. Decisao

O Baseline 4.0 fica formalmente definido como **executor tecnico fino** do runtime.

Ele continua responsavel por fornecer a infraestrutura macro e operacional que serve os blocos acima dele, mas nao por carregar o significado principal do gameplay.

Em particular:

- `GameplaySessionFlow` continua acima do baseline e e o owner semantico da sessao
- `ADR-0055` continua sendo o seam canonico de integracao semantica de sessao
- `ADR-0052` continua sendo a camada acima do baseline para transformacao composta da sessao/runtime

O baseline serve essas camadas, mas nao absorve ownership delas.

## 4. Boundaries

### Fica no baseline

- boot e dependency root
- `SceneFlow` macro
- loading e fade
- gates e readiness tecnicos
- `WorldReset`, `SceneReset` e `ResetInterop`
- dispatch macro de rota
- `InputModes` como request/apply operacional
- materializacao e reset operacional

### Sai do baseline

- ownership semantico da sessao
- preparacao semantica de gameplay
- participation ownership
- continuidade e reset de sessao como politica semantica
- bridges semanticas de sessao
- selecao semantica de phase
- decisao de intencao acima do request/apply operacional
- presenter local de `IntroStage`

### Limite pratico

O baseline para antes de `GameplaySessionFlow`, `ADR-0055` e `ADR-0052`.
Ele nao pode servir de centro semantico do gameplay, nem de casa provisoria para costura permanente.

## 5. Consequencias

Este congelamento implica o seguinte:

- `SceneFlowBootstrap` deve encolher e voltar a wiring tecnico fino
- `NavigationBootstrap` deve voltar a macro dispatch puro
- `GameLoopBootstrap` nao deve permanecer como host pesado de sincronizacao transversal
- `GameplaySessionContextService.cs` deve ser fatiado
- `LevelLifecycle` residual nao deve continuar servindo como nome de ownership que ja nao pertence ali
- o baseline deve ficar mais fino antes de evoluir spawn, reset, `ActorRegistry` e binders

Tambem fica registrado que `InputModes` continua sendo aplicacao operacional, nao arbitro de ownership.
A existencia de dedupe no trilho de requests nao resolve ambiguidade de emissor canonico.

## 6. Ordem de refatoracao

Ordem ampla recomendada:

1. congelar o emissor canonicamente unico de intencao para seams adjacentes
2. tirar de `SceneFlowBootstrap` o gate de preparacao de gameplay e a bridge de participacao
3. esvaziar `NavigationBootstrap` de continuidade e reset de sessao
4. separar `GameplaySessionContextService.cs` em contratos, runtime, compat e adapters
5. reespecificar `GameLoopBootstrap` e `GameLoopInstaller` apenas depois que a fronteira acima do baseline estiver estavel
6. seguir entao para spawn, reset, `ActorRegistry` e binders

## 7. Relacao documental

Este ADR nao substitui os ADRs anteriores.
Ele operacionaliza o canon ja congelado por:

- `ADR-0044` como guarda-chuva do baseline ideal
- `ADR-0045` como centro semantico do gameplay
- `ADR-0047` como pipeline canonico de montagem da phase
- `ADR-0052` como composicao acima do baseline
- `ADR-0055` como seam explicito de integracao semantica de sessao

## 8. Fechamento

O baseline 4.0 passa a ser lido como executor tecnico fino, e nao como centro semantico do gameplay.
Toda costura acima dessa fronteira deve morar em `GameplaySessionFlow`, `Session Transition` ou `Session Integration`.

Validacao registrada: o baseline foi enxugado e estabilizado no papel tecnico definido por este ADR.
