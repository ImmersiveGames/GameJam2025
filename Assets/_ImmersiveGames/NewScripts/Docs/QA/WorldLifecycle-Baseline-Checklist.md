# Checklist de Validação — WorldLifecycle (Baseline)

## Objetivo

Validar **exclusivamente por logs** que o **WorldLifecycle** está:

- Determinístico (ordem e fases previsíveis)
- Serializado por **gate** (nenhuma execução concorrente)
- Correto em **Hard Reset** (despawn + respawn)
- Correto em **Soft Reset Players (reset-in-place)**

> Este QA valida **infraestrutura** e **contratos de ciclo de vida**.
> Ele **não valida gameplay**, FSM, input, HUD ou balanceamento.

---

## Pré-condições

- Cena de boot/QA com NewScripts habilitado (ex.: `NewBootstrap`).
- `WorldLifecycleBaselineRunner` presente na cena.
- `WorldLifecycleController.AutoInitializeOnStart` deve estar **desabilitado** pelo runner (antes do `Start`), para evitar reset automático.
- Logs em nível **INFO/VERBOSE** (pelo menos para `WorldLifecycle*`, `SimulationGateService` e spawn services).

---

## Execução

1. Abrir a cena de boot/QA e rodar Play.
2. Usar o menu de contexto do `WorldLifecycleBaselineRunner`:
    - **Full Baseline** (Hard Reset + Soft Reset Players), ou
    - rodar separadamente **Hard Reset** e **Soft Reset Players**.

---

## Critérios de Aceite — Hard Reset

O Hard Reset deve apresentar, em ordem lógica, os seguintes sinais (substrings típicas):

### 1) Início e Gate
- `WorldLifecycleController` loga **Reset iniciado** com `reason=` e `scene=`.
- `WorldLifecycleOrchestrator` loga **World Reset Started**.
- `SimulationGateService` loga `Acquire token='WorldLifecycle.WorldReset'` e `Active=1`.
- `WorldLifecycleOrchestrator` loga **Gate Acquired (WorldLifecycle.WorldReset)**.

### 2) Hooks de cena (quando existirem)
- `OnBeforeDespawn phase started` (hooks >= 0).
- `OnAfterDespawn phase started` (hooks >= 0).
- `OnBeforeSpawn phase started` (hooks >= 0).
- `OnAfterSpawn phase started` (hooks >= 0).

> Observação: hooks podem ser 0 em cenas mínimas; isso **não falha** o baseline.

### 3) Despawn (services)
- `Despawn started`
- Para cada spawn service habilitado:
    - `Despawn service started: <ServiceName>`
    - `DespawnAsync iniciado`
    - `Despawn service completed: <ServiceName>`

### 4) Spawn (services) + registro
- `Spawn started`
- Para cada spawn service habilitado:
    - `Spawn service started: <ServiceName>`
    - `SpawnAsync iniciado`
    - Um ator deve ser registrado no `ActorRegistry` (ex.: `Ator registrado: <ActorId>`)
    - `Spawn service completed: <ServiceName>`

### 5) Hooks de ator (quando existirem)
- `OnAfterActorSpawn actor hooks phase started (actors=...)`
- Para ao menos 1 ator, execução de hooks (quando configurados).

### 6) Gate Release + finalização
- `SimulationGateService` loga `Release token='WorldLifecycle.WorldReset'` e `Active=0`, `IsOpen=True`.
- `WorldLifecycleOrchestrator` loga **Gate Released** e **World Reset Completed**.
- `WorldLifecycleController` loga **Reset concluído**.
- Runner loga `Hard Reset - END` e marca **SUCCESS**.

---

## Critérios de Aceite — Soft Reset Players (reset-in-place)

### Intenção atual (MVP / Smoke Test)

O Soft Reset Players existe para validar **infra de escopos** (gate + filtro + execução de participantes),
mantendo o payload propositalmente mínimo enquanto os controllers legados ainda não foram migrados.

**Portanto:**
- É aceitável haver **apenas 1 resetável** (ou poucos) coletado no player.
- O baseline **não exige** mudança observável de gameplay.

### Evidências mínimas esperadas (por logs)

#### 1) Início e Gate
- `WorldLifecycleController` loga **Soft reset (Players) iniciado** com `reason=` e `scene=`.
- `WorldLifecycleOrchestrator` loga **Scoped Reset Started** com `Scopes=Players` e `Flags=SoftReset`.
- `SimulationGateService` loga `Acquire token='flow.soft_reset'` e `Active=1`.
- `WorldLifecycleOrchestrator` loga **Gate Acquired (flow.soft_reset)**.

#### 2) Garantia reset-in-place (sem despawn/spawn)
- Durante o soft reset:
    - `Despawn service skipped by scope filter: <ServiceName>`
    - `Spawn service skipped by scope filter: <ServiceName>`

**Falha imediata** se aparecer:
- `Despawn service started:` / `DespawnAsync iniciado` (durante soft reset)
- `Spawn service started:` / `SpawnAsync iniciado` (durante soft reset)

#### 3) Execução do participante de escopo
- Deve haver linha de ordenação:
    - `Scoped reset execution order: PlayersResetParticipant(scope=Players, order=...)`
- Deve haver start/end do participante:
    - `ResetScope.Players start`
    - `Resetables collected ... count=...`
    - `ResetScope.Players end`

#### 4) Gate Release + finalização
- `SimulationGateService` loga `Release token='flow.soft_reset'` e `Active=0`, `IsOpen=True`.
- `WorldLifecycleOrchestrator` loga **Scoped Reset Completed**.
- `WorldLifecycleController` loga **Soft reset (Players) concluído**.
- Runner loga `Soft Reset Players - END` e marca **SUCCESS**.

---

## Invariantes adicionais (recomendados)

- Durante qualquer reset, sistemas gate-aware devem registrar `GateChanged: open=False`.
- Ao final, devem registrar `GateChanged: open=True`.
- Não deve haver exceções ou erros no Console durante o baseline.

---

## Condições de Falha

- Gate não adquirido ou não liberado.
- Ordem de fases quebrada (ex.: Spawn antes de Despawn no hard reset).
- Soft reset executando `SpawnAsync`/`DespawnAsync` (violação de reset-in-place).
- Execução concorrente (ex.: segundo reset iniciando antes do release do anterior).
- Runner não conclui com **SUCCESS**.
- Exceções / erros no Console durante o ciclo.

---

## Regra de Ouro

> **WorldLifecycle Baseline passa primeiro.**
> **GameLoop QA passa depois.**

Se o baseline falha, **qualquer QA funcional é inválido**.
