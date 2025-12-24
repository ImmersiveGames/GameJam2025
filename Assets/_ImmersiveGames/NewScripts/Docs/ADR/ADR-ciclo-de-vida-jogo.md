# ADR – Ciclo de Vida do Jogo, Reset por Escopos e Fases Determinísticas

> **Owner deste ADR**: decisões arquiteturais sobre **fases, escopos e reset-in-place**
> **Owner operacional (pipeline, ordem, edge cases e troubleshooting)**:
> `../WorldLifecycle/WorldLifecycle.md`

---

## Contexto

A evolução incremental do projeto revelou problemas estruturais recorrentes:

### Ordem e Readiness

* Ausência de fases formais de readiness causa corridas entre:

    * boot de cena
    * spawn de atores
    * binds de UI
    * liberação de gameplay
* Sistemas iniciam comportamento antes de o mundo estar consistente.

### Spawn

* Pipelines de spawn variam por cena.
* Não há previsibilidade sobre:

    * quando pools estão aquecidos
    * quando atores existem
    * quando serviços dependentes podem agir.

### Bind (UI cross-scene)

* HUDs e overlays compartilham dados entre cenas.
* Binds ocorrem cedo demais (referências nulas) ou tarde demais (perda de eventos).

### Reset

* Não existe contrato único para:

    * reset completo
    * reset parcial
* Controladores duplicam lógica, muitas vezes violando determinismo.

---

## Objetivos

Este ADR estabelece decisões para:

1. Definir **fases oficiais de readiness** do jogo.
2. Padronizar **spawn determinístico em passes explícitos**.
3. Permitir **late bind de UI cross-scene** sem heurísticas.
4. Introduzir **resets por escopo** (soft / hard) com semântica clara.
5. Integrar Scene Flow, WorldLifecycle e GameLoop **sem alterar APIs existentes**.

---

## Decisões Arquiteturais

### Separação clara de responsabilidades

| Sistema            | Responsabilidade                           |
| ------------------ | ------------------------------------------ |
| **GameLoop**       | Estado macro da **simulação**              |
| **Scene Flow**     | Readiness de cena + binds cross-scene      |
| **WorldLifecycle** | Reset determinístico (spawn/despawn/reset) |
| **SimulationGate** | Serialização e bloqueio operacional        |

---

### FSM / Game Flow Controller

* Permanece **simples**
* Opera apenas com sinais de alto nível:

    * `EnterFrontend`
    * `StartSimulation`
    * `ExitSimulation`
* **Não executa** spawn, reset ou bind.
* Delegação explícita para Scene Flow + WorldLifecycle.

---

### Scene Flow

* Responsável por:

    * readiness da cena
    * coordenação de binds cross-scene
* Introduz fases formais:

    * `SceneScopeReady`
    * `SceneScopeBound`
* Atua como **gatekeeper** antes do gameplay.

---

### WorldLifecycle

* Único responsável por:

    * despawn
    * spawn
    * reset
* Executa pipelines **determinísticos**
* Passa a suportar:

    * **Hard Reset**
    * **Soft Reset por escopo (Reset-In-Place)**

---

### Coordenação entre sistemas

* Scene Flow **adquire e libera** o gate de readiness.
* WorldLifecycle **executa reset** respeitando o gate.
* GameLoop **apenas reflete o estado da simulação**.

---

## Definição de Fases (linha do tempo)

Fases oficiais do ciclo de vida:

```
SceneScopeReady
→ WorldServicesReady
→ SpawnPrewarm
→ SceneScopeBound
→ GameplayReady
```

**Owner do detalhamento operacional**:
`../WorldLifecycle/WorldLifecycle.md#fases-de-readiness`

### Semântica das fases

* **SceneScopeReady**

    * Gate adquirido
    * Registries de cena disponíveis
* **WorldServicesReady**

    * Serviços de mundo configurados
* **SpawnPrewarm**

    * Aquecimento de pools e recursos
* **SceneScopeBound**

    * Late bind liberado (HUD, overlays)
* **GameplayReady**

    * Gate liberado
    * Simulação autorizada

---

## Reset Scopes

### Owner das semânticas

* `../WorldLifecycle/WorldLifecycle.md#resets-por-escopo`
* `../WorldLifecycle/WorldLifecycle.md#reset-por-escopo--soft-reset-players-reset-in-place`

---

## Soft Reset Semantics — Reset-In-Place (DECISÃO FORMAL)

### Decisão Arquitetural

> **Soft Reset Players é, por definição, Reset-In-Place**

Isso significa:

* ❌ **Sem despawn**
* ❌ **Sem spawn**
* ❌ **Sem recriação de GameObjects**
* ❌ **Sem mudança de ActorId**

