# World Lifecycle (NewScripts)

> Este documento descreve **operacionalmente** o comportamento do **WorldLifecycle** no **domínio da simulação**
> e implementa as decisões descritas no
> **ADR – Ciclo de Vida do Jogo, Reset por Escopos e Fases Determinísticas**
> (`../ADR/ADR-ciclo-de-vida-jogo.md`).

> **Escopo explícito deste documento**
> Este documento **não descreve navegação de App / Frontend / Menus visuais**.
> Ele descreve **exclusivamente o ciclo de vida da simulação** executada **dentro de uma cena de Gameplay**.

---

## 1. Visão Geral

O **WorldLifecycle** é responsável por:

* Construir a simulação
* Resetar a simulação
* Garantir determinismo entre execuções
* Serializar operações críticas via `ISimulationGateService`

Ele **não**:

* controla telas de menu
* navega entre cenas
* define UX de App

Essas responsabilidades pertencem ao **App Frontend / Scene Flow**.

### Contratos principais

* **Hard Reset (Full Reset)**
  Reconstrói completamente o mundo da simulação
  (despawn + spawn)

* **Soft Reset Players (Reset-In-Place)**
  Reset **lógico por escopo**, preservando instâncias, identidades e hierarquia física

---

## 2. Notas de Infraestrutura (Fronteiras Arquiteturais)

### FSMs

* Infra genérica de FSM:
  `_ImmersiveGames/NewScripts/Infrastructure/Fsm`
* FSM concreta do GameLoop (controle de simulação):
  `_ImmersiveGames/NewScripts/Gameplay/GameLoop`

### Bootstrap (fronteira com legado)

* O **legado não inicializa o NewScripts**
* O `DependencyBootstrapper` legado **não registra serviços do NewScripts**
* Em `NEWSCRIPTS_MODE`:

    * `GlobalBootstrap` registra:

        * `ISimulationGateService`
        * `GamePauseGateBridge`
        * `NewScriptsStateDependentService`
        * `WorldLifecycleRuntimeDriver`
    * Tudo ocorre **antes das cenas**
    * Sem dependência do bootstrap legado

➡️ Essa separação é **intencional e não negociável**.

---

## 3. Pause — Gate sem congelar física

### Semântica correta de Pause

Pause **não é**:

* `Time.timeScale = 0`
* congelar `Rigidbody`
* interromper física

Pause **é**:

* bloqueio lógico de ações

### Pipeline de Pause

```
GamePauseEvent(paused=true)
→ GamePauseGateBridge
→ SimulationGateTokens.Pause
→ ISimulationGateService
→ NewScriptsStateDependentService
→ ActionType.Move bloqueado
```

### Efeito prático

* Física continua rodando
* Gravidade continua atuando
* Controladores deixam de aplicar input/velocidade
* Nenhuma mutação em `timeScale`

➡️ O **serviço oficial de permissões** é
`NewScriptsStateDependentService` (registrado pelo `GlobalBootstrap`)

Para estados globais (Boot / Playing / Paused) veja:
`../GameLoop/GameLoop.md`

---

## 4. Reset Determinístico — Hard Reset (Full Reset)

Pipeline garantido pelo `WorldLifecycleOrchestrator`:

```
Acquire Gate (WorldLifecycle.WorldReset)
→ OnBeforeDespawn (World)
→ OnBeforeActorDespawn
→ DespawnAsync
→ OnAfterDespawn
→ IResetScopeParticipant (se houver)
→ OnBeforeSpawn
→ SpawnAsync
→ OnAfterActorSpawn
→ OnAfterSpawn
→ Release Gate
```

### Propriedades garantidas

* Ordem estável
* Fail-fast
* Logs de duração
* Serialização via gate

### Logs de skip

Se uma fase não tiver hooks:

```
<PhaseName> phase skipped (hooks=0)
```

➡️ Isso **é esperado**, não erro.

---

## 5. Reset por Escopo — Soft Reset Players (Reset-In-Place)

### Contrato arquitetural

Soft Reset Players **não reconstrói o mundo**.

Durante este fluxo:

* ❌ `DespawnAsync` não é chamado
* ❌ `SpawnAsync` não é chamado
* ✅ Instâncias preservadas
* ✅ `ActorId` preservado
* ✅ `ActorRegistry` permanece estável

### Gate

* Token: `SimulationGateTokens.SoftReset` (`flow.soft_reset`)

### Pipeline Soft Reset Players

