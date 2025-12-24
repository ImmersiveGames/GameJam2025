
# Checklist de Validação — WorldLifecycle (Baseline)

## Objetivo

Validar **exclusivamente por logs** que o **WorldLifecycle** está:

* Determinístico
* Serializado por gate
* Correto em hard reset
* Correto em soft reset (reset-in-place)

> **Este QA valida infraestrutura.**
> Ele **não valida gameplay**, FSM ou input.

---

## Mapa Rápido — Quando rodar este checklist

| Situação                   | Rodar este QA?    | Motivo            |
| -------------------------- | ----------------- | ----------------- |
| Mudança no WorldLifecycle  | ✅ Obrigatório     | Infra crítica     |
| Mudança em reset           | ✅ Obrigatório     | Evita regressões  |
| Alteração em gates         | ✅ Obrigatório     | Serialização      |
| Alteração em spawn/despawn | ✅ Obrigatório     | Determinismo      |
| Alteração no GameLoop      | ❌ Não necessário  | Use GameLoop QA   |
| Bug visual de gameplay     | ❌ Não prioritário | Infra já validada |

---

## Hard Reset — Full Reset

### Como disparar

ContextMenu no `WorldLifecycleController`:

```
QA/Reset World Now
```

### Ordem esperada (obrigatória)

1. Acquire gate `WorldLifecycle.WorldReset`
2. OnBeforeDespawn (world hooks)
3. OnBeforeActorDespawn (actor hooks)
4. `DespawnAsync`
5. OnAfterDespawn
6. OnBeforeSpawn
7. `SpawnAsync`
8. OnAfterActorSpawn
9. OnAfterSpawn
10. Release gate

### PASS

* Todas as fases na ordem.
* Logs explícitos de *phase skipped* quando aplicável.
* `World Reset Completed`.

### FAIL

* Gate ausente.
* Ordem quebrada.
* Spawn/despawn fora de fase.

---

## Soft Reset Players — Reset-In-Place

### Como disparar

ContextMenu:

```
QA/Soft Reset Players Now
```

### Contrato obrigatório

* **Não existe despawn**
* **Não existe spawn**
* Instâncias preservadas
* `ActorId` preservado
* Registry mantém contagem

### Ordem esperada

1. Acquire gate `flow.soft_reset`
2. `ResetContext.Scopes = [Players]`
3. Hooks de ator (se existirem)
4. Execução de `PlayersResetParticipant`
5. Hooks finais (se existirem)
6. Release gate

### Evidência positiva esperada

Logs como:

```
Despawn service skipped by scope filter
Spawn service skipped by scope filter
```

### FAIL imediato

* Qualquer `SpawnAsync` ou `DespawnAsync`.
* Novo `ActorId`.
* Participante fora do escopo `Players`.

---

## Pré-condições obrigatórias

* Cena: `NewBootstrap`
* Componentes:

    * `NewSceneBootstrapper`
    * `WorldLifecycleController`
    * `WorldDefinition` válido
* Hooks QA recomendados:

    * `SceneLifecycleHookLoggerA/B`
    * `ActorLifecycleHookLogger`

---

## Fluxos suportados

### Fluxo A — Produção (AutoInit)

* `AutoInitializeOnStart = true`
* Reset ocorre automaticamente
* Bootstrap controla warnings

### Fluxo B — Runner (QA explícito)

* `WorldLifecycleBaselineRunner`
* F7 → Hard Reset
* F8 → Soft Reset Players
* F9 → Baseline completo
* Runner controla warnings

---

## Critérios globais de reprovação

* Gate não adquirido/liberado
* Ordem quebrada
* Soft reset com spawn/despawn
* Warnings não restaurados
* Execução concorrente não bloqueada

---

## Regra de Ouro

> **WorldLifecycle Baseline passa primeiro.
> GameLoop QA passa depois.**

Se o baseline falha, **qualquer QA funcional é inválido**.
