# Plan - Baseline 4.0 Slice 8 - Save Checkpoint Canonical

Subordinado a `ADR-0043`, `ADR-0044`, `ADR-0041`, `ADR-0037` e aos planos canônicos:
- [Blueprint-Baseline-4.0-Ideal-Architecture.md](./Blueprint-Baseline-4.0-Ideal-Architecture.md)
- [Plan-Baseline-4.0-Execution-Guardrails.md](./Plan-Baseline-4.0-Execution-Guardrails.md)
- [Plan-Baseline-4.0-Reorganization.md](./Plan-Baseline-4.0-Reorganization.md)

## Canonical Target

`Save` continua como camada canônica de orquestração de persistência.

`Checkpoint` é um domínio próprio dentro de `Save`, mas não é `Preferences` e não substitui `Progression`.

O contrato canônico desta fase congela o rail de `Checkpoint` como contrato subordinado ao blueprint, suportado apenas por seams oficiais e contextuais.

O objetivo é documental: definir o contrato canônico de `Checkpoint` sem criar backend final, sem checkpoint operacional e sem sugerir backbone runtime paralelo.

Identidade canônica quando o contrato exigir checkpoint:
- `checkpointId`
- `profileId`
- `slotId`

Ownership canônico:
- `Save` orquestra.
- `Checkpoint` não é owner de `GameLoop`, `SceneFlow`, `Navigation`, `WorldReset` ou `Frontend/UI`.
- Backend concreto permanece detalhe de infraestrutura.

## Inventory Decision Matrix

| Item | Decision | Why |
|---|---|---|
| `Save` | Keep | continua expressando a orquestracao canonica de persistencia |
| `Checkpoint` | Keep with reshape | e dominio proprio do `Save`, mas precisa permanecer explicitamente subordinado ao blueprint |
| `CheckpointSnapshot` | Keep with reshape | representa o payload conceitual do checkpoint sem virar estado operacional final |
| `ICheckpointService` | Keep with reshape | contrato de aplicacao necessario, desde que nao assuma ownership de fluxo de jogo |
| `ICheckpointBackend` | Keep with reshape | contrato de infraestrutura necessario, mas nao source-of-truth do rail |
| `checkpointId` | Keep | identidade canonica obrigatoria quando checkpoint exigir persistencia |
| `profileId` | Keep | identidade canônica compartilhada com o rail de `Save` |
| `slotId` | Keep | identidade canônica compartilhada com o rail de `Save` |
| `Preferences` | Keep | dominio canonico separado, explicitamente fora do rail de `Checkpoint` |
| `Progression` | Keep | dominio canonico separado, explicitamente fora do rail de `Checkpoint` |
| `IProgressionBackend` | Keep | infraestrutura canonica do `Progression`, nao do `Checkpoint` |
| `InMemoryProgressionBackend` | Keep with reshape | backend provisório de `Progression`, mantido fora do rail de `Checkpoint` |
| `GameRunEndedEvent` | Keep | seam oficial upstream que pode suportar checkpoint contextual sem gatilho cego |
| `WorldResetCompletedEvent` | Keep | seam oficial upstream que pode suportar checkpoint contextual sem gatilho cego |
| `SceneTransitionCompletedEvent` | Keep | seam oficial upstream que pode suportar checkpoint contextual sem gatilho cego |
| `GameLoop` | Forbid adapter | checkpoint nao pode deslocar ownership para a camada de fluxo |
| `SceneFlow` | Forbid adapter | checkpoint nao pode criar bridge permanente para mascarar fronteira errada |
| `Navigation` | Forbid adapter | checkpoint nao pode virar rotulo alternativo de dispatch primario |
| `WorldReset` | Forbid adapter | checkpoint nao pode assumir semanticamente o owner do reset |
| `Frontend/UI` | Forbid adapter | checkpoint nao pode ser empurrado para ownership visual |
| `Blueprint-Baseline-4.0-Ideal-Architecture.md` | Keep | e a fonte de verdade do alvo arquitetural |
| `Plan-Baseline-4.0-Execution-Guardrails.md` | Keep | define o template e as regras de aceite desta fase |
| `Plan-Baseline-4.0-Reorganization.md` | Keep | fornece a direcao de reorganizacao e vocabulario de backlog |

## Canonical Runtime Rail

`GameRunEndedEvent`, `WorldResetCompletedEvent` e `SceneTransitionCompletedEvent` continuam sendo os seams oficiais e contextuais preferidos para qualquer checkpoint futuro.

O rail canonico desta fase e:

`official contextual hooks -> Save canonical orchestration -> Checkpoint contract -> infrastructure backend detail`

Regras do rail:
- hooks oficiais sao seams preferidos, nao gatilhos cegos
- `Checkpoint` nao substitui `Preferences`
- `Checkpoint` nao substitui `Progression`
- `Checkpoint` nao cria polling path
- `Checkpoint` nao usa varredura generica do mundo
- `Checkpoint` nao depende de fallback silencioso
- `Checkpoint` nao cria ownership visual
- backend concreto nao e source-of-truth do contrato