```
Acquire Gate (flow.soft_reset)
→ (opcional) World Hooks
→ OnBeforeActorDespawn (atores existentes)
→ IResetScopeParticipant (Players)
→ OnAfterActorSpawn (atores existentes)
→ (opcional) World Hooks finais
→ Release Gate
```

Logs esperados:

```
Despawn service skipped by scope filter
Spawn service skipped by scope filter
```

➡️ Evidência positiva de reset-in-place correto.

---

## 6. Hard Reset vs Soft Reset (Resumo)

| Aspecto       | Hard Reset    | Soft Reset Players |
| ------------- | ------------- | ------------------ |
| Despawn       | Sim           | Não                |
| Spawn         | Sim           | Não                |
| ActorId       | Recriado      | Preservado         |
| GameObject    | Reinstanciado | Mantido            |
| ActorRegistry | Recomposto    | Mantido            |
| Gate          | WorldReset    | SoftReset          |
| Semântica     | Reconstrução  | Reset lógico       |

---

## 7. Cache de Hooks de Ator

* Hooks (`IActorLifecycleHook`) podem ser cacheados por `Transform`
* Cache:

    * válido apenas durante o reset
    * limpo no `finally`
* Nenhum cache entre resets

---

## 8. Scene Flow × WorldLifecycle (Separação de Domínios)

* **Scene Flow**:

    * troca de cenas
    * readiness
    * binds cross-scene
* **WorldLifecycle**:

    * reset
    * spawn/despawn
    * simulação

### Linha do tempo oficial

```
SceneTransitionStarted
↓
SceneScopeReady
↓
SceneTransitionScenesReady
↓
WorldLoaded
↓
SpawnPrewarm
↓
SceneScopeBound
↓
SceneTransitionCompleted
↓
GameplayReady
↓
[Soft Reset] ou [Hard Reset]
```

Fonte:
`../ADR/ADR-ciclo-de-vida-jogo.md#definição-de-fases-linha-do-tempo`

---

## 9. Readiness (Contrato Funcional)

* **SceneScopeReady**
  Registries prontos, gameplay bloqueado
* **WorldLoaded**
  Serviços de mundo podem preparar dados
* **SceneScopeBound**
  UI / HUD podem bindar
* **GameplayReady**
  Gate liberado, simulação ativa

➡️ Gameplay **nunca** roda antes de `GameplayReady`.

---

## 10. Spawn Determinístico e Late Bind

Passes oficiais:

1. SpawnPrewarm
2. World Services
3. Actors
4. Late Bindables
5. UI Bind

Regra:

* UI conecta **após SceneScopeBound**
* antes de `GameplayReady`

---

## 11. ResetScope.Players — Semântica Correta

### Escopo é funcional, não estrutural

`ResetScope.Players` representa:

* baseline de gameplay do jogador
* input, câmera, HUD, timers, caches

❌ **Não** significa:

> “reset apenas dos componentes do prefab Player”

### Participantes podem ser externos

* UI
* câmera
* domínios
* serviços
* timers

---

## 12. Baseline Audit — ResetScope.Players (2025-12-19)

### Estado atual (As-Is)

* Infra de reset por escopo existe e funciona
* Pipeline correto
* Gate correto
* **Payload funcional inexistente**

### Participantes

* Apenas `PlayersResetParticipant`
* Executa apenas logs

### Conclusão

Soft Reset Players está **arquiteturalmente correto**, mas **funcionalmente vazio**.

➡️ Próximo passo:
Conectar resets reais via `IResetScopeParticipant`, **sem alterar o pipeline**.

---

## 13. Registry, Injeção e Boot Order

* `WorldLifecycleHookRegistry` criado apenas no `NewSceneBootstrapper`
* Consumidores devem:

    * usar `Start()` ou
    * lazy injection com retry curto

---

## 14. Troubleshooting QA / Boot

Checklist:

1. `NewSceneBootstrapper` presente
2. Lazy injection
3. Mensagem acionável em falha

---

## 15. Migração Legado → NewScripts

* Ver ADR-0001
* Guardrails:

    * sem referência direta ao legado
    * bridges explícitas
    * gate sempre ativo

---

## 16. Baseline Validation

Checklist:
`../QA/WorldLifecycle-Baseline-Checklist.md`

---

## Nota Final de Semântica

Este documento descreve **o ciclo de vida da simulação**
executada **dentro de GameplayScene**.

Menus de App, splash screens e navegação pertencem a outro domínio.

Essa separação é **intencional** e **fundamental** para escalar o projeto.
