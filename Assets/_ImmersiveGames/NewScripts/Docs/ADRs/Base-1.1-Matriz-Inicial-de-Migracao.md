# Base 1.1 — Matriz Inicial de Migração Corrigida

## Fonte normativa

Este documento usa apenas os ADRs vivos da **Base 1.1**, `ADR-0060` a `ADR-0067`.

ADRs anteriores são histórico de referência e não devem ser usados como fonte normativa de ownership, exceto quando explicitamente citados pelos ADRs Base 1.1.

## Objetivo

Conduzir a migração conceitual e prática da **Base 1.0** para a **Base 1.1 — Pipeline Convergence / Convergência para Pipelines Determinísticos**.

A Base 1.1 não é Base 2.0, não é transição descartável e não propõe reorganização física em `Core/Concrete/Adapter`. O objetivo é dar shape final de pipeline aos fluxos concretos já materializados na Base 1.0.

## Vocabulário canônico

- `Run Pipeline`
- `Session Pipeline`
- `Pipeline Stage`
- `Pipeline Fact`
- `Pipeline Command`
- `Pipeline Handoff`
- `Pipeline Policy`
- `Pipeline Adapter`
- `Session Activity`
- `Pipeline Identity`
- `Pipeline Snapshot`
- `foreign/stale events`

## Princípios aplicados

- Módulos produzem `Pipeline Facts` ou `Pipeline Commands`.
- Pipelines decidem ordem, lifecycle, policies e handoffs.
- `Pipeline Adapters` executam side-effects comandados por pipelines.
- Gates, InputModes e GameLoop executam estado/efeitos; não decidem lifecycle.
- IntroStage é `Activation Stage / Pipeline Policy`, não owner de ativação.
- RunResult, RunDecision e PostRun pertencem a `Deactivation / Continuity`.
- Todo ciclo relevante precisa de identidade explícita.
- `foreign/stale events` não podem alterar o pipeline ativo.

---

## Matriz corrigida após Auditoria 1 — Session Pipeline