## Parallel Rails to Eliminate

- checkpoint por polling de runtime
- checkpoint por varredura generica do mundo
- checkpoint como substituto de `Preferences`
- checkpoint como substituto de `Progression`
- checkpoint com backend concreto tratado como contrato principal
- checkpoint com fallback silencioso para config ausente
- checkpoint com adapter permanente para manter fronteira errada
- checkpoint com ownership em `GameLoop`, `SceneFlow`, `Navigation`, `WorldReset` ou `Frontend/UI`

## Phase Scope

Escopo desta fase:
- `NewScripts` only
- `Docs/Plans` only
- sem tocar em `Scripts` legado
- sem alterar runtime
- sem criar ADR novo
- sem expandir para backend final
- sem reabrir o Slice 7
- sem checkpoint operacional
- sem save state completo

Foco da fase:
- congelar `Checkpoint` como contrato canônico subordinado a `Save`
- manter `Preferences` e `Progression` fora do rail de checkpoint
- registrar os seams oficiais que suportariam checkpoint contextual
- documentar a fronteira correta de ownership e infraestrutura

## Explicit Prohibitions

- mover ownership para camada visual
- usar adapter ou bridge para esconder fronteira errada
- adicionar fallback silencioso para mascarar contrato fraco
- adicionar observabilidade em polling path sem necessidade
- corrigir sintoma local sem declarar owner canônico
- transformar evento observável em API pública por conveniência
- criar backend final de checkpoint nesta fase
- misturar `Checkpoint` com `Preferences`
- misturar `Checkpoint` com `Progression`
- tratar `GameLoop`, `SceneFlow`, `Navigation`, `WorldReset` ou `Frontend/UI` como owners semanticos do slice

## Acceptance Gates

O Slice 8 so e aceito se:
- o owner canônico estiver declarado
- o desvio semantico estiver explicitado
- o reaproveitamento valido estiver separado do que sera substituido
- a matriz de decisao estiver completa
- as proibicoes tiverem sido cumpridas
- a evidencia mostrar que o contrato canonico nao foi diluido
- `Checkpoint` estiver descrito como dominio proprio dentro de `Save`
- `Checkpoint` nao for confundido com `Preferences` ou `Progression`
- `profileId`, `slotId` e `checkpointId` forem obrigatorios onde o contrato exigir
- `Save` continuar como camada canonica de orquestracao
- os hooks oficiais permanecerem os seams preferidos e contextuais
- nenhum fallback silencioso novo for introduzido
- nenhum polling de runtime for usado para sustentar o contrato
- nenhum adapter permanente for introduzido para preservar fronteira errada
- `Checkpoint` nao ganhar ownership de `GameLoop`, `SceneFlow`, `Navigation`, `WorldReset` ou `Frontend/UI`
- o Slice 7 permanecer fechado sem reabertura

## Evidence Required

- docs afetados:
  - [Blueprint-Baseline-4.0-Ideal-Architecture.md](./Blueprint-Baseline-4.0-Ideal-Architecture.md)
  - [Plan-Baseline-4.0-Execution-Guardrails.md](./Plan-Baseline-4.0-Execution-Guardrails.md)
  - [Plan-Baseline-4.0-Reorganization.md](./Plan-Baseline-4.0-Reorganization.md)
  - [Plan-Baseline-4.0-Slice-8-Save-Checkpoint-Canonical.md](./Plan-Baseline-4.0-Slice-8-Save-Checkpoint-Canonical.md)
- inventario de itens avaliados:
  - `Save`
  - `Checkpoint`
  - `CheckpointSnapshot`
  - `ICheckpointService`
  - `ICheckpointBackend`
  - `checkpointId`
  - `profileId`
  - `slotId`
  - `Preferences`
  - `Progression`
  - `IProgressionBackend`
  - `InMemoryProgressionBackend`
  - `GameRunEndedEvent`
  - `WorldResetCompletedEvent`
  - `SceneTransitionCompletedEvent`
  - `GameLoop`
  - `SceneFlow`
  - `Navigation`
  - `WorldReset`
  - `Frontend/UI`
  - `Blueprint-Baseline-4.0-Ideal-Architecture.md`
  - `Plan-Baseline-4.0-Execution-Guardrails.md`
  - `Plan-Baseline-4.0-Reorganization.md`
- decisão por item: registrada na `Inventory Decision Matrix`
- conflitos encontrados:
  - `Checkpoint` precisava ficar subordinado a `Save` sem virar backbone paralelo
  - `Preferences` e `Progression` precisavam permanecer fora do rail de checkpoint
  - os hooks oficiais precisavam continuar contextuais e sem gatilho cego
- validação realizada:
  - revisão documental contra blueprint, guardrails e reorganization
  - normalização da ordem obrigatória de seções
  - confirmação de que não houve alteração de runtime nesta fase
