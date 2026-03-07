# DevQA Leak Sweep Audit v2 (DQ-1.5)

Date: 2026-03-07  
Source of truth: local workspace (`C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts`).

## Behavior-preserving statement
- No public contract changes.
- No pipeline order changes in `GlobalCompositionRoot`.
- Runtime behavior preserved; only Editor/DevQA isolation and folder relocation were applied.

## Before evidence
Command:
`rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu|InitializeOnLoadMethod" C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**"`

Relevant matches:
- `QA/LevelFlow/NTo1/LevelFlowNTo1QaEditor.cs` (UnityEditor/MenuItem/AssetDatabase)
- `QA/LevelFlow/Compat/ScenarioB/LevelFlowCompatScenarioBEditor.cs` (UnityEditor/MenuItem/AssetDatabase/FindAssets/EditorApplication)
- `Core/Logging/HardFailFastH1.cs:24` (`UnityEditor.EditorApplication`)
- `Core/Events/EventBusUtil.cs` (`using UnityEditor`, `InitializeOnLoadMethod`, `EditorApplication.playModeStateChanged`)
- `Core/Logging/DebugManagerConfig.cs` (`ContextMenu`)
- `Infrastructure/Composition/GlobalCompositionRoot.BootstrapConfig.cs:25` (`UnityEditor.EditorApplication`)
- `Infrastructure/Composition/GlobalCompositionRoot.SceneFlow.cs` (`using UnityEditor`, `UnityEditor.EditorApplication`)

Inventory only (allowed runtime init):
`rg -n "RuntimeInitializeOnLoadMethod" C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**"`
- `Core/Logging/DebugUtility.cs:51`
- `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs:61`

## Applied fixes
- Moved QA tooling folder:
  - `NewScripts/QA/**` -> `NewScripts/Editor/QA/**` (including `.meta`).
- `Core/Logging/HardFailFastH1.cs`
  - converted to `partial` runtime class;
  - removed direct UnityEditor usage;
  - added Editor implementation in `Editor/Core/Logging/HardFailFastH1.Editor.cs`.
- `Core/Events/EventBusUtil.cs`
  - converted to `partial` runtime class;
  - moved `InitializeOnLoadMethod` + `EditorApplication.playModeStateChanged` to `Editor/Core/Events/EventBusUtil.Editor.cs`.
- `Core/Logging/DebugManagerConfig.cs`
  - converted to `partial`;
  - removed `ContextMenu` attributes from runtime file;
  - added DevQA partial with context menus in `Dev/Core/Logging/DebugManagerConfig.DevQA.cs`.
- `Infrastructure/Composition/GlobalCompositionRoot.BootstrapConfig.cs`
  - removed direct `UnityEditor.EditorApplication` call;
  - now uses centralized `StopPlayModeOrQuit()` + editor partial hook.
- `Infrastructure/Composition/GlobalCompositionRoot.SceneFlow.cs`
  - removed `using UnityEditor` and editor APIs from runtime file;
  - moved editor build-settings resolution and playmode stop hook to `Editor/Infrastructure/Composition/GlobalCompositionRoot.Editor.cs`.

## After evidence
Strict leak tokens (excluding Dev/Editor/Legacy/QA):
`rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"`
- Result: `0 matches`.

Requested command with `InitializeOnLoadMethod`:
`rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu|InitializeOnLoadMethod" C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"`
- Matches are only:
  - `Core/Logging/DebugUtility.cs:51` (`RuntimeInitializeOnLoadMethod`)
  - `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs:61` (`RuntimeInitializeOnLoadMethod`)
- Note: this is expected by design, and aligns with DQ-1.5 rule that `RuntimeInitializeOnLoadMethod` is allowed.
