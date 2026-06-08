<!--
STATUS: HISTÓRICO PARA CONSULTA.
Este ADR foi reclassificado pelo ADR-2.0-0001 — Capability Discovery e Activity Capability Inventory.
Use como evidência, histórico e intenção funcional. Em conflito, ADR-2.0-0001 prevalece.
-->

﻿# ADR-0001 - Base 1.1: Pipeline Convergence, Identidade Explícita e Isolamento contra Foreign Events

## Status
- Estado: Accepted / Foundation
- Data: 2026-05-12
- Tipo: Direction / Canonical architecture
- Fonte de verdade canônica deste contrato: este ADR.

## Contexto

A Base 1.0 estabilizou vários fluxos concretos, mas deixou a arquitetura com linguagem mista entre semântica, rails operacionais e owners por conveniência. O conceito de ciclo também não possuía identidade explícita, permitindo que eventos foreign/stale competissem com o pipeline ativo por causa de vocabulário compartilhado.

Base 1.1 converge os fluxos já materializados em pipelines com identidade explícita, sem reabrir a discussão de uma Base 2.0.

## Decisão

Adota-se a **Base 1.1 como Pipeline Convergence** com identidade explícita e isolamento contra foreign/stale events.

### Princípios Centrais

1. **Convergência para Pipelines Determinísticos**
   - `Run Pipeline` substitui o conceito histórico de `macro`.
   - `Session Pipeline` substitui o conceito histórico de `local`.
   - Módulos produzem `Pipeline Facts` ou `Pipeline Commands`.
   - Pipelines decidem ordem, lifecycle, `Pipeline Policies` e `Pipeline Handoffs`.
   - `Pipeline Adapters` executam side-effects comandados pelo pipeline.

2. **Identidade Explícita em Todo Ciclo Relevante**
   - Run, session, activation/phases e deactivation precisam de identidade válida.
   - Foreign/stale events não podem alterar o pipeline ativo.
   - Identidade válida é requisito de leitura, não detalhe opcional.
   - Qualquer `Pipeline Handoff` deve carregar o contexto mínimo para validar a origem.

3. **Isolamento contra Foreign/Stale Events**
   - O pipeline ativo passa a ser protegido contra ruído temporal.
   - Replays, atrasos e ecos de ciclo antigo deixam de competir com a verdade atual.
   - Nenhuma etapa regride para um ciclo antigo sem revalidação canônica.
   - Identidade não é inferida por nome de classe, cena ou timing.

## Invariantes Obrigatórios

- Base 1.1 não é Base 2.0.
- Base 1.1 não é transição descartável.
- Identidade explícita é obrigatória em todo ciclo relevante.
- Foreign/stale events não alteram o pipeline ativo.
- Pipeline ativo é decidido por identidade e contrato, não por ambiguidade temporal.
- Nenhum `Pipeline Adapter` cria política própria.
- Nenhum módulo operacional reescreve a identidade do ciclo.
- Nenhum pipeline depende de leitura foreign/stale para decidir o ativo.

## Consequências

- A pasta principal deixa de carregar normativa da Base 1.0.
- Ownership deixa de ser inferido por quem executa o código.
- A Responsabilidade de decisão fica concentrada no pipeline.
- O lado executor deixa de ser confundido com o lado decisor.
- A depuração fica mais previsível porque cada `Pipeline Fact` pode ser comparado com a identidade correta.

## Relação com Base 1.0 e Base 2.0

- Base 1.0 é antecedente histórico e prova de materialização dos fluxos.
- Base 1.1 dá shape final de pipeline aos fluxos concretos já materializados na Base 1.0.
- Base 2.0 futura só pode extrair, generalizar ou reorganizar o que a Base 1.1 provar.

## Scope dos ADRs Base 1.1

Este ADR e os seguintes definem a Base 1.1:

- **ADR-0001** (este): Base 1.1 Pipeline Convergence e Identidade Explícita
- **ADR-0002**: Run Pipeline Canonical
- **ADR-0003**: Session Operational Pipeline e Transition Envelope
- **ADR-0004**: Session Activity Pipeline
- **ADR-0005**: Modules Produzem Facts/Commands, Adapters Executam Side-Effects
- **ADR-0006**: Route, Scene Composition, Fade, Loading e Audio Adapters
- **ADR-0007**: Gates, InputModes e Simulation Executors
- **ADR-0008**: SaveSystem Canonical

ADRs anteriores viram histórico de referência em `Docs/ADRs/Historico/`.

## Materializacao Base11Sandbox - Checkpoint Congelado

Evidencia de runtime consolidada no checkpoint `Base11Sandbox Minimal Route + Session Activity Cycle - PASS`:

- Rota operacional mínima por asset direto
- `SessionOperationalRouteAsset` como contrato de rota, não como policy
- `SessionOperationalPipeline` como owner de comando, completude e handoff
- `Base11SandboxOperationalRouteTransitionAdapter` como adapter executor
- `SceneCompositionExecutor` como executor físico
- `SessionActivityPipeline` como owner do ciclo interno de activity
- `routeIdentity` obrigatória e serve como identidade/log/guard
- `SessionActivityEntryHandoff` não reutiliza identidade operacional como identidade de activity
- Foreign/stale events continuam inertes; o pipeline ativo só aceita eventos compatíveis com a identidade corrente

Este checkpoint confirma, na prática, a decisão central deste ADR:

- Pipelines decidem
- Adapters executam side-effects
- Identity explícita protege o ciclo
- Foreign/stale events permanecem inertes

## Checkpoint congelado - SessionActivity deterministico e pending command lifecycle (2026-05-19)

Contrato Base 1.1 congelado:

- `SessionActivityPipeline` deve permanecer deterministico em todos os rails de lifecycle.
- Comando sincronico nao pode representar conclusao de rail quando houver `pending async` em andamento.
- Todo rail segue o shape:
  - `request`
  - `started/in-progress`
  - `completed/failed`
- `PendingOperation` e `Pipeline Command` em execucao; nao e estado solto.
- `ClearPendingOperation` so pode ocorrer no consumo validado de completion do command pendente.
- `SessionOperationalPipeline` nao pode descarregar route scene enquanto `SessionActivityPipeline` tiver rail/pending ativo.
- Trilhos legacy/paralelos devem ser removidos no caminho de implementacao; nao manter compatibilidade narrativa paralela.

