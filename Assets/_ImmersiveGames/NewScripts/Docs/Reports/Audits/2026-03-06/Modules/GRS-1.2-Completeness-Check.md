# GRS-1.2 Completeness Check (workspace local)

Date: 2026-03-06
Scope checked:
- `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs`
- `Modules/Gameplay/Runtime/Actions/States/StateDependentService.cs`
- ownership inventory via `Modules/**` + `Infrastructure/**`

Source of truth:
- Workspace local files only.

## Criteria

### 1) SceneFlowInputModeBridge same-frame dedupe (signature + frame)
- Status: OK
- Required:
  - same-frame idempotency for `SceneTransitionStartedEvent`
  - log `[OBS][GRS] InputModeBridge dedupe ...`

Evidence (`rg`):
```text
Modules/InputModes/Interop/SceneFlowInputModeBridge.cs:32 _lastStartedSignature
Modules/InputModes/Interop/SceneFlowInputModeBridge.cs:33 _lastStartedFrame
Modules/InputModes/Interop/SceneFlowInputModeBridge.cs:60 Time.frameCount
Modules/InputModes/Interop/SceneFlowInputModeBridge.cs:66 [OBS][GRS] InputModeBridge dedupe event='SceneTransitionStarted' ...
Modules/InputModes/Interop/SceneFlowInputModeBridge.cs:39 EventBus<SceneTransitionStartedEvent>.Register(...)
```

### 2) StateDependentService same-frame reset dedupe (reason + frame)
- Status: OK
- Required:
  - same-frame idempotency for `GameResetRequestedEvent`
  - log `[OBS][GRS] StateDependent reset dedupe ...`
  - remains auxiliary consumer (no restart ownership)

Evidence (`rg`):
```text
Modules/Gameplay/Runtime/Actions/States/StateDependentService.cs:70 _lastResetFrame
Modules/Gameplay/Runtime/Actions/States/StateDependentService.cs:71 _lastResetReason
Modules/Gameplay/Runtime/Actions/States/StateDependentService.cs:217 Time.frameCount
Modules/Gameplay/Runtime/Actions/States/StateDependentService.cs:221 [OBS][GRS] StateDependent reset dedupe ...
Modules/Gameplay/Runtime/Actions/States/StateDependentService.cs:245 EventBus<GameResetRequestedEvent>.Register(...)
```

### 3) Ownership inventory check (no accidental second owner)
- Status: OK

Evidence (`rg`):
```text
Publisher reset:
Modules/GameLoop/Commands/GameCommands.cs:72 EventBus<GameResetRequestedEvent>.Raise(...)

Owner restart canônico:
Modules/Navigation/Runtime/MacroRestartCoordinator.cs:33 EventBus<GameResetRequestedEvent>.Register(...)

Consumer auxiliar:
Modules/Gameplay/Runtime/Actions/States/StateDependentService.cs:245 EventBus<GameResetRequestedEvent>.Register(...)

Transition publishers:
Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:157 Started.Raise
Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:185 Completed.Raise
```

## Runtime log note
- NOT TRIGGERED by this run (requires duplicate same-frame path).
- Static evidence confirms presence in code.

## Result
- GRS-1.2 completeness: PASS (static).
- Code patch reapplied: not required.
