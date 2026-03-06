# Gates-Readiness-StateDependent Cleanup Audit v2 (GRS-1.2)

Date: 2026-03-06
Scope:
- `Modules/Gates/**`
- `Modules/SceneFlow/Readiness/**`
- `Modules/InputModes/**`
- `Modules/Gameplay/Runtime/Actions/States/**`
- read-only callsites in `Infrastructure/**`

Goal:
- inventory de ownership real para `SceneTransitionStartedEvent`, `SceneTransitionCompletedEvent`, `GameResetRequestedEvent`
- hardening mínimo idempotente sem alterar fluxo de runtime

## Code changes (behavior-preserving)
- `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs`
  - dedupe same-frame em `OnTransitionStarted` (signature+frame)
  - novo log: `[OBS][GRS] InputModeBridge dedupe ...`
- `Modules/Gameplay/Runtime/Actions/States/StateDependentService.cs`
  - dedupe same-frame de `GameResetRequestedEvent` (reason+frame)
  - novo log: `[OBS][GRS] StateDependent reset dedupe ...`

Sem mudanças em contratos públicos, eventos, payloads e sem criação de novo fluxo.

## Ownership inventory (real)

### SceneTransitionStartedEvent
- Publisher canônico:
  - `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` (`Raise`)
- Consumers observados:
  - `Modules/SceneFlow/Readiness/Runtime/GameReadinessService.cs`
  - `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs`
  - `Modules/GameLoop/Runtime/Bridges/GameLoopSceneFlowCoordinator.cs`
  - `Modules/SceneFlow/Runtime/SceneFlowSignatureCache.cs`
  - `Modules/SceneFlow/Loading/Runtime/LoadingHudOrchestrator.cs`
  - `Modules/LevelFlow/Dev/LevelFlowDevContextMenu.cs` (dev)

### SceneTransitionCompletedEvent
- Publisher canônico:
  - `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` (`Raise`)
- Consumers observados:
  - `Modules/SceneFlow/Readiness/Runtime/GameReadinessService.cs`
  - `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs`
  - `Modules/GameLoop/Runtime/Bridges/GameLoopSceneFlowCoordinator.cs`
  - `Modules/SceneFlow/Runtime/SceneFlowSignatureCache.cs`
  - `Modules/SceneFlow/Loading/Runtime/LoadingHudOrchestrator.cs`
  - `Modules/LevelFlow/Runtime/LevelStageOrchestrator.cs`

### GameResetRequestedEvent
- Publisher:
  - `Modules/GameLoop/Commands/GameCommands.cs` (`Raise`)
- Consumers observados:
  - owner de restart canônico: `Modules/Navigation/Runtime/MacroRestartCoordinator.cs`
  - consumer auxiliar: `Modules/Gameplay/Runtime/Actions/States/StateDependentService.cs`

## Static evidence (rg)

### 1) Eventos e callsites
Command:
```powershell
rg -n "EventBus<SceneTransitionStartedEvent>|EventBus<SceneTransitionCompletedEvent>|EventBus<GameResetRequestedEvent>" Modules Infrastructure
```
Relevant lines:
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:157` (`SceneTransitionStartedEvent.Raise`)
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:185` (`SceneTransitionCompletedEvent.Raise`)
- `Modules/GameLoop/Commands/GameCommands.cs:72` (`GameResetRequestedEvent.Raise`)
- `Modules/Navigation/Runtime/MacroRestartCoordinator.cs:33` (`GameResetRequestedEvent.Register`)
- `Modules/Gameplay/Runtime/Actions/States/StateDependentService.cs:230` (`GameResetRequestedEvent.Register`)

### 2) Register(new EventBinding<...>) check
Command:
```powershell
rg -n "Register\(new EventBinding<SceneTransitionStartedEvent>|Register\(new EventBinding<SceneTransitionCompletedEvent>|Register\(new EventBinding<GameResetRequestedEvent>" Modules Infrastructure
```
Result:
- `(no inline Register(new EventBinding<...>) callsites)`
- Registros são feitos via variáveis de binding (pattern atual do código).

### 3) Classes-alvo
Command:
```powershell
rg -n "class\s+SceneFlowInputModeBridge|class\s+GameReadinessService|class\s+StateDependentService" Modules
```
Result:
- `SceneFlowInputModeBridge`, `GameReadinessService`, `StateDependentService` encontrados como esperado.

### 4) Tokens de gate
Command:
```powershell
rg -n "Acquire\(|Release\(|flow\.scene_transition|sim\.gameplay" Modules
```
Relevant lines:
- `Modules/SceneFlow/Readiness/Runtime/GameReadinessService.cs:163` (`Acquire(SimulationGateTokens.SceneTransition)`)
- `Modules/Gates/SimulationGateTokens.cs:19` (`flow.scene_transition`)
- `Modules/Gates/SimulationGateTokens.cs:28` (`sim.gameplay`)

## No second owner accidentally introduced
- Nenhum novo `Register` para os três eventos foi adicionado em novos arquivos.
- `GameReadinessService` segue owner de readiness/gate token de transição.
- `SceneFlowInputModeBridge` segue restrito a InputMode/GameLoop sync.
- `StateDependentService` continua consumer auxiliar de reset (agora idempotente no mesmo frame).

## Residual risk
- `StateDependentService` ainda é consumer auxiliar de `GameResetRequestedEvent`; não executa restart, mas mantém superfície de reação de estado.
- Consumidores cross-módulo de `SceneTransition*` continuam múltiplos por desenho (observabilidade/hud/coordenadores), sem consolidação estrutural nesta etapa.

## Behavior-preserving assertions
- Sem alteração de ordem de pipeline.
- Sem alteração de eventos/contratos/payloads.
- Sem remoção de consumers canônicos.
- Apenas dedupe idempotente local com logs `[OBS][GRS]`.
