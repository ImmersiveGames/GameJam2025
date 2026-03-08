# Build Matrix

Fonte unica para governanca de build/guards do baseline 3.1.

| Build | DevQA compila? | DevQA disponivel (menus/context menus)? | RuntimeInitializeOnLoadMethod allowlist | Editor-only APIs permitidas? | Smoke A-E executavel via QA? |
|---|---|---|---|---|---|
| Editor | Sim | Sim | `Core/Logging/DebugUtility.cs` + `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs` | Sim, mas apenas em `Editor/**` ou sob `#if UNITY_EDITOR` | Sim |
| DevBuild | Sim | Parcial: harness/runtime DevQA sim; `MenuItem`/tooling editor-only nao | `Core/Logging/DebugUtility.cs` + `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs` | Nao no player; qualquer API `UnityEditor` continua isolada em trilho editor-only | Sim |
| Release | Nao | Nao | `Core/Logging/DebugUtility.cs` + `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs` | Nao | Nao |

## Regras explicitas
- `Release`: DevQA fica excluido por guards de compilacao (`UNITY_EDITOR || DEVELOPMENT_BUILD`).
- `DevBuild`: DevQA fica incluido no player para harness/evidencia runtime; tooling editor-only continua fora.
- `Editor`: DevQA e Editor tooling ficam disponiveis, sem alterar o contrato de boot canonico.
- Allowlist de `RuntimeInitializeOnLoadMethod` fora de `Dev/**`, `Editor/**`, `Legacy/**`, `QA/**`: exatamente
  - `Core/Logging/DebugUtility.cs`
  - `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs`
- Qualquer novo `RuntimeInitializeOnLoadMethod` fora dessa allowlist exige nova decisao documentada + evidencia `rg`.

## Layering curto
- `DevQA` pode adicionar harness, menu de evidencia, hotkeys e installers em `Editor`/`DevBuild`.
- `RuntimeMode/Logging` continua owner do boot logging (`EarlyDefault`, `BootstrapPolicy`, dedupe, `policyKey`).
- `DevQA` nao pode alterar boot order nem introduzir writer alternativo de logging policy.
## Gates
- Execucao canonica de PR checks: `Tools/Gates/Run-NewScripts-RgGates.ps1`.
- O script centraliza os gates A / A2 / B para evitar drift e remover a ambiguidade de substring entre `InitializeOnLoadMethod` e `RuntimeInitializeOnLoadMethod`.
