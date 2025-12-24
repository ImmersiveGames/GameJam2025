# QA — GameLoop + StateDependent

## Objetivo

Validar o **comportamento funcional do gameplay** no NewScripts, especificamente:

* FSM do **GameLoop** (estado macro).
* Fluxo de **start sincronizado com Scene Flow (Opção B)**.
* Pausa, retomada e reset do loop.
* Bloqueio/liberação de ações via `IStateDependentService`.
* Integração correta com `SimulationGateService`.

> **Escopo explícito**
> Este QA **não valida** spawn, despawn, ordem de hooks ou determinismo do WorldLifecycle.
> Ele **assume** que a infraestrutura já está correta.

---

## Mapa Rápido — Quando rodar este QA

| Situação                              | Rodar este QA?   | Motivo                      |
| ------------------------------------- | ---------------- | --------------------------- |
| Alteração no GameLoop (FSM, estados)  | ✅ Obrigatório    | Garante transições corretas |
| Mudança em pausa / resume             | ✅ Obrigatório    | Valida gates e bloqueios    |
| Alteração em `IStateDependentService` | ✅ Obrigatório    | Evita input indevido        |
| Mudança em Scene Flow (start)         | ✅ Obrigatório    | Evita start duplo           |
| Alteração em WorldLifecycle           | ❌ Não suficiente | Use o Baseline              |
| Investigação de bug de gameplay       | ✅ Recomendado    | Foco funcional              |

---

## QAs Ativos

### 1) GameLoopStateFlowQATester

**Arquivo**
`Assets/_ImmersiveGames/NewScripts/Infrastructure/QA/GameLoopStateFlowQATester.cs`

#### O que cobre

**FSM do GameLoop**

* Boot → Menu
* Menu → Playing
* Playing → Paused
* Paused → Playing
* Reset → Boot → Menu

**Start (Opção B — Scene Flow)**

* `GameStartEvent` não inicia o jogo imediatamente.
* Start só ocorre após `SceneTransitionScenesReadyEvent` (profile `startup`).
* `RequestStart()` é chamado **exatamente uma vez**.

**StateDependent / Gates**

* `ActionType.Move`:

    * Bloqueado em `Menu`
    * Bloqueado em `Paused`
    * Liberado em `Playing`
* Gate `SimulationGateTokens.Pause` bloqueia Move mesmo em `Playing`.

#### Como executar

1. Cena com:

    * `GlobalBootstrap`
    * Scene Flow nativo
    * GameLoop registrado
2. Garantir fluxo **Opção B** (coordinator ativo).
3. Executar:

    * ContextMenu: `QA/GameLoop/State Flow/Run`
      **ou**
    * `runOnStart = true`
4. Validar logs:

   ```
   [QA][GameLoopStateFlow] PASS
   ```

#### Critério de aprovação

* Nenhum FAIL.
* Nenhum start duplo.
* Bloqueios e liberações coerentes com estado.

---

### 2) PlayerMovementLeakSmokeBootstrap

**Arquivo**
`Assets/_ImmersiveGames/NewScripts/Infrastructure/QA/PlayerMovementLeakSmokeBootstrap.cs`

#### O que cobre

* Gate bloqueia movimento **sem congelar física**.
* Reset limpa estado de movimento.
* Reabertura do gate **não gera input fantasma**.
* Integração real com `PlayerMovementController`.

#### Como executar

* Entrar em Play Mode com cena padrão (`NewBootstrap`).
* Runner é automático.
* Relatório gerado em:

  ```
  Docs/Reports/PlayerMovement-Leak.md
  ```

---

## O que este QA NÃO garante

* Ordem de hooks do WorldLifecycle.
* Determinismo de spawn/despawn.
* Reset-In-Place correto.
* Integridade de registries.

👉 Para isso, **use o checklist de baseline**.
