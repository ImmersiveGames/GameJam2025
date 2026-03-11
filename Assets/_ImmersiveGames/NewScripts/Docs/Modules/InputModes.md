# InputModes

## Status atual (Baseline 3.1)
- Single-writer ativo: `Modules/InputModes/Runtime/InputModeCoordinator.cs`.
- Request rail ativo: `Modules/InputModes/Runtime/InputModeRequestEvent.cs` via `EventBus<InputModeRequestEvent>`.
- Executor canonico: `Modules/InputModes/InputModeService.cs`.
- Owner de registro e gating: `Infrastructure/Composition/GlobalCompositionRoot.InputModes.cs`.
- Bridge de SceneFlow: `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs` agora e requestor; o coordinator consome requests e aplica o write real.
- Requestors atuais no workspace local:
  - `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs`
  - `Modules/GameLoop/Runtime/Services/GameLoopService.cs`
  - `Modules/GameLoop/IntroStage/Runtime/ConfirmToStartIntroStageStep.cs`
  - `Modules/PostGame/PostGameOwnershipService.cs`
  - `Modules/PostGame/Bindings/PostGameOverlayController.cs`
  - `Modules/GameLoop/Pause/Bindings/PauseOverlayController.cs`
- Config source-of-truth: `RuntimeModeConfig.inputModes`; fallback unico em `Modules/InputModes/Runtime/InputModesDefaults.cs`.

## Ownership
| Component | Owner de | Nao-owner de | Anchors |
|---|---|---|---|
| `InputModeCoordinator` | write real em `IInputModeService`; dedupe same-frame por key; warn-once quando o servico nao existe | decidir pipeline/boot; resolver config; executar action maps diretamente | `InputModeRequested`, `InputModeApplied`, `InputModeRequestDeduped` |
| `InputModeService` | execucao concreta de `SetFrontendMenu`, `SetGameplay`, `SetPauseOverlay` e switch de action maps | decidir quem escreve ou quando mudar de modo | `ApplyMode()`, `SwitchCurrentActionMap()` |
| `InputModeRequestEvent` | contrato de intencao runtime (`Kind`, `Reason`, `Source`, `ContextSignature`) | aplicar action maps; resolver DI | `EventBus<InputModeRequestEvent>` |
| `GlobalCompositionRoot.InputModes` | registro de `IInputModeService`; gating do trilho runtime; registro de coordinator/bridge | write de runtime; requests de modo | `RegisterInputModesFromRuntimeConfig()`, `RegisterInputModeSceneFlowBridge()` |
| `SceneFlowInputModeBridge` | requestor de Frontend/Game por transicao; sync de GameLoop por profile | write direto em `IInputModeService` | `SceneFlow/Completed:Gameplay`, `SceneFlow/Completed:Frontend` |
| `PauseOverlayController` | requestor de `PauseOverlay`, `Gameplay`, `FrontendMenu` no trilho de pause | write direto em `IInputModeService` | `PauseOverlay/Show`, `PauseOverlay/Hide`, `PauseOverlay/ReturnToMenuFrontend` |
| `PostGameOwnershipService` | requestor no ownership de PostGame | write direto em `IInputModeService` | `PostGame/Entered`, `PostGame/Exit` |
| `PostGameOverlayController` | requestor do overlay de PostGame | write direto em `IInputModeService` | `PostGame/Show`, `PostGame/RunStarted` |
| `GameLoopService` | requestor de gameplay ao entrar em `Playing` | write direto em `IInputModeService` | `GameLoop/Playing` |
| `ConfirmToStartIntroStageStep` | requestor de menu/frontend na IntroStage | write direto em `IInputModeService` | `IntroStageController/ConfirmToStart` |

## Contrato do rail
### Single-writer invariant
A invariante canonica e: apenas `InputModeCoordinator` chama `SetFrontendMenu`, `SetGameplay` e `SetPauseOverlay` em runtime. Evidencia local: o `rg` de callsites reais (excluindo contrato e implementacao) retorna apenas `Modules/InputModes/Runtime/InputModeCoordinator.cs`.

