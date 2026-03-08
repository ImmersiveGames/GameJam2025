# InputModes Cleanup Audit v3

Date: 2026-03-08
Task: IM-1.2b
Status: DONE (behavior-preserving)

## Objective
- Consolidate ActionMap defaults into a single canonical helper.
- Keep RuntimeModeConfig.inputModes as runtime source-of-truth for configured names.
- Preserve contracts and composition pipeline.

## Files touched
- Modules/InputModes/Runtime/InputModesDefaults.cs
- Infrastructure/Composition/GlobalCompositionRoot.InputModes.cs
- Modules/InputModes/InputModeService.cs
- Modules/InputModes/Interop/SceneFlowInputModeBridge.cs
- Infrastructure/RuntimeMode/RuntimeModeConfig.cs
- Docs/Modules/InputModes.md
- Docs/Reports/Audits/2026-03-06/Audit-Index.md
- Docs/Reports/Audits/2026-03-06/Module-Audit-Summary.md

## Pre-change evidence (summary)
`	ext
rg -n '"Player"|"UI"|playerActionMapName|menuActionMapName' Modules/InputModes Infrastructure/Composition Infrastructure/RuntimeMode Modules/GameLoop Modules/PostGame Modules/SceneFlow -g '*.cs'
- fallback/default literals found in GlobalCompositionRoot.InputModes.cs and InputModeService.cs
- RuntimeModeConfig.InputModesSettings serialized the same defaults directly
- SceneFlowInputModeBridge still logged map names with hardcoded literals
`

`	ext
rg -n 'string\.IsNullOrWhiteSpace\(.*MapName|\?\?\s*"Player"|\?\?\s*"UI"' Modules/InputModes Infrastructure -g '*.cs'
Modules/InputModes/InputModeService.cs
Infrastructure/Composition/GlobalCompositionRoot.InputModes.cs
`

## Implementation
- Added canonical helper Modules/InputModes/Runtime/InputModesDefaults.cs.
- GlobalCompositionRoot.InputModes.cs now resolves final names through InputModesDefaults.ResolveFrom(config).
- InputModeService now normalizes via InputModesDefaults.NormalizeOrDefault(...) instead of private duplicated constants.
- SceneFlowInputModeBridge no longer carries raw "Player" / "UI" literals; logs read from canonical constants.
- RuntimeModeConfig.InputModesSettings now references canonical constants for serialized defaults.
- Optional one-shot observability added in composition root:
  - [OBS][InputMode] ActionMapDefaultsApplied reason='blank_config' ...

## Post-checks (summary)
`	ext
rg -n 'RegisterInputModesFromRuntimeConfig|InputModesDefaults|playerActionMapName|menuActionMapName' Infrastructure/Composition/GlobalCompositionRoot.InputModes.cs Modules/InputModes/Runtime/InputModesDefaults.cs Infrastructure/RuntimeMode/RuntimeModeConfig.cs -g '*.cs'
- GlobalCompositionRoot.InputModes.cs resolves via InputModesDefaults.ResolveFrom(config)
- RuntimeModeConfig points to InputModesDefaults.PlayerActionMapName / MenuActionMapName
`

`	ext
rg -n 'string\.IsNullOrWhiteSpace\(.*MapName|\?\?\s*"Player"|\?\?\s*"UI"' Modules/InputModes Infrastructure -g '*.cs'
Infrastructure/Composition/GlobalCompositionRoot.InputModes.cs: only blank-config detection for observability
(no direct fallback literals remain)
`

`	ext
rg -n '"Player"|"UI"' Infrastructure/Composition/GlobalCompositionRoot.InputModes.cs
0 matches
`

`	ext
raw literal check
- canonical raw literals remain in Modules/InputModes/Runtime/InputModesDefaults.cs
- RuntimeModeConfig now references helper constants instead of duplicating literals
- SceneFlowInputModeBridge no longer contains raw literals; it references InputModesDefaults constants for observability only
`

`	ext
rg -n 'UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu' . -g '*.cs' -g '!**/Dev/**' -g '!**/Editor/**' -g '!**/Legacy/**' -g '!**/QA/**'
0 matches
`

## Invariants preserved
- No public API changed in IInputModeService, InputModeService, or SceneFlowInputModeBridge.
- No change in GlobalCompositionRoot.Pipeline.cs.
- No new Editor API leak outside Dev/** or Editor/**.

## Manual validation pending
- Enter Gameplay -> Pause -> Resume -> PostGame -> Menu.
- Confirm no behavioral regression in action map switching.
