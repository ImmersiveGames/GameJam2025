# Gates-Readiness-StateDependent Module Audit

## A) Scope
- Entra (escopo amplo acordado): `Modules/Gates/**`, `Modules/SceneFlow/Readiness/**`, `Modules/InputModes/**`, `Modules/Gameplay/Runtime/Actions/States/**`.
- EventBus inputs principais: `SceneTransitionStartedEvent`, `SceneTransitionCompletedEvent`, `GameResetRequestedEvent`, eventos de pause/resume/exit.
- Chamadas publicas: `ISimulationGateService`, `IInputModeService`, `IStateDependentService`.
- Unity callbacks: `InputModeBootstrap` e bindings de gameplay.

## B) Outputs
- Gate: bloqueio/liberacao de simulacao por `SimulationGateService`.
- InputMode: aplicacao de modo `Gameplay`/`FrontendMenu` por `SceneFlowInputModeBridge`.
- StateDependent: reage a reset/pause para estado de gameplay.

## C) DI Registration
- `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs`: registra `ISimulationGateService`.
- `Infrastructure/Composition/GlobalCompositionRoot.PauseReadiness.cs`: registra `GamePauseGateBridge`, `GameReadinessService`.
- `Infrastructure/Composition/GlobalCompositionRoot.NavigationInputModes.cs`: registra `IInputModeService`, `SceneFlowInputModeBridge`.
- `Infrastructure/Composition/GlobalCompositionRoot.StateDependentCamera.cs`: registra `IStateDependentService` (`StateDependentService`).

## D) Canonical Runtime Rail
- Canonico: SceneFlow publica `Started/Completed` -> `GameReadinessService` e `SceneFlowInputModeBridge` atualizam gate/input -> gameplay services (incluindo `StateDependentService`) operam no estado final.

## E) LEGACY/Compat
- Nao ha classe marcada explicitamente como bridge desativado neste modulo combinado.
- Persistem caminhos paralelos de dedupe/estado (compat operacional), sem owner unico para assinatura de transicao.

## F) Redundancy Candidates
- `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs` - dedupe de `profile|signature` local; risco medio de divergir do dedupe de readiness/loading.
- `Modules/SceneFlow/Readiness/Runtime/GameReadinessService.cs` - tambem consome `Started/Completed`; risco medio de overlap de responsabilidade com InputModeBridge.
- `Modules/Gameplay/Runtime/Actions/States/StateDependentService.cs` - listener adicional de `GameResetRequestedEvent` alem do owner canonico de restart; risco medio de efeito lateral em restart.

## G) Deletion/Merge Plan (DOC-ONLY)
- Definir owner unico para dedupe de assinatura de transicao e alinhar os demais consumidores.
- Separar claramente: readiness controla gate; input bridge controla mapa/input; state-dependent controla estado de atores.
- Revisar listener de reset em `StateDependentService` para manter apenas o necessario ao dominio local.

| FilePath | Type (Runtime/Editor/Shared) | Public Entry Points | EventBus IN | EventBus OUT | DI Provides | Notes (Canon/Legacy/Dead) |
|---|---|---|---|---|---|---|
| Modules/Gates/SimulationGateService.cs | Runtime | `Acquire`, `Release`, tokens | `GamePauseCommandEvent` (via bridge) | N/A | `ISimulationGateService` | Canon |
| Modules/Gates/Interop/GamePauseGateBridge.cs | Runtime | ctor/Dispose | `GamePauseCommandEvent`, `GameResumeRequestedEvent`, `GameExitToMenuRequestedEvent` | N/A | concrete bridge | Canon |
| Modules/SceneFlow/Readiness/Runtime/GameReadinessService.cs | Runtime | ctor/Dispose | `SceneTransitionStartedEvent`, `SceneTransitionScenesReadyEvent`, `SceneTransitionCompletedEvent` | `ReadinessChangedEvent` | concrete service | Canon |
| Modules/InputModes/InputModeService.cs | Runtime | `SetGameplay`, `SetFrontendMenu` | N/A | N/A | `IInputModeService` | Canon |
| Modules/InputModes/Interop/SceneFlowInputModeBridge.cs | Runtime | ctor/Dispose | `SceneTransitionStartedEvent`, `SceneTransitionCompletedEvent` | N/A | concrete bridge | Canon (dedupe local) |
| Modules/Gameplay/Runtime/Actions/States/StateDependentService.cs | Runtime | state control API | `GameResetRequestedEvent` | N/A | `IStateDependentService` | Canon + overlap candidate |