✔️ Instâncias e identidades são preservadas
✔️ Apenas estado lógico é resetado
✔️ Referências externas permanecem válidas

---

### Contrato do Soft Reset

* Gate utilizado: `flow.soft_reset`
* Participantes:

    * apenas `IResetScopeParticipant` cujo `Scope` esteja presente em `ResetContext.Scopes`
* Escopos são **opt-in**
* Não afeta:

    * registries de cena
    * binds de UI
    * pools
    * hierarquia de objetos

---

### Justificativa da decisão

* Reduz churn e GC
* Mantém referências externas estáveis (UI, câmera, serviços)
* Permite retries rápidos
* Evita reconstrução desnecessária do mundo
* Fornece baseline funcional previsível

➡️ **Soft Reset não é “mini hard reset”**
➡️ É um **reset lógico controlado por contrato**

---

## Hard Reset (Contraponto)

* Despawn + Spawn completos
* Registries recompostos
* Binds refeitos
* Gate reacquirido
* Mundo reconstruído

➡️ Usado para:

* troca de cena
* reload estrutural
* transições de mundo

---

## Spawn Passes (Decisão)

Pipeline determinístico em passes explícitos:

1. SpawnPrewarm
2. World Services
3. Actors
4. Late Bindables
5. UI Bind (após SceneScopeBound)

**Owner operacional**:
`../WorldLifecycle/WorldLifecycle.md#spawn-determinístico-e-late-bind`

---

## Late Bind (UI cross-scene)

### Decisão

* Binds de HUD e overlays:

    * **somente após `SceneScopeBound`**
* Nunca durante spawn ou reset.

Isso elimina:

* referências nulas
* corrida de binds
* heurísticas de retry em UI

---

## Uso do SimulationGateService

* Obrigatório para:

    * readiness
    * resets
    * transições
* Scene Flow:

    * adquire gate em `SceneScopeReady`
    * libera em `GameplayReady`
* Hard Reset:

    * reabre gate
* Soft Reset:

    * reutiliza gate existente
    * bloqueia apenas durante o reset-in-place

---

## Linha do Tempo Oficial (Consolidada)

```
SceneTransitionStarted
↓
SceneScopeReady (Gate Acquired, registries prontos)
↓
SceneTransitionScenesReady
↓
WorldLoaded
↓
SpawnPrewarm
↓
SceneScopeBound (Late Bind liberado)
↓
SceneTransitionCompleted
↓
GameplayReady (Gate liberado)
↓
[Soft Reset → WorldLifecycle reset scoped (Reset-In-Place)]
[Hard Reset → Desbind + WorldLifecycle full reset + reacquire gate]
```

---

## Consequências

* Determinismo forte entre cenas
* Observabilidade clara via logs
* UI resiliente a reloads e resets
* Reset previsível e reutilizável
* Eliminação de heurísticas em controladores

---

## Não-objetivos

* Alterar APIs públicas existentes
* Implementar multiplayer online
* Definir UX de menus
* Criar testes automatizados neste ADR

---

## Plano de Implementação (fases)

1. Contratos e sinais
2. Scene Flow
3. WorldLifecycle
4. UI Late Bind
5. Telemetria e QA manual

---

### Estado do ADR

**Status**: ✅ **Decisão consolidada**
**Implementação**: parcialmente concluída (ver baseline no `WorldLifecycle.md`)

---

## Addendum (2025-12-24) — SceneFlow/Coordenação

### Integração SceneFlow → WorldLifecycle
A transição de cenas (SceneFlow) define o “momento determinístico” para iniciar o reset do mundo:

- **Trigger:** `SceneTransitionScenesReadyEvent`
- **Responsável:** `WorldLifecycleRuntimeDriver`
- **Ação:** `WorldLifecycleController.ResetWorldAsync(reason="ScenesReady/<ActiveScene>")` para perfis de gameplay.

No perfil `startup` (MenuScene), o driver deve **skippar** o reset. Isto mantém MenuScene como uma cena “sem infra/spawn”.

### Correlação de transições (assinatura)
Quando o GameLoop aguarda a conclusão, ele precisa correlacionar:
- `SceneTransitionCompletedEvent` e
- `WorldLifecycleResetCompletedEvent`

pela mesma assinatura do contexto. Recomendação: utilizar `SceneTransitionContext.ToString()` (ou um ID equivalente gerado no disparo).

### Nota de implementação: structs e nullability
`SceneTransitionContext` é um `readonly struct`. Portanto:
- não existe “context == null”.
- checks de nulidade devem ser aplicados apenas às **propriedades** (`ScenesToLoad`, `ScenesToUnload`, `TargetActiveScene`, etc.).
