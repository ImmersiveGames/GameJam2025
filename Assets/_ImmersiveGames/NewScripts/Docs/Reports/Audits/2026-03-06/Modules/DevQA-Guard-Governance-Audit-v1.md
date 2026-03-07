# DevQA Guard Governance Audit v1 (DQ-1.6)

Date: 2026-03-07  
Source of truth: local workspace (`C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts`).

## Behavior-preserving statement
- No public runtime contract changes.
- No composition pipeline order changes.
- No gameplay/progression/scene-flow changes.
- Behavior-preserving in Release; DevBuild/Editor keeps QA harness.

## Canonical Policy
| Zone | Canonical paths | Default guard | Allowed | Disallowed |
|---|---|---|---|---|
| Runtime | everything outside `Dev/**`, `Editor/**`, `Legacy/**`, `Editor/QA/**` | no Dev/Editor guard by default | `RuntimeInitializeOnLoadMethod` with no `UnityEditor` dependency | `UnityEditor`, `EditorApplication`, `AssetDatabase`, `FindAssets`, `MenuItem`, `ContextMenu`, `InitializeOnLoadMethod` |
| Dev | `Dev/**`, `Modules/**/Dev/**` | `#if UNITY_EDITOR || DEVELOPMENT_BUILD` | Dev/QA harness, hotkeys, debug GUI | `UnityEditor` APIs outside `#if UNITY_EDITOR` |
| Editor | `Editor/**`, `Modules/**/Editor/**` | editor-only; prefer `#if UNITY_EDITOR` | tooling, editor bootstrap | player compilation |
| QA | `Editor/QA/**` | `#if UNITY_EDITOR` by folder and/or explicit guard | editor-only QA harness | player code |
| Legacy | `Legacy/**` | apply Dev or Editor rule based on content | isolated compat/tooling | new runtime leaks |

## Do / Don't
- Do: use `partial + Dev file` to remove `ContextMenu`, `MenuItem`, `AssetDatabase` and `EditorApplication` from runtime.
- Do: guard `Modules/**/Dev/*.DevQA.cs` with `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- Do: keep `using UnityEditor;` only inside `#if UNITY_EDITOR`.
- Don't: use `NEWSCRIPTS_QA` or `NEWSCRIPTS_DEV` as canonical guards.
- Don't: leave `InitializeOnLoadMethod` outside `Editor/**`.

## Step A - Inventory
### A1. All relevant #if / #elif
Command:
`rg -n "^\s*#(if|elif)\s+.*(UNITY_EDITOR|DEVELOPMENT_BUILD|NEWSCRIPTS_|ENABLE_|DEBUG)" . -g "*.cs"`

Summary:
- `UNITY_EDITOR` / `DEVELOPMENT_BUILD` remain the canonical guards in `Dev/**`, `Editor/**`, DevQA partials, and a few controlled runtime observability points.
- `NEWSCRIPTS_QA` and `NEWSCRIPTS_DEV`: `0 matches`.
- Residual `NEWSCRIPTS_*` matches:
  - `Core/Logging/DebugUtility.cs:54`
  - `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs:64`
  - `Infrastructure/Composition/GlobalCompositionRoot.Baseline.cs:9`
  - `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs:79`
  - `Infrastructure/Observability/Baseline/BaselineInvariantAsserter.cs:548`
  - `Infrastructure/Observability/Baseline/BaselineInvariantAsserter.cs:553`

### A2. Editor API leaks outside Dev/Editor/Legacy/QA
Command:
`rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu|InitializeOnLoadMethod" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"`

Summary:
- Matches only:
  - `Core/Logging/DebugUtility.cs:51` `RuntimeInitializeOnLoadMethod`
  - `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs:61` `RuntimeInitializeOnLoadMethod`
- No `UnityEditor`, `EditorApplication`, `AssetDatabase`, `FindAssets`, `MenuItem`, `ContextMenu` or `InitializeOnLoadMethod` leaks outside allowed folders.

