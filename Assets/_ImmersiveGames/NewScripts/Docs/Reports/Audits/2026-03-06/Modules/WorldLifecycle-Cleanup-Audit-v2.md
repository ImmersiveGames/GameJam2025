# WL-1.2 - WorldLifecycle publishers consolidation audit v2 (behavior-preserving)

Date: 2026-03-06
Source of truth: local workspace files.

## Scope
- `Modules/WorldLifecycle/WorldRearm/Application/WorldResetOrchestrator.cs`
- `Modules/WorldLifecycle/WorldRearm/Application/WorldResetService.cs`
- `Modules/WorldLifecycle/Runtime/WorldLifecycleSceneFlowResetDriver.cs`
- `Modules/WorldLifecycle/Runtime/WorldResetCommands.cs`
- `Modules/WorldLifecycle/Runtime/WorldLifecycleOrchestrator.cs`
- `Modules/WorldLifecycle/Bindings/WorldLifecycleController.cs`
- `Modules/WorldLifecycle/Runtime/WorldLifecycleResetCompletionGate.cs` (consumer check)

## A) Mandatory inventory (before changes)

### A1) V1 publishers
Command:
```text
rg -n "EventBus<WorldLifecycleResetStartedEvent>\.Raise|EventBus<WorldLifecycleResetCompletedEvent>\.Raise" Modules/WorldLifecycle Modules/SceneFlow Infrastructure -g "*.cs"
```
Relevant matches (before):
- `Modules/WorldLifecycle/WorldRearm/Application/WorldResetOrchestrator.cs:156` started V1
- `Modules/WorldLifecycle/WorldRearm/Application/WorldResetOrchestrator.cs:168` completed V1
- `Modules/WorldLifecycle/WorldRearm/Application/WorldResetService.cs:75` completed V1 fallback
- `Modules/WorldLifecycle/Runtime/WorldLifecycleSceneFlowResetDriver.cs:262` completed V1 fallback/SKIP

### A2) V2 publishers
Command:
```text
rg -n "EventBus<WorldLifecycleResetRequestedV2Event>\.Raise|EventBus<WorldLifecycleResetCompletedV2Event>\.Raise" Modules/WorldLifecycle Modules/SceneFlow Infrastructure -g "*.cs"
```
Relevant matches (before):
- `Modules/WorldLifecycle/Runtime/WorldResetCommands.cs:153`
- `Modules/WorldLifecycle/Runtime/WorldResetCommands.cs:178`

### A3) V1 consumers (gate)
Command:
```text
rg -n "Register\(.*WorldLifecycleResetCompletedEvent|WorldLifecycleResetCompletionGate" Modules/SceneFlow Modules/WorldLifecycle -g "*.cs"
```
Relevant matches:
- `Modules/WorldLifecycle/Runtime/WorldLifecycleResetCompletionGate.cs` (register/consume V1 completed)

## B) Applied refactor (WL-1.2)
- Added explicit V1 publish helper methods in `WorldResetOrchestrator`:
  - `PublishResetStartedV1(string contextSignature, string reason)`
  - `PublishResetCompletedV1(string contextSignature, string reason)`
- Replaced direct V1 `EventBus.Raise(...)` in fallback paths:
  - `WorldResetService` catch fallback now calls `WorldResetOrchestrator.PublishResetCompletedV1(...)`.
  - `WorldLifecycleSceneFlowResetDriver` SKIP/fallback now calls `WorldResetOrchestrator.PublishResetCompletedV1(...)`.
- Added observability line in driver fallback path:
  - `[OBS][WorldLifecycle] V1FallbackPublish ...`
- Kept V2 publishes exclusively in `WorldResetCommands`.
- Added short boundary comments:
  - `WorldResetOrchestrator`: owner pipeline macro + V1 publish
  - `WorldLifecycleController`/`WorldLifecycleOrchestrator`: local/scoped reset rail
  - `WorldResetCommands`: owner V2 command/telemetry publish

## C) Mandatory post-change evidence

### C1) V1 Raise appears only in WorldResetOrchestrator helper
Command:
```text
rg -n "EventBus<WorldLifecycleResetStartedEvent>\.Raise|EventBus<WorldLifecycleResetCompletedEvent>\.Raise" Modules/WorldLifecycle Modules/SceneFlow Infrastructure -g "*.cs"
```
Result:
- `Modules/WorldLifecycle/WorldRearm/Application/WorldResetOrchestrator.cs:154`
- `Modules/WorldLifecycle/WorldRearm/Application/WorldResetOrchestrator.cs:160`

### C2) V2 Raise appears only in WorldResetCommands
Command:
```text
rg -n "EventBus<WorldLifecycleResetRequestedV2Event>\.Raise|EventBus<WorldLifecycleResetCompletedV2Event>\.Raise" Modules/WorldLifecycle Modules/SceneFlow Infrastructure -g "*.cs"
```
Result:
- `Modules/WorldLifecycle/Runtime/WorldResetCommands.cs:155`
- `Modules/WorldLifecycle/Runtime/WorldResetCommands.cs:180`

### C3) Fallback routing to V1 helper
Command:
```text
rg -n "PublishResetCompletedV1|PublishResetStartedV1|V1FallbackPublish" Modules/WorldLifecycle -g "*.cs"
```
Relevant matches:
- `WorldResetOrchestrator.cs:152,158,170,181`
- `WorldResetService.cs:74`
- `WorldLifecycleSceneFlowResetDriver.cs:263,266`

## D) Publishers/Consumers/Owners table (post-change)

| Group | Event | Publisher | Consumer | Owner |
|---|---|---|---|---|
| V1 gate | `WorldLifecycleResetStartedEvent` | `WorldResetOrchestrator.PublishResetStartedV1` | (none in current scope) | `WorldResetOrchestrator` |
| V1 gate | `WorldLifecycleResetCompletedEvent` | `WorldResetOrchestrator.PublishResetCompletedV1` (including fallback callers) | `WorldLifecycleResetCompletionGate` (+ GameLoop coordinator external) | `WorldResetOrchestrator` |
| V2 commands/telemetry | `WorldLifecycleResetRequestedV2Event` | `WorldResetCommands` | no `Register` in audited scope | `WorldResetCommands` |
| V2 commands/telemetry | `WorldLifecycleResetCompletedV2Event` | `WorldResetCommands` | no `Register` in audited scope | `WorldResetCommands` |

## E) Behavior-preserving note
- No public contracts/payloads changed.
- No pipeline order, reset gating tokens, or reset policy changed.
- Existing anchor logs preserved; only `[OBS]` fallback routing log added.

## F) Manual checklist (to run in Editor/Dev Build)
- Smoke A-E + `MacroRestart` + `NextLevel` + `RestartCurrentLevelLocal` + `Victory->Restart`.
- Capture `lastlog.log` and attach to this audit as runtime evidence.
