# GameLoop (NewScripts)

## Escopo e Responsabilidade
O **GameLoop** define o **estado macro da simulação** dentro de uma **GameplayScene**. Ele **não representa**: navegação de App, menus de frontend, telas de splash ou UX. Esses pertencem ao **App Frontend / Scene Flow**.

### O que o GameLoop controla
- Ativação ou bloqueio da **simulação** (estado macro).
- Sinalização de “atividade” (ex.: `IsGameActive`) consumida por sistemas externos.
- Regras de **capabilidade por estado** (helper) para ações (ex.: `IsActionAllowedByLoopState`).
- Fonte de verdade de estado macro para integração com `IStateDependentService`.

➡️ O GameLoop **não carrega semântica de UI**, apenas **estado operacional da simulação**.

---

## Estados do GameLoop (Semântica Corrigida)
Os estados **não representam telas**, mas **fases da simulação**:

| Estado   | Significado |
|---------|-------------|
| **Boot**  | Simulação ainda não inicializada (pré-reset / pré-start) |
| **Ready** *(antes “Menu”)* | Simulação construída, mas **não ativa** (idle/ready) |
| **Playing** | Simulação ativa |
| **Paused**  | Simulação pausada (ações bloqueadas via gate) |

> Nota: o nome `Menu` era legado conceitual. No NewScripts, ele representa **“Simulation Ready / Idle”**, não um menu visual.

---

## Objetivo
O GameLoop:
- mantém uma FSM **determinística**;
- é **desacoplado de MonoBehaviours**;
- atua como **fonte de verdade** do estado macro e do status de atividade da simulação.

O GameLoop **não**:
- carrega cenas;
- dispara spawn/despawn;
- executa reset do mundo;
- consulta Gate/Readiness diretamente.

---

## Componentes e papéis

### `GameLoopStateMachine`
- FSM de estado macro (Boot/Ready/Playing/Paused).
- Processa sinais **transientes** (start/pause/resume/reset).
- Permite **encadear transições no mesmo tick** (ex.: Boot → Ready → Playing após um único start), com guarda `MaxTransitionsPerTick`.

### `GameLoopService`
- Fachada do GameLoop exposta via DI (`IGameLoopService`).
- Recebe requests (`RequestStart/Pause/Resume/Reset`), seta sinais e executa `Update()` por tick.
- Faz `ResetTransientSignals()` ao final do tick para garantir determinismo.

### `GameLoopSceneFlowCoordinator`
- Coordena start “de produção” via **Scene Flow + WorldLifecycle**.
- Converte intenção (REQUEST) em execução (COMMAND) somente quando o runtime está “ready”.

### `GameLoopEventInputBridge`
- Bridge de **eventos definitivos** (EventBus → `IGameLoopService`).
- **Não consome start**.
- Consome apenas pause/resume/reset.

---

## Eventos: REQUEST vs COMMAND (contrato oficial)

### REQUEST (intenção)
- `GameStartRequestedEvent` é **REQUEST**: sinaliza “quero iniciar”.
- (Opcional/legado) `GameStartCommandEvent` pode existir como **alias legado**, mas é tratado como REQUEST.
  Recomendação: **não usar** em código novo.

> REQUEST não garante execução imediata. Ele apenas inicia a coordenação do fluxo.

### COMMAND (execução definitiva)
- O **COMMAND** de start do GameLoop **não é um evento**.
- O start definitivo ocorre quando o Coordinator chama:
  - `IGameLoopService.Initialize()` (idempotente, opcional como proteção), e
  - `IGameLoopService.RequestStart()`
  após:
  - `SceneTransitionCompletedEvent` **e**
  - `WorldLifecycleResetCompletedEvent` (ou SKIP).

---

## Gate/Readiness e determinismo

### Separação de responsabilidades
- O GameLoop **não é gate-aware** (não consulta `ISimulationGateService` ou `GameReadinessService`).
- A autorização final de ações (gate + readiness + estado) é responsabilidade do:
  - `IStateDependentService` (gate-aware).