### A3. NEWSCRIPTS_* usage
Command:
`rg -n "NEWSCRIPTS_(QA|DEV|DEBUG|BASELINE|MODE)" . -g "*.cs"`

Summary:
- `NEWSCRIPTS_QA`: `0 matches`
- `NEWSCRIPTS_DEV`: `0 matches`
- `NEWSCRIPTS_DEBUG`: `0 matches`
- Remaining matches are only `NEWSCRIPTS_MODE` and `NEWSCRIPTS_BASELINE_ASSERTS`.

## Deprecated NEWSCRIPTS_* Residuals
These symbols are not canonical for DQ-1.6 and remain only as deprecated residuals to avoid changing bootstrap/assert semantics in this task.

| Symbol | Path:Line | Rationale |
|---|---|---|
| `NEWSCRIPTS_MODE` | `Core/Logging/DebugUtility.cs:54` | Debug-only bootstrap log/reset observability; leaving it untouched avoids startup behavior changes. |
| `NEWSCRIPTS_MODE` | `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs:64` | Global bootstrap gating for the NewScripts area; changing it here would alter runtime boot semantics. |
| `NEWSCRIPTS_BASELINE_ASSERTS` | `Infrastructure/Composition/GlobalCompositionRoot.Baseline.cs:9` | Optional baseline asserter installation switch. |
| `NEWSCRIPTS_BASELINE_ASSERTS` | `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs:79` | Optional baseline asserter registration in the composition pipeline. |
| `NEWSCRIPTS_BASELINE_ASSERTS` | `Infrastructure/Observability/Baseline/BaselineInvariantAsserter.cs:548` | Throw-on-fail behavior for baseline invariants. |
| `NEWSCRIPTS_BASELINE_ASSERTS` | `Infrastructure/Observability/Baseline/BaselineInvariantAsserter.cs:553` | Exception type compiled only when baseline asserts are enabled. |

## Touched Files / Moves
- Modified:
  - `Docs/Modules/DevQA.md`
  - `Docs/Reports/Audits/2026-03-06/Audit-Index.md`
  - `Docs/Reports/Audits/2026-03-06/Module-Audit-Summary.md`
- Added:
  - `Docs/Reports/Audits/2026-03-06/Modules/DevQA-Guard-Governance-Audit-v1.md`
  - `Docs/Reports/Audits/2026-03-06/Modules/DevQA-Guard-Governance-Audit-v1.md.meta`
- Moves:
  - none

## Step D - Post-check
### D1. Strict leak sweep
Command:
`rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"`

Result:
- `0 matches`

### D2. Runtime bootstrap inventory
Command:
`rg -n "RuntimeInitializeOnLoadMethod" . -g "*.cs"`

Result:
- `Core/Logging/DebugUtility.cs:51`
- `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs:61`
- `Modules/ContentSwap/Dev/Runtime/ContentSwapDevBootstrapper.cs:9`

Interpretation:
- Runtime-allowed canonical bootstrap points remain only the first two files.
- `Modules/ContentSwap/Dev/Runtime/ContentSwapDevBootstrapper.cs:9` is a Dev-path inventory match and remains outside the strict runtime zone.

### D3. NEWSCRIPTS_* inventory
Command:
`rg -n "NEWSCRIPTS_" . -g "*.cs"`

Result:
- `Core/Logging/DebugUtility.cs:54`
- `Core/Logging/DebugUtility.cs:55`
- `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs:64`
- `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs:66`
- `Infrastructure/Composition/GlobalCompositionRoot.Baseline.cs:9`
- `Infrastructure/Composition/GlobalCompositionRoot.Baseline.cs:15`
- `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs:79`
- `Infrastructure/Observability/Baseline/BaselineInvariantAsserter.cs:19`
- `Infrastructure/Observability/Baseline/BaselineInvariantAsserter.cs:548`
- `Infrastructure/Observability/Baseline/BaselineInvariantAsserter.cs:553`

Interpretation:
- Non-zero by design for DQ-1.6.
- All remaining callsites are documented above as `DEPRECATED` residuals or comments/log strings tied to those residual symbols.