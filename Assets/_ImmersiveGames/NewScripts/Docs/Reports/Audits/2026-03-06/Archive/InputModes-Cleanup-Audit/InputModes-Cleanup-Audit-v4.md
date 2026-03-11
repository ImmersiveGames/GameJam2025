# InputModes Cleanup Audit v4

Date: 2026-03-08
Task: IM-1.2c
Status: DONE (DOC-only)

## Objective
- Inventory all IInputModeService callsites that write input mode changes.
- Classify each touchpoint as writer, requestor, reader, or suspect duplicate.
- Define a runtime contract for a future single-writer rail without changing .cs in this step.

## Evidence (summary)
`	ext
rg -n "IInputModeService|TryGetGlobal<IInputModeService>|InputModeService\(" Modules Infrastructure -g "*.cs"
- canonical registration in Infrastructure/Composition/GlobalCompositionRoot.InputModes.cs
- runtime writer resolution in SceneFlowInputModeBridge, PostGameOwnershipService, GameLoopService, ConfirmToStartIntroStageStep
- injected consumers in PauseOverlayController and PostGameOverlayController
- legacy no-op presence in Modules/InputModes/Legacy/Bootstrap/InputModeBootstrap.cs
`

`	ext
rg -n "\.Set|Apply|Switch|Enter|Exit|ActionMap|Gameplay|Frontend|Menu" Modules/InputModes Modules/GameLoop Modules/PostGame Modules/SceneFlow -g "*.cs"
- direct SetFrontendMenu / SetGameplay / SetPauseOverlay writes found in:
  - Modules/InputModes/Interop/SceneFlowInputModeBridge.cs
  - Modules/GameLoop/Pause/Bindings/PauseOverlayController.cs
  - Modules/PostGame/PostGameOwnershipService.cs
  - Modules/PostGame/Bindings/PostGameOverlayController.cs
  - Modules/GameLoop/Runtime/Services/GameLoopService.cs
  - Modules/GameLoop/IntroStage/Runtime/ConfirmToStartIntroStageStep.cs
`

## Callsites inventory
| FilePath | Method | Read/Write | Trigger | Recommendation |
|---|---|---|---|---|
| Modules/InputModes/InputModeService.cs | ApplyMode() | Write | canonical execution of action map switch | KEEP as canonical executor; do not let domain modules bypass it |
| Modules/InputModes/Interop/SceneFlowInputModeBridge.cs | OnTransitionCompleted() | Write | SceneFlow transition completed | CANDIDATE owner/coordinator; strongest current writer because it already owns transition-driven mode sync |
| Modules/GameLoop/Pause/Bindings/PauseOverlayController.cs | ApplyPauseInputMode() / ApplyGameplayInputMode() | Write | Pause overlay local show/hide and return-to-menu | Convert to requestor in IM-1.2d+; should not write directly if single-writer contract is enforced |
| Modules/PostGame/PostGameOwnershipService.cs | ApplyPostGameInputMode() / ApplyExitInputMode() | Write | PostGame enter/exit ownership | Convert to requestor; domain ownership can keep deciding intent, but not write directly |
| Modules/PostGame/Bindings/PostGameOverlayController.cs | ApplyPostGameInputMode() / ApplyGameplayInputMode() | Write | Overlay fallback/local ownership path | DUPLICATE/SUSPECT; likely removable after ownership rail is fully trusted |
| Modules/GameLoop/Runtime/Services/GameLoopService.cs | ApplyGameplayInputMode() | Write | Playing state transition | DUPLICATE/SUSPECT; overlaps with SceneFlow and local UI rails |
| Modules/GameLoop/IntroStage/Runtime/ConfirmToStartIntroStageStep.cs | ApplyUiInputMode() | Write | IntroStage confirmation | Convert to requestor; intent is valid, direct write increases overlap |
| Modules/InputModes/Legacy/Bootstrap/InputModeBootstrap.cs | EnsureInstalled() / EnsureRegistered() | Read | legacy explicit shim | KEEP as legacy no-op only; must not regain runtime writer role |
| Infrastructure/Composition/GlobalCompositionRoot.InputModes.cs | RegisterInputModesFromRuntimeConfig() | Read | boot registration | KEEP as service registration owner only; not part of runtime write contract |

## Classification summary
- Owner/writer today:
  - Modules/InputModes/InputModeService.cs as canonical executor
  - Modules/InputModes/Interop/SceneFlowInputModeBridge.cs as strongest current orchestration candidate
- Legitimate requestors that currently write directly:
  - PauseOverlayController
  - PostGameOwnershipService
  - ConfirmToStartIntroStageStep
- Duplicated or suspect direct writers:
  - PostGameOverlayController
  - GameLoopService
- Readers/incidental:
  - GlobalCompositionRoot.InputModes
  - InputModeBootstrap legacy shim

## Contract proposal
- Runtime target: only one runtime coordinator should call IInputModeService directly.
- Recommended shape:
  - executor remains InputModeService
  - single writer becomes either SceneFlowInputModeBridge evolved into coordinator, or a dedicated InputModeCoordinator
  - all other modules publish/request intent only, e.g. InputModeRequestEvent
- Migration rule:
  - pause, postgame, intro, and gameplay domains keep deciding intent/reason
  - direct SetFrontendMenu / SetGameplay / SetPauseOverlay calls become future refactor targets

## Risks
- PostGameOwnershipService and PostGameOverlayController can both write for the same domain if ownership fallback is misread.
- GameLoopService and SceneFlowInputModeBridge both push gameplay mode, increasing the chance of order-sensitive overlap.
- PauseOverlayController writes locally for UI responsiveness, so moving it later will need care to preserve perceived immediacy.

## IM-1.2d candidates
1. Introduce InputModeRequestEvent and route pause/postgame/intro through it.
2. Decide whether SceneFlowInputModeBridge should be promoted to InputModeCoordinator or split.
3. Remove duplicate fallback writing from PostGameOverlayController after ownership service is confirmed as sole domain authority.
4. Remove gameplay-mode direct write from GameLoopService if SceneFlow/coordinator already covers the same transition.
