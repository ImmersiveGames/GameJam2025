# DevQA Guard Governance Audit v4 (DQ-1.9)

Date: 2026-03-07  
Source of truth: local workspace (`C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts`).

## Behavior-preserving statement
- DOC-only change.
- No `.cs` files changed.
- Release behavior-preserving.
- DevBuild/Editor QA harness unchanged.

## RuntimeInitializeOnLoadMethod allowlist
Allowed outside `Dev/**`, `Editor/**`, `Legacy/**`, `QA/**`:
- `Core/Logging/DebugUtility.cs`
  - justification: reset/bootstrap logging state.
- `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs`
  - justification: canonical composition bootstrap entrypoint.

Rule:
- Any new `RuntimeInitializeOnLoadMethod` outside this allowlist requires:
  - a new audit
  - fresh `rg` evidence
  - explicit decision recorded in docs

## Evidence
### RuntimeInitializeOnLoadMethod allowlist check
Command:
`rg -n "RuntimeInitializeOnLoadMethod" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"`

Result:
- `Core/Logging/DebugUtility.cs:51`
- `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs:61`

### PR gate checklist
- Strict leak sweep:
  - `rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"`
- RuntimeInitializeOnLoadMethod allowlist:
  - `rg -n "RuntimeInitializeOnLoadMethod" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"`

## Files touched
- Modified:
  - `Docs/Modules/DevQA.md`
  - `Docs/Reports/Audits/2026-03-06/Audit-Index.md`
  - `Docs/Reports/Audits/2026-03-06/Module-Audit-Summary.md`
- Added:
  - `Docs/Reports/Audits/2026-03-06/Modules/DevQA-Guard-Governance-Audit-v4.md`
  - `Docs/Reports/Audits/2026-03-06/Modules/DevQA-Guard-Governance-Audit-v4.md.meta`