### `IsActionAllowedByLoopState`
- É um **helper** (capability map) por estado macro.
- **Não é** autorização final de gameplay.
- Use-o para filtros iniciais/telemetria/UI, mas **sempre** valide com `IStateDependentService` antes de executar ações no gameplay.

---

## Boas práticas e princípios SOLID
- **SRP**:
  - FSM (GameLoopStateMachine) gerencia estados;
  - Coordinator sincroniza com SceneFlow/WorldLifecycle;
  - Bridge converte eventos definitivos.
- **DIP**: dependências via DI e interfaces (`IGameLoopService`).
- **Determinismo**: sinais transientes + encadeamento com guarda (`MaxTransitionsPerTick`) + reset por tick.
- **Idempotência**: `Initialize()` e `Dispose()` devem ser seguras para chamadas repetidas.
- **Sem UI coupling**: use observers (`IGameLoopStateObserver`) para reagir a estados, não para controlar fluxo de start.

---

## Dicas de uso

### Registro
- Use `GameLoopBootstrap.Ensure()` no bootstrap global para registrar:
  - `IGameLoopService`
  - `GameLoopEventInputBridge`
  - (se aplicável) runtime driver/coordinator

### Start (produção)
- Emita **REQUEST**:
  - `EventBus<GameStartRequestedEvent>.Raise(new GameStartRequestedEvent());`
- O Coordinator faz:
  - `TransitionAsync(startPlan)`
  - aguarda `TransitionCompleted` + `WorldLifecycleResetCompleted`
  - chama `RequestStart()`.

### Pausa
- Use `GamePauseCommandEvent(isPaused: true/false)`; o bridge traduz para `RequestPause/RequestResume`.

### Reset
- Use `GameResetRequestedEvent`; o bridge traduz para `RequestReset()`.

---

## Exemplos de uso

### Inicialização (bootstrap)
```csharp
using UnityEngine;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;

public class MyBootstrap : MonoBehaviour
{
    private void Awake()
    {
        GameLoopBootstrap.Ensure();
    }
}
````

### REQUEST de start via evento (UI/QA)

```csharp
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;

EventBus<GameStartRequestedEvent>.Raise(new GameStartRequestedEvent());
```

### Observer custom (reagir a estados)

```csharp
public class MyObserver : IGameLoopStateObserver
{
    public void OnStateEntered(GameLoopStateId stateId, bool isActive) { /* log/UI */ }
    public void OnStateExited(GameLoopStateId stateId) { /* cleanup */ }
    public void OnGameActivityChanged(bool isActive) { /* habilitar simulação */ }
}

// Exemplo: injetar no construtor da FSM
// new GameLoopStateMachine(signals, new MyObserver());
```

### Integração com gate (externa)

* Não acople gate na FSM.
* Use `IStateDependentService.CanPerform(action)` para checar:

    * gate
    * readiness
    * estado do GameLoop (macro)
    * e outros critérios centralizados.

---

## Anti-patterns (evitar)

* Tratar `IsActionAllowedByLoopState(...)` como autorização final de gameplay.
* Chamar `IGameLoopService.RequestStart()` diretamente no REQUEST (pular Coordinator) em produção.
* Suportar transições concorrentes esperando determinismo sem correlação extra (o runtime assume 1 transição em voo).

---

## Atualizações (2025-12-25)

* **Renomeações aplicadas**:

    * Estado `Menu` → `Ready` (semântica “Simulation Ready/Idle”).
    * `CanPerform` → `IsActionAllowedByLoopState` (deixa explícito que é helper por estado, não gate-aware).
    * Start: `GameStartRequestedEvent` como REQUEST (preferencial); `GameStartCommandEvent` apenas como alias legado (tratado como REQUEST).
    * Pause: `GamePauseEvent` → `GamePauseCommandEvent` (evento definitivo).
* **Contrato consolidado**:

    * Start definitivo é **COMMAND via chamada** `RequestStart()` pelo Coordinator quando “ready”.
    * Gate/Readiness seguem fora do GameLoop; enforcement final é `IStateDependentService`.
