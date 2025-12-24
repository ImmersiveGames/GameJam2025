# GameLoop (NewScripts)

## Escopo e Responsabilidade

O **GameLoop** define o **estado macro da simulação** dentro de uma **GameplayScene**.

Ele **não representa**:

* navegação de App
* menus de frontend
* telas de splash ou UX

Esses pertencem ao **App Frontend / Scene Flow**.

### O que o GameLoop controla

* Ativação ou bloqueio da **simulação**
* Permissões globais de ação (via `IStateDependentService`)
* Estados macro **internos da simulação**

➡️ O GameLoop **não carrega semântica de UI**, apenas **estado operacional da simulação**.

---

## Estados do GameLoop (Semântica Corrigida)

Os estados **não representam telas**, mas **fases da simulação**:

| Estado                     | Significado                                              |
| -------------------------- | -------------------------------------------------------- |
| **Boot**                   | Simulação ainda não inicializada (pré-reset / pré-start) |
| **Ready** *(antes “Menu”)* | Simulação construída, mas **não ativa**                  |
| **Playing**                | Simulação ativa                                          |
| **Paused**                 | Simulação pausada (ações bloqueadas via gate)            |

> ⚠️ **Nota importante**
> O nome `Menu` é legado conceitual.
> No NewScripts, ele representa **“Simulation Ready / Idle”**, não um menu visual.

---

## Objetivo

O GameLoop:

* Mantém uma FSM **determinística**
* É totalmente **desacoplado de MonoBehaviours**
* Atua como **fonte de verdade** para:

    * `IStateDependentService`
    * permissões de ação
    * status de atividade da simulação

Ele **não**:

* carrega cenas
* dispara spawn/despawn
* executa reset do mundo

➡️ Essas responsabilidades pertencem ao **Scene Flow + WorldLifecycle**.

---

## Componentes

### IGameLoopService / GameLoopService

* FSM em **C# puro**
* Sem dependência de Unity lifecycle
* Recebe **intenções** via:

    * `RequestStart`
    * `RequestPause`
    * `RequestResume`
    * `RequestReset`
* Exposto como serviço global
* Tickado por um driver externo

```text
UI / Scene Flow / QA
        ↓
GameLoopService (FSM pura)
```

---

### GameLoopDriver

* Responsável por:

    * chamar `Tick(dt)`
* Normalmente atrelado ao `Update()` do Unity
* Pode ser omitido em cenários de tick manual (QA)

---

### GameLoopBootstrap

Responsável por:

* Registrar `IGameLoopService` no DI global
* Inicializar a FSM
* Registrar bridges de entrada (EventBus → GameLoop)

➡️ Não depende de código legado
➡️ Executa no **GlobalBootstrap do NewScripts**

---

## Bridges

### GameLoopEventInputBridge (Entrada)

Bridge responsável por **converter eventos globais em sinais de FSM**.

Características importantes:

* Consome apenas **eventos definitivos**
* Ignora `GameStartEvent` quando o coordinator está ativo
* Evita **start duplo**

Responsabilidades:

| Evento                   | Ação                           |
| ------------------------ | ------------------------------ |
| GamePauseEvent           | `RequestPause / RequestResume` |
| GameResumeRequestedEvent | `RequestResume`                |
| GameResetRequestedEvent  | `RequestReset`                 |

---

### GameLoopSceneFlowCoordinator

Responsável por **sincronizar o start da simulação** com o Scene Flow.

Ele é o **ponto de acoplamento explícito** entre:

```
Intenção de Start
        ↓
Scene Flow
        ↓
WorldLifecycle Reset
        ↓
Liberação do GameLoop
```

---

## Eventos (Contratos)

### Eventos de Intenção

* **GameStartEvent**

    * Significa: *“quero iniciar a simulação”*
    * Nunca é definitivo por si só
    * Quando o coordinator existe:

        * **não inicia diretamente o GameLoop**

---

### Eventos Definitivos (COMMAND)

Eventos que significam **“agora pode”**:

| Evento                   | Semântica                  |
| ------------------------ | -------------------------- |
| GamePauseEvent           | Estado definitivo de pausa |
| GameResumeRequestedEvent | Retomar simulação          |
| GameResetRequestedEvent  | Resetar FSM do GameLoop    |

Esses eventos **não disparam Scene Flow**.

---

## Fluxo Opção B — Start sincronizado com Scene Flow

### Passo a passo

1. UI / sistema publica:

   ```
   GameStartEvent
   ```

2. `GameLoopSceneFlowCoordinator`:

    * recebe o evento
    * executa `ISceneTransitionService.TransitionAsync(startPlan)`
    * aguarda `SceneTransitionScenesReadyEvent`

3. `WorldLifecycleRuntimeDriver`:

    * reage a `ScenesReady`
    * executa reset determinístico

4. Coordinator:

    * considera o sistema “pronto”
    * chama:

      ```
      IGameLoopService.RequestStart()
      ```

5. `GameLoopEventInputBridge`:

    * ignora `GameStartEvent`
    * continua processando pause/resume/reset

---

### Garantias desse fluxo

* Start ocorre **uma única vez**
* Simulação **nunca inicia antes do reset**
* GameLoop **não depende de Scene Flow diretamente**
* Responsabilidades permanecem separadas

---

## Integração com WorldLifecycle e Gates

* GameLoop **não controla gates**
* Ele **informa estado**
* `NewScriptsStateDependentService`:

    * consulta GameLoop
    * consulta SimulationGate
    * decide permissões de ação

➡️ GameLoop = **estado**
➡️ Gate = **condição**
➡️ StateDependent = **decisão**

---

## QA

### GameLoopStateFlowQATester

Arquivo:

```
Infrastructure/QA/GameLoopStateFlowQATester.cs
```

Valida:

* Estado inicial (Boot → Ready)
* Start via Scene Flow
* Start único
* Pause / Resume
* Reset do GameLoop
* Integração com `IStateDependentService`

---

### PlayerMovementLeakSmokeBootstrap

Arquivo:

```
Infrastructure/QA/PlayerMovementLeakSmokeBootstrap.cs
```

Valida:

* Gate de pausa bloqueia `Move`
* Física continua ativa
* Reset limpa input
* Nenhum input fantasma

---

## Relação com ADRs

* GameLoop **não executa reset**
* Ele apenas **reflete o estado da simulação**
* Reset e gates seguem:

    * `ADR-ciclo-de-vida-jogo.md`
    * `WorldLifecycle.md`

---

## Nota Final de Semântica

> O GameLoop **não representa menus de App**
> Ele representa **fases internas da simulação**

Essa distinção é **fundamental** para:

* evitar acoplamento indevido
* permitir múltiplos frontends
* suportar retries, pause, resume e reset de forma previsível

---

### Próximo passo recomendado

1. **Glossário unificado** (Scene, App, Simulation, World, Gameplay, Ready, Playing)
2. Renomear `Menu → Ready` no código **quando fizer sentido**
3. Consolidar o diagrama **App Frontend × Simulation Lifecycle**

Se quiser, seguimos exatamente nessa ordem.
