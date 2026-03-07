# DevQA Guard Governance Audit v3 (DQ-1.8)

Date: 2026-03-07  
Source of truth: local workspace (`C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts`).

## Behavior-preserving statement
- No public contract changes.
- No runtime pipeline changes.
- Release behavior-preserving.
- DevBuild/Editor QA harness remains via `GlobalCompositionRoot.DevQA`.

## Before evidence
### Shim callsites
Command:
`rg -n "ContentSwapDevBootstrapper\.EnsureInstalled\(" . -g "*.cs"`

Result:
- `0 matches`

### Canonical owner
Command:
`rg -n "ContentSwapDevInstaller\.EnsureInstalled\(" .\Infrastructure\Composition -g "*.cs"`

Result:
- `Infrastructure/Composition/GlobalCompositionRoot.DevQA.cs:32`

## Applied change
- Moved legacy shim to quarantine path:
  - `Modules/ContentSwap/Dev/Runtime/ContentSwapDevBootstrapper.cs`
  - -> `Modules/ContentSwap/Legacy/Dev/Runtime/ContentSwapDevBootstrapper.cs`
- Moved paired `.meta` with the file, preserving GUID.
- Kept content/API unchanged as a legacy shim under `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- Canonical owner remains `GlobalCompositionRoot.DevQA` -> `ContentSwapDevInstaller.EnsureInstalled()`.

## Files touched
- Moved:
  - `Modules/ContentSwap/Dev/Runtime/ContentSwapDevBootstrapper.cs`
  - `Modules/ContentSwap/Dev/Runtime/ContentSwapDevBootstrapper.cs.meta`
- Modified:
  - `Docs/Modules/DevQA.md`
- Added:
  - `Docs/Reports/Audits/2026-03-06/Modules/DevQA-Guard-Governance-Audit-v3.md`
  - `Docs/Reports/Audits/2026-03-06/Modules/DevQA-Guard-Governance-Audit-v3.md.meta`

## After evidence
### Shim references
Command:
`rg -n "ContentSwapDevBootstrapper" . -g "*.cs"`

Expected result:
- only `Modules/ContentSwap/Legacy/Dev/Runtime/ContentSwapDevBootstrapper.cs`

### Strict leak sweep
Command:
`rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu|InitializeOnLoadMethod" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"`

Expected result:
- `0 matches`

## Conclusion
- Canonical owner remains unique in composition.
- No runtime dependency on the legacy shim remains.
- Release behavior-preserving; DevBuild/Editor QA harness remains via `GlobalCompositionRoot.DevQA`.