# Infra Composition Cleanup v2 (IC-1.3, behavior-preserving)

Date: 2026-03-07
Source of truth: local workspace files.

## Scope
- `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs`
- `Infrastructure/Composition/Modules/**` (removed)
- No changes under `Modules/**`.

## Decision path
### PASS - full removal path
- All `*CompositionModule.cs` were trivial (stage gate + delegate call only).
- `IGlobalCompositionModule` and `GlobalCompositionContext` usage was restricted to pipeline + module files.
- Applied direct stage dispatch in `InstallCompositionModules()`.

## Pre-change evidence
Commands:
```text
rg -n "class\s+\w+CompositionModule\b" Infrastructure/Composition/Modules -g "*.cs"
rg -n "IGlobalCompositionModule|GlobalCompositionContext" Infrastructure/Composition -g "*.cs"
```
Summary:
- 9 module classes found in `Infrastructure/Composition/Modules`.
- `IGlobalCompositionModule` and `GlobalCompositionContext` references only in pipeline + module mechanism.

## Code changes
- `GlobalCompositionRoot.Pipeline.cs`
  - removed module mechanism (`IGlobalCompositionModule[]` + `GlobalCompositionContext` + loop).
  - `InstallCompositionModules()` now dispatches directly by `_compositionInstallStage` with `switch`.
  - preserved method names called per stage:
    - `RegisterRuntimePolicyServices`
    - `InstallGatesServices`
    - `InstallGameLoopServices`
    - `InstallSceneFlowServices`
    - `InstallWorldLifecycleServices`
    - `InstallNavigationServices`
    - `RegisterLevelsServices`
    - `InstallContentSwapServices`
    - `InstallDevQaServices`
  - preserved `RegisterEssentialServicesOnly()` call order and stage sequence.
- Removed dead module mechanism files:
  - `Infrastructure/Composition/Modules/**` (including `.meta`)

## Post-change evidence (mandatory)
### Stage install callsites preserved
Command:
```text
rg -n "InstallCompositionModules\(|RegisterRuntimePolicyServices\(|InstallGatesServices\(|InstallGameLoopServices\(|InstallSceneFlowServices\(|InstallWorldLifecycleServices\(|InstallNavigationServices\(|RegisterLevelsServices\(|InstallContentSwapServices\(|InstallDevQaServices\(" Infrastructure/Composition -g "*.cs"
```
Result (relevant):
- `GlobalCompositionRoot.Pipeline.cs` keeps the same stage calls from `RegisterEssentialServicesOnly()`.
- `InstallCompositionModules()` contains a single switch path per stage.

### Old module mechanism removed from active code
Command:
```text
rg -n "Infrastructure\.Composition\.Modules|\w+CompositionModule\b|IGlobalCompositionModule|GlobalCompositionContext" Infrastructure/Composition -g "*.cs"
```
Result:
- `0 matches`

## PASS/FAIL checklist
- [PASS] Order of `RegisterEssentialServicesOnly()` unchanged.
- [PASS] No change to stage names, method names, signatures, or visibility.
- [PASS] No `#if` change in pipeline behavior.
- [PASS] No changes in `Modules/**`.
- [PASS] No new fallback path introduced.

## Notes
- This step is structural only (boilerplate removal in infrastructure composition dispatch).
- Runtime behavior is expected to remain unchanged.
