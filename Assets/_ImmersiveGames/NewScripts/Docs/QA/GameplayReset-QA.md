# QA – Gameplay Reset (NewScripts)

Este QA valida o reset determinístico em `GameplayScene` via `WorldLifecycle`, incluindo:

- despawn/spawn de múltiplos atores (baseline: Player + Eater);
- comportamento dos targets/grupos (quando executados via ferramentas de QA/debug).

---

## Pré-requisitos

- Seguir o QA de `GameLoop-StateFlow-QA.md` até estar em `GameplayScene` com `GameLoop=Playing`.
- `WorldDefinition` da `GameplayScene` deve ter, no mínimo, **Player** e **Eater** habilitados.

---

## 1) Reset completo (equivalente a `AllActorsInScene`)

### Ação (produção)
1. Disparar o fluxo de restart que recarrega `GameplayScene` (ex.: evento de reset, botão/atalho de QA que publica o comando de restart).

### Esperado (logs)
1. Scene Flow inicia transição para `GameplayScene` (profile `gameplay`).
2. `WorldLifecycleRuntimeCoordinator` dispara hard reset após `SceneTransitionScenesReadyEvent`.
3. `WorldLifecycleOrchestrator` executa:
    - Despawn: services em ordem (Player, Eater)
    - Spawn: services em ordem (Player, Eater)
    - Gate `WorldLifecycle.WorldReset` adquirido durante todo o ciclo
4. `ActorRegistry` ao final do reset:
    - count == 2
    - ids dos atores mudam (novas instâncias)
5. `WorldLifecycleResetCompletedEvent` é emitido com reason `ScenesReady/GameplayScene`.
6. `WorldLifecycleResetCompletionGate` libera o `SceneFlow` para `FadeOut` apenas após o evento de conclusão.

### Evidências (trechos curtos)
- `[WorldLifecycleRuntimeCoordinator] Disparando hard reset após ScenesReady. reason='ScenesReady/GameplayScene'`
- `[WorldLifecycleOrchestrator] Gate Acquired (WorldLifecycle.WorldReset)`
- `[WorldLifecycleResetCompletionGate] Completed for signature=...`

---

## 2) Reset parcial – `PlayersOnly` (QA/Debug)

### Ação
1. Executar a rotina de reset parcial “PlayersOnly” (menu QA/debug do projeto).

### Esperado
1. Apenas o Player é despawnado e respawnado.
2. O Eater permanece ativo (mesma instância/id, se o projeto não recria o Eater nesse target).
3. Logs devem indicar claramente o target selecionado e os services/atores afetados.

---

## 3) Reset parcial – `EaterOnly` (QA/Debug)

### Ação
1. Executar a rotina de reset parcial “EaterOnly” (menu QA/debug do projeto).

### Esperado
1. Apenas o Eater é despawnado e respawnado.
2. O Player permanece ativo (mesma instância/id).
3. Logs devem indicar claramente o target selecionado e os services/atores afetados.

---

## 4) Reset por `ActorIdSet` e `ByActorKind` (QA/Debug)

### Ação
1. Garanta que a cena tenha `GameplayResetRequestQaDriver` e `GameplayResetKindQaSpawner` ativos.
2. Use o menu de contexto **QA/GameplayResetRequest/Fill ActorIds...** no driver para preencher IDs.
3. Execute:
   - **Run ActorIdSet** (driver)
   - **Run ByActorKind** (driver)
   - (Opcional) **Run Reset Kind Dummy/Player** (spawner)

### Esperado
1. `ActorIdSet` deve resetar **apenas** os IDs informados.
2. `ByActorKind` deve resetar **apenas** o `ActorKind` solicitado.
3. Logs devem refletir:
   - “`Resolved targets:` ...” com a contagem esperada.
   - fases `Cleanup/Restore/Rebind` por target (ex.: `GameplayResetPhaseLogger`).

### Evidência referência
- [QA-GameplayReset-RequestMatrix](../Reports/QA-GameplayReset-RequestMatrix.md)
- [QA-GameplayResetKind](../Reports/QA-GameplayResetKind.md)

---

## Notas

- Durante reset, é esperado que `IStateDependentService` bloqueie ações por:
    - `GateClosed` (durante transição/reset)
    - `GameplayNotReady` (antes de `SceneTransitionCompletedEvent`)
    - `NotPlaying` (se o `GameLoop` ainda não estiver em Playing)
- Se o reset parcial não estiver exposto no build atual, mantenha este QA como referência para quando o harness/menus de QA estiverem disponíveis.