| Área atual Base 1.0 | Owner atual provável | Problema de ownership/lifecycle | Novo pipeline owner Base 1.1 | Papel final | Ação recomendada | Prioridade | Status auditoria | ADR Base 1.1 relacionado |
|---|---|---|---|---|---|---|---|---|
| `GameplaySessionFlow` | `GameplaySessionFlow` / prepare/completion gates | Mistura preparação local, ativação, readiness e lifecycle de sessão. Auditoria inicial não isolou P0 direto, mas segue como área agregadora do `Session Pipeline`. | `Session Pipeline` | Pipeline Owner / Pipeline Stage | criar pipeline explícito | P1 necessário | Mantém risco estrutural; auditar por subáreas | ADR-0062, ADR-0060, ADR-0067 |
| `SessionTransition` | `SessionTransitionOrchestrator` / resolver / execution port | Está próximo do owner correto, mas precisa garantir separação entre decisão de pipeline e execução operacional. | `Session Pipeline` | Pipeline Handoff / Pipeline Adapter | formalizar identidade | P1 necessário | Sem achado P0; manter como candidato a stage/handoff canônico | ADR-0062, ADR-0063, ADR-0067 |
| `PhaseEntryReadinessCoordinator` | Readiness coordinator | Confirmado: ainda decide `Blocked/Ready` para entrada de fase a partir de `SceneTransition`, `IntroStage`, `GameplayPhaseRuntime` e `Participation`. Isso mantém policy de lifecycle fora do `Session Pipeline`. | `Session Pipeline` | Pipeline Fact Producer | rebaixar; formalizar identidade; mover decisão final de entrada para pipeline | P1 necessário, com risco P0 | Achado confirmado | ADR-0062, ADR-0063, ADR-0067 |
| `GameplayInteractionReadinessService` | Interaction readiness service | Confirmado: ainda decide quando pedir `Gameplay` no `InputModes` com base em prontidão local. A troca de modo nasce fora do `Session Pipeline`. | `Session Pipeline` emite o comando; `InputModes` executa | Pipeline Command Producer / Pipeline Adapter de ponte | mover decisão para pipeline; manter serviço apenas como produtor/ponte de comando; formalizar identidade | P1 necessário | Achado confirmado | ADR-0063, ADR-0066, ADR-0067 |
| `GameplaySessionFlowPrepareOperationalHandoffService` | Operational handoff service | Confirmado: há retorno silencioso quando `RouteId`/`RouteRef` não servem antes de `skip/no-content` explícito ou fail-fast. | `Session Pipeline` / `SessionTransition` | Pipeline Adapter | trocar retorno mudo por `skip/no-content` explícito ou fail-fast conforme contrato obrigatório | P2 posterior, elevar para P1 se rota for obrigatória | Achado confirmado | ADR-0062, ADR-0063, ADR-0067 |
| `IntroStageCoordinator` | `IntroStageCoordinator` | Trata activation como ownership local/phase-owned. Ainda precisa de auditoria dedicada. | `Session Pipeline` | Pipeline Stage | rebaixar | P0 bloqueante | Pendente Auditoria 2 | ADR-0064, ADR-0062, ADR-0067 |
| `IntroStageLifecycleDispatchService` | Dispatch/lifecycle service | Dispatch pode parecer dono de lifecycle da activation. Ainda precisa de auditoria dedicada. | `Session Pipeline` | Pipeline Adapter / Executor técnico | mover decisão para pipeline | P1 necessário | Pendente Auditoria 2 | ADR-0064, ADR-0063, ADR-0067 |
| `GameplayStateGate` | Gate de estado gameplay | Gate bloqueia/libera e pode aparentar decidir lifecycle. | `Session Pipeline` | Executor técnico | rebaixar | P0 bloqueante | Pendente Auditoria 3 | ADR-0066, ADR-0062 |
| `SimulationGate` | Gate de simulação | Gate executa bloqueio/liberação, mas não deve decidir quando o ciclo avança. | `Session Pipeline` | Executor técnico | mover decisão para pipeline | P0 bloqueante | Pendente Auditoria 3 | ADR-0066, ADR-0067 |
| `InputModes` | Input mode service/controller | InputMode pode carregar semântica de frontend/gameplay/pause. Auditoria 1 confirmou risco via `GameplayInteractionReadinessService`. | `Session Pipeline` / `Session Activity` | Executor técnico | rebaixar; aceitar apenas comando do pipeline | P1 necessário | Parcialmente confirmado | ADR-0066, ADR-0062 |
| `GameLoop` | GameLoop state machine/service | Risco de `GameLoop` decidir run/session lifecycle por estar no centro do runtime. | `Run Pipeline` + `Session Pipeline` | Executor técnico / Pipeline Handoff executor | mover decisão para pipeline | P0 bloqueante | Pendente Auditoria 3 | ADR-0066, ADR-0061, ADR-0062 |
| `ActorsExecution` | Actors materialization/execution services | Materialização/readiness/stale/orphan podem virar decisão de ciclo. | `Session Pipeline` | Pipeline Adapter / Pipeline Fact Producer | mover decisão para pipeline | P0 bloqueante | Pendente Auditoria 4 | ADR-0063, ADR-0067, ADR-0062 |
| `PhaseCatalog / PhaseOrdinalNavigation` | `PhaseCatalog` / ordinal navigation services | Ordem de phase e navegação ordinal podem bypassar pipeline e publicar handoff direto. | `Run Pipeline` + `Session Pipeline` | Pipeline Fact Producer / Pipeline Command Producer | remover trilho antigo | P0 bloqueante | Pendente Auditoria 6 | ADR-0061, ADR-0062, ADR-0063, ADR-0067 |
| `RunEndRail / RunResult / RunDecision` | Run end / post-run services | Fim de run e continuidade podem competir com run ativa como owner global. | `Run Pipeline` — Deactivation / Continuity | Pipeline Stage / Pipeline Command Producer | criar pipeline explícito | P0 bloqueante | Pendente Auditoria 5 | ADR-0061, ADR-0065, ADR-0067 |
| `SceneFlow / Navigation` | Navigation / scene flow services | Scene transition pode decidir sessão/run por conveniência operacional. | `Session Pipeline` / `Run Pipeline` | Pipeline Adapter | rebaixar | P1 necessário | Pendente Auditoria 7 | ADR-0062, ADR-0063, ADR-0067 |
| `RuntimeComposition / Bootstrap` | Bootstrap/installers/composition root | Composition pode virar owner oculto de lifecycle/config/runtime. | Pipelines consomem composition facts/config | Executor técnico / Pipeline Adapter | formalizar identidade | P1 necessário | Pendente Auditoria 7 | ADR-0060, ADR-0063, ADR-0067 |
| Save hooks | Save services/hooks | Save pode decidir progressão/continuidade em vez de executar comando. | `Run Pipeline` / `Session Pipeline` | Pipeline Adapter | rebaixar | P2 posterior | Pendente Auditoria 7 | ADR-0063, ADR-0067, ADR-0061 |
| `PauseResume` | Pause/resume services + input/gates | Pause pode virar lifecycle paralelo via gate/input/game loop. | `Session Pipeline` / `Session Activity` | Pipeline Stage / Executor técnico | criar pipeline explícito | P1 necessário | Pendente Auditoria 7 | ADR-0062, ADR-0066, ADR-0067 |

---

## Correções aplicadas após Auditoria 1

### 1. `PhaseEntryReadinessCoordinator`

Status anterior:

```text
P0 bloqueante hipotético.
```

Status corrigido:

```text
P1 necessário, com risco P0.
```

Razão:

```text
Readiness ainda decide linguagem de entrada (`Blocked/Ready`), mas só vira P0 se esse resultado liberar gate, input, GameLoop ou handoff sem passar pelo Session Pipeline.
```

Ação final:

```text
Rebaixar para Pipeline Fact Producer.
Session Pipeline consome o fact e decide entrada/handoff/policy.
```

### 2. `GameplayInteractionReadinessService`

Status anterior:

```text
P1 genérico.
```

Status corrigido:

```text
P1 confirmado.
```

Razão:

```text
O serviço decide pedido de InputMode Gameplay com base em prontidão local.
Na Base 1.1, o Session Pipeline emite o Pipeline Command; InputModes executa.
```

Ação final:

```text
Mover decisão para Session Pipeline.
Manter o serviço apenas como ponte/produtor de comando, com identidade explícita e filtro contra foreign/stale events.
```

### 3. `GameplaySessionFlowPrepareOperationalHandoffService`

Novo item adicionado à matriz.

Razão:

```text
Retorno silencioso em RouteId/RouteRef inválido ou incompatível viola ausência explícita.
```

Ação final:

```text
Trocar return mudo por skip/no-content explícito ou fail-fast, conforme o contrato do rail.
```

---

## Corte P0/P1 após Auditoria 1

### P0 ainda não confirmado

Nenhum P0 confirmado na Auditoria 1.

### P1 confirmado

1. `PhaseEntryReadinessCoordinator`
2. `GameplayInteractionReadinessService`

### P2 confirmado

1. `GameplaySessionFlowPrepareOperationalHandoffService`

Elevar para P1 se `RouteId`/`RouteRef` forem contrato obrigatório do rail.