### Request rail invariant
A invariante canonica do rail e: existe um unico `Register` para `EventBus<InputModeRequestEvent>` e os demais produtores so fazem `Raise(...)`. Evidencia local: o `rg` de register retorna 1 hit em `InputModeCoordinator.cs`, enquanto o `rg` de raises lista apenas os requestors acima.

### Degraded behavior / fail-soft
Se `IInputModeService` nao existir ou `InputModes` estiver disabled/degraded:
- o composition root nao registra `InputModeCoordinator` nem `SceneFlowInputModeBridge` e loga uma vez:
  - `[OBS][InputMode] InputModeCoordinator/Bridge skipped reason='input_modes_disabled_or_not_registered'.`
- se uma request ainda chegar ao coordinator sem servico disponivel, ela vira no-op com warn-once:
  - `[WARN][InputModes] Request ignored; IInputModeService missing ...`
- nao ha `HardFailFastH1` para ausencia do servico nesse caso; o hardening do rail assume fail-soft controlado.

### Observability anchors
Anchors esperados no trilho canonico:
- `[OBS][InputModes] InputModeRequested ... contextSignature='...'`
- `[OBS][InputModes] InputModeApplied ... contextSignature='...'`
- `[OBS][InputModes] InputModeRequestDeduped reason='same_frame' key='...' contextSignature='...'`
- `[OBS][InputMode] InputModeCoordinator/Bridge skipped reason='input_modes_disabled_or_not_registered'.`
- `[WARN][InputModes] Request ignored; IInputModeService missing ... contextSignature='...'`

## Timeline (A-E)
- A. Boot: `GlobalCompositionRoot.Pipeline.cs` chama `RegisterInputModesFromRuntimeConfig()`.
- B. Config: `RuntimeModeConfig.inputModes` resolve enable/disable e nomes de maps; `InputModesDefaults` aplica fallback quando necessario; `IInputModeService` e registrado.
- C. Gating: `GlobalCompositionRoot.InputModes.cs` registra `InputModeCoordinator` e `SceneFlowInputModeBridge` apenas se `IInputModeService` existir.
- D. Runtime requests: SceneFlow, Pause, PostGame, GameLoop e IntroStage publicam `InputModeRequestEvent`.
- E. Apply: `InputModeCoordinator` dedupa same-frame e aplica o modo via `IInputModeService` / `InputModeService`.

## Defaults & Config Source-of-truth
- Fonte de configuracao: `Infrastructure/RuntimeMode/RuntimeModeConfig.cs -> InputModesSettings`.
- Source-of-truth de runtime: `RuntimeModeConfig.inputModes` quando presente.
- Fallback unico: `Modules/InputModes/Runtime/InputModesDefaults.cs`.
  - `PlayerActionMapName = "Player"`
  - `MenuActionMapName = "UI"`
- Regra de manutencao: nao duplicar defaults em services, bridges ou requestors.

## Legacy / Compat
- `Modules/InputModes/Legacy/Bootstrap/InputModeBootstrap.cs` foi removido em `BATCH-CLEANUP-STD-2` por prova de tipo morto (callsite + GUID = 0).
- O bootstrap automatico nao faz parte do trilho canonico e nao deve voltar sem nova evidencia.

## Freeze note (IM-1.2e)
Este modulo esta em trilho canonico. Migracoes futuras devem adicionar requestors, nunca writers diretos de `IInputModeService`.
## BATCH-CLEANUP-STD-2
- Removed in `BATCH-CLEANUP-STD-2`: `Modules/InputModes/Legacy/Bootstrap/InputModeBootstrap.cs`.
- Rationale: shim legacy/no-op sem callsite em `.cs` fora do proprio arquivo e sem referencia por GUID em assets.

