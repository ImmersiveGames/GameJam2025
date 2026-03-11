# BATCH-CLEANUP-STD-6

Date: 2026-03-10
Source of truth: workspace local (`C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts`)

## Final table

| FilePath | TypeName | MetaGuid | A(CallsitesOutside) | B(AssetRefsOutside) | Decision | Notes |
|---|---|---|---:|---:|---|---|
| `Dev/Core/Logging/DebugLogSettings.cs` | `DebugLogSettings` | `8c6d4ad0c2044d4bb4b6c1a5c07f9d5d` | 0 | 0 | DELETE | Dev-only ScriptableObject with no code callsites outside excluded scopes; companion asset was deleted in the same batch to avoid missing script. |
| `Dev/Core/Logging/DebugManagerConfig.cs` | `DebugManagerConfig` | `e1a960ba9013456db4097f7dae8f25d7` | 0 | 0 | DELETE | Dev-only MonoBehaviour with no non-Dev references. |
| `Dev/Core/Logging/DebugManagerConfig.DevQA.cs` | `DebugManagerConfig` | `1548f079ac6e2d3b` | 0 | 0 | DELETE | Partial DevQA companion of `DebugManagerConfig`; deleted together with the base file. |
| `Editor/Core/Logging/DebugUtility.LoggingPolicyEvidence.Editor.cs` | `DebugUtilityLoggingPolicyEvidenceMenu` | `e4177e0793134d5081f1234cb62b9152` | 0 | 0 | DELETE | Editor-only evidence menu with no external code or asset references. |
| `Editor/Core/Events/EventBusUtil.Editor.cs` | `EventBusUtil` | `12bc6d378f5ae094` | 3 | 0 | KEEP | Partial editor hook for a runtime type still referenced outside Editor. |
| `Editor/Core/Logging/HardFailFastH1.Editor.cs` | `HardFailFastH1` | `b95a32c874ef1d60` | 35 | 0 | KEEP | Partial editor companion for a runtime type with active callsites. |
| `Editor/Infrastructure/Composition/GlobalCompositionRoot.Editor.cs` | `GlobalCompositionRoot` | `d159cb4a2ef86307` | 139 | 0 | KEEP | Partial editor companion for an active runtime type. |

## Deleted
- `Dev/Core/Logging/DebugLogSettings.cs`
- `Dev/Core/Logging/DebugLogSettings.cs.meta`
- `Dev/Core/Logging/DebugLogSettings.asset`
- `Dev/Core/Logging/DebugLogSettings.asset.meta`
- `Dev/Core/Logging/DebugManagerConfig.cs`
- `Dev/Core/Logging/DebugManagerConfig.cs.meta`
- `Dev/Core/Logging/DebugManagerConfig.DevQA.cs`
- `Dev/Core/Logging/DebugManagerConfig.DevQA.cs.meta`
- `Editor/Core/Logging/DebugUtility.LoggingPolicyEvidence.Editor.cs`
- `Editor/Core/Logging/DebugUtility.LoggingPolicyEvidence.Editor.cs.meta`

## Kept
- `Editor/Core/Events/EventBusUtil.Editor.cs`
- `Editor/Core/Logging/HardFailFastH1.Editor.cs`
- `Editor/Infrastructure/Composition/GlobalCompositionRoot.Editor.cs`

## Folder cleanup
- Removed empty folder `Dev/Core/Logging/` and its folder meta.
- `Editor/Core/Logging/` remained because `HardFailFastH1.Editor.cs` is still present.

## Post-checks
```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" Assets/_ImmersiveGames/NewScripts -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
0 matches
```

```text
rg -n "EventBus<\s*GameResetRequestedEvent\s*>\.Register" Assets/_ImmersiveGames/NewScripts -g "*.cs"
.\Modules\Gameplay\Runtime\Actions\States\StateDependentService.cs:232:                EventBus<GameResetRequestedEvent>.Register(_gameResetBinding);
.\Modules\Navigation\Runtime\MacroRestartCoordinator.cs:33:            EventBus<GameResetRequestedEvent>.Register(_resetBinding);
```

```text
rg -n "EventBus<\s*GameExitToMenuRequestedEvent\s*>\.Register" Assets/_ImmersiveGames/NewScripts -g "*.cs"
.\Modules\Navigation\Runtime\ExitToMenuCoordinator.cs:36:            EventBus<GameExitToMenuRequestedEvent>.Register(_exitBinding);
```

```text
rg -n "EventBus<InputModeRequestEvent>\.Register" Assets/_ImmersiveGames/NewScripts -g "*.cs"
.\Modules\InputModes\Runtime\InputModeCoordinator.cs:23:            EventBus<InputModeRequestEvent>.Register(_requestBinding);
```

```text
Tools/Gates/Run-NewScripts-RgGates.ps1
PASS Gate A
PASS Gate A2
PASS Gate B
```

## Outcome
- Behavior-preserving.
- Only orphan Dev/Editor logging tooling was removed.
- Canonical runtime owners and gates remained unchanged.
