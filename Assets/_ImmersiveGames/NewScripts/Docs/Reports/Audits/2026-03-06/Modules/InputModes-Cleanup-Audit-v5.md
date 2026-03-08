# InputModes Cleanup Audit v5
Date: 2026-03-08
Task: IM-1.2d
Status: CODE + DOC, behavior-preserving target

## Objective
Convergir o runtime para single-writer de IInputModeService sem alterar pipeline/stages.

## Writers removed
- Modules/InputModes/Interop/SceneFlowInputModeBridge.cs
- Modules/GameLoop/Pause/Bindings/PauseOverlayController.cs
- Modules/PostGame/PostGameOwnershipService.cs
- Modules/PostGame/Bindings/PostGameOverlayController.cs
- Modules/GameLoop/Runtime/Services/GameLoopService.cs
- Modules/GameLoop/IntroStage/Runtime/ConfirmToStartIntroStageStep.cs

## New canonical rail
- Writer unico: Modules/InputModes/Runtime/InputModeCoordinator.cs
- Request event: Modules/InputModes/Runtime/InputModeRequestEvent.cs
- Registration owner: Infrastructure/Composition/GlobalCompositionRoot.InputModes.cs
- Executor preservado: Modules/InputModes/InputModeService.cs

## Evidence - pre inventory summary
Pre-change inventory (from IM-1.2c / step 0) showed direct write callsites in:
- SceneFlowInputModeBridge
- PauseOverlayController
- PostGameOwnershipService
- PostGameOverlayController
- GameLoopService
- ConfirmToStartIntroStageStep

## Evidence - post rg
### Single writer proof
`rg -n "TryGetGlobal<IInputModeService>|SetFrontendMenu\(|SetGameplay\(|SetPauseOverlay\(" Modules Infrastructure -g "*.cs"`

Result summary:
- Runtime write calls now appear in `Modules/InputModes/Runtime/InputModeCoordinator.cs`
- `GlobalCompositionRoot.InputModes.cs` keeps only registration-time presence checks
- `Legacy/Bootstrap/InputModeBootstrap.cs` keeps only no-op shim/presence check
- No remaining runtime module/bridge writes outside coordinator

### Request rail proof
`rg -n "EventBus<InputModeRequestEvent>\.Raise|new InputModeRequestEvent\(" Modules Infrastructure -g "*.cs"`

Result summary:
- Requestors found in `SceneFlowInputModeBridge`, `PauseOverlayController`, `PostGameOwnershipService`, `PostGameOverlayController`, `GameLoopService`, `ConfirmToStartIntroStageStep`
- Coordinator consumes `EventBus<InputModeRequestEvent>` and applies `IInputModeService`

### Leak sweep
`rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"`

Result: 0 matches.

## Files touched
- Infrastructure/Composition/GlobalCompositionRoot.InputModes.cs
- Modules/InputModes/Runtime/InputModeRequestEvent.cs
- Modules/InputModes/Runtime/InputModeCoordinator.cs
- Modules/GameLoop/Pause/Bindings/PauseOverlayController.cs
- Modules/PostGame/PostGameOwnershipService.cs
- Modules/PostGame/Bindings/PostGameOverlayController.cs
- Modules/GameLoop/Runtime/Services/GameLoopService.cs
- Modules/GameLoop/IntroStage/Runtime/ConfirmToStartIntroStageStep.cs
- Modules/InputModes/Interop/SceneFlowInputModeBridge.cs
- Docs/Modules/InputModes.md
- Docs/Reports/Audits/2026-03-06/Audit-Index.md
- Docs/Reports/Audits/2026-03-06/Module-Audit-Summary.md

## Invariants preserved
- Nenhuma mudanca de ordem do pipeline/stages.
- Contratos publicos de IInputModeService, InputModeService e SceneFlowInputModeBridge preservados.
- Sem UnityEditor fora de Dev/Editor/Legacy/QA.

## Manual checklist
- Menu -> Gameplay
- Confirm IntroStage -> Playing
- Pause -> Resume
- Victory -> PostGame overlay
- Restart -> IntroStage
- ExitToMenu
- Anchors esperados:
  - `[OBS][InputModes] InputModeRequested ...`
  - `[OBS][InputModes] InputModeApplied ...`
  - ausencia de thrash/dedupe inesperado fora das transicoes naturais
