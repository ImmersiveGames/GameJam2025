# DevQA Guard Governance Audit v2 (DQ-1.7)

Date: 2026-03-07  
Source of truth: local workspace (`C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts`).

## Behavior-preserving statement
- No public contract changes.
- No runtime pipeline order changes.
- Release behavior-preserving.
- DevBuild/Editor QA harness remains installed via `GlobalCompositionRoot.DevQA`.

## Scope
- Remove the parallel Dev bootstrap attribute from `Modules/ContentSwap/Dev/Runtime/ContentSwapDevBootstrapper.cs`.
- Keep the canonical ContentSwap DevQA owner in `Infrastructure/Composition/GlobalCompositionRoot.DevQA.cs`.

## Before
### RuntimeInitializeOnLoadMethod
Command:
`rg -n "RuntimeInitializeOnLoadMethod" . -g "*.cs"`

Result:
- `Core/Logging/DebugUtility.cs:51`
- `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs:61`
- `Modules/ContentSwap/Dev/Runtime/ContentSwapDevBootstrapper.cs:9`

### Canonical ContentSwap DevQA owner
Command:
`rg -n "ContentSwapDevInstaller\.EnsureInstalled\(|ContentSwapDevContextMenu|ContentSwapDevInstaller" . -g "*.cs"`

Relevant result:
- `Infrastructure/Composition/GlobalCompositionRoot.DevQA.cs:32` -> `ContentSwapDevInstaller.EnsureInstalled();`

## Applied change
- Removed the automatic `RuntimeInitializeOnLoadMethod` bootstrap path from `Modules/ContentSwap/Dev/Runtime/ContentSwapDevBootstrapper.cs`.
- Kept the file under `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- Added explicit idempotent legacy API:
  - `ContentSwapDevBootstrapper.EnsureInstalled()`
  - emits `[OBS][LEGACY][DevQA] ContentSwapDevBootstrapper.EnsureInstalled called; canonical owner is GlobalCompositionRoot.DevQA.`
  - delegates to `ContentSwapDevInstaller.EnsureInstalled()`
- Canonical owner remained unchanged:
  - `GlobalCompositionRoot.DevQA` -> `RegisterContentSwapQaInstaller()` -> `ContentSwapDevInstaller.EnsureInstalled()`

## Files touched
- Modified:
  - `Modules/ContentSwap/Dev/Runtime/ContentSwapDevBootstrapper.cs`
  - `Docs/Modules/DevQA.md`
- Added:
  - `Docs/Reports/Audits/2026-03-06/Modules/DevQA-Guard-Governance-Audit-v4.md`
  - `Docs/Reports/Audits/2026-03-06/Modules/DevQA-Guard-Governance-Audit-v4.md.meta`

## After
### RuntimeInitializeOnLoadMethod
Command:
`rg -n "RuntimeInitializeOnLoadMethod" . -g "*.cs"`

Result:
- `Core/Logging/DebugUtility.cs:51`
- `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs:61`

### ContentSwap Dev guards
Command:
`rg -n "#if\s+UNITY_EDITOR\s*\|\|\s*DEVELOPMENT_BUILD|using UnityEditor" .\Modules\ContentSwap\Dev -g "*.cs"`

Expected result:
- Dev files remain under `#if UNITY_EDITOR || DEVELOPMENT_BUILD`
- `using UnityEditor` stays only where needed in Dev bindings

### Leak safety
Command:
`rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"`

Expected result:
- `0 matches`

## Conclusion
- The parallel ContentSwap Dev bootstrap path was removed.
- DevQA installation remains canonical via composition only.
- Release behavior-preserving; DevBuild/Editor QA harness remains via `GlobalCompositionRoot.DevQA`.
