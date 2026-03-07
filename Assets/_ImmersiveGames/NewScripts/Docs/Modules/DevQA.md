# DevQA (Baseline 3.1)

## O que existe
- Pipeline canonico de instalacao DevQA:
  - `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs` -> `CompositionInstallStage.DevQA` -> `InstallDevQaServices()`.
  - `Infrastructure/Composition/GlobalCompositionRoot.DevQA.cs` registra:
    - `IntroStageDevInstaller`
    - `ContentSwapDevInstaller`
    - `SceneFlowDevInstaller`
    - `LevelFlowDevInstaller`
    - `IntroStageRuntimeDebugGui` (guardado por `UNITY_EDITOR || DEVELOPMENT_BUILD`).
- Features principais:
  - IntroStage QA (`Modules/GameLoop/IntroStage/Dev/**`)
  - ContentSwap QA (`Modules/ContentSwap/Dev/**`)
  - SceneFlow QA (`Modules/SceneFlow/Dev/**`)
  - LevelFlow QA (`Modules/LevelFlow/Dev/**`)
  - WorldLifecycle Dev hotkey/hook (`Modules/WorldLifecycle/Dev/**`)
  - Editor tooling em `Modules/**/Editor/**` (drawers/validators/menu items).

## Como usar
- ContextMenu:
  - selecione GOs `QA_IntroStage`, `QA_ContentSwap`, `QA_SceneFlow`, `QA_LevelFlow` no Hierarchy (DontDestroyOnLoad).
- MenuItem (Editor):
  - caminhos `Tools/NewScripts/QA/...` e `ImmersiveGames/NewScripts/...` para acoes dev/editor.
- RuntimeDebugGui:
  - `IntroStageRuntimeDebugGui` aparece em runtime de dev para concluir IntroStage.
- QA GOs criados em runtime:
  - instaladores criam/reaproveitam GOs `QA_*` e marcam `DontDestroyOnLoad`.

## Politica canonica
- Nao deve rodar em producao:
  - instalacao central via `InstallDevQaServices()` esta sob `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- Ferramentas editor-only:
  - classes em `Editor/**` e blocos `#if UNITY_EDITOR`.
- Runtime debug controlado:
  - bootstrappers/hotkeys com guards de build (`UNITY_EDITOR`, `DEVELOPMENT_BUILD`, `NEWSCRIPTS_DEV`, `NEWSCRIPTS_QA`).

## Manual confirmation required
- `Modules/ContentSwap/Dev/Runtime/ContentSwapDevBootstrapper.cs`: possivel sobreposicao com installer central DevQA.
- `Modules/WorldLifecycle/Dev/WorldResetRequestHotkeyDevBootstrap.cs`: bootstrap runtime paralelo ao trilho de composicao DevQA.
- `Modules/WorldLifecycle/Dev/WorldLifecycleHookLoggerA.cs`: classe dev sem guard explicito de compilacao.
- ContextMenus QA em arquivos de runtime (`PauseOverlayController`, `PostGameOverlayController`): manter ate validar wiring por inspector/prefab.

## Status DQ-1.2 (2026-03-06)
- Trilho canonico reforcado: instalacao DevQA centralizada em `GlobalCompositionRoot.Pipeline.cs` (`CompositionInstallStage.DevQA`) + `GlobalCompositionRoot.DevQA.cs`.
- `ContentSwapDevBootstrapper` foi colocado em quarentena como legacy shim sem auto-install; o owner canonico permanece `GlobalCompositionRoot.DevQA` -> `ContentSwapDevInstaller.EnsureInstalled()`.
- Hotkey DEV de WorldLifecycle foi centralizado no installer DevQA (`RegisterWorldLifecycleQaInstaller` -> `WorldResetRequestHotkeyDevBootstrap.EnsureInstalled()`).
- Guards consolidados nos arquivos alterados para `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.

## Status DQ-1.3 (2026-03-06)
- `WorldLifecycleHookLoggerA` agora esta isolado por `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- Callsite de registro em `SceneScopeCompositionRoot.RegisterSceneLifecycleHooks` tambem esta sob `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- `Modules/Gameplay/Editor/RunRearm/**` normalizado para `#if UNITY_EDITOR`.
- `PauseOverlayController` permanece com ContextMenu QA protegido por `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- `PostGameOverlayController` teve helper/guard QA (`_qaGuardBusy`, `BeginQaRiskCommand`, `EndQaRiskCommand`) isolados no mesmo guard DevQA.
- Evidencia estatica e rationale em `Docs/Reports/Audits/2026-03-06/Modules/DevQA-Cleanup-Audit-v3.md`.

Nota de compilacao (DQ-1.3): o codigo DevQA deve compilar apenas em `UNITY_EDITOR` ou `DEVELOPMENT_BUILD`; builds release de Player excluem esse codigo via guards de pre-processamento para evitar referencias a namespaces/artefatos Dev fora do escopo de QA.

## Build Matrix (Contract)
| Ambiente | UNITY_EDITOR | DEVELOPMENT_BUILD | DevQA Tools (QA_*) | Hotkeys/ContextMenus | Evidencia esperada em log |
|---|---|---|---|---|---|
| Editor | true | n/a | ON | ON | logs [QA]/[DevQA] presentes |
| Dev Build | false | true | ON | ON | logs [QA]/[DevQA] presentes |
| Release | false | false | OFF | OFF | ausencia de logs de DevQA |

Se precisar de QA em build distribuido, use Development Build (ou crie um simbolo/variant proprio; fora do escopo do baseline).

## Leak Sweep Policy (DQ-1.4)
- Runtime files (`Modules/**` fora de `Dev/Editor/Legacy`) nao devem carregar tooling DevQA/Editor embutido quando a extracao estrutural for segura.
- Padrao preferencial: `partial` no runtime + arquivo `.../Dev/<Class>.DevQA.cs`.
- Arquivo DevQA inteiro sob `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- Qualquer `using UnityEditor;` e chamadas `UnityEditor.*` devem ficar sob `#if UNITY_EDITOR`.
- Em casos de fail-fast/runtime critico (ex.: `Application.Quit`/`EditorApplication.isPlaying` em guard editor), manter no runtime e classificar como `A` (sem mudanca nesta etapa).

## Status DQ-1.4 (2026-03-07)
- Leak sweep executado em `Modules/**` com exclusao de `Dev/Editor/Legacy` para identificar vazamentos reais no trilho runtime.
- `TransitionStyleCatalogAsset` teve `ContextMenu` de validacao extraido para parcial DevQA (`Modules/SceneFlow/Navigation/Dev/TransitionStyleCatalogAsset.DevQA.cs`).
- `PauseOverlayController.DevQA` validado: `using UnityEditor;` permanece sob `#if UNITY_EDITOR` e sem vazamento no runtime.
- Suspeitos restantes foram classificados como runtime-critical (`A`) ou manual confirmation (`C`) no snapshot DQ-1.4.
- Snapshot: `Docs/Reports/Audits/2026-03-06/Modules/DevQA-LeakSweep-Audit-v1.md`.

## DQ-1.4.1 Pattern (partial + DevQA)
- Mantenha o arquivo runtime principal no mesmo path e torne a classe `partial` quando necessario.
- Extraia blocos Editor/QA para `.../Dev/<Class>.DevQA.cs` com guard de arquivo `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- Em arquivos DevQA, `using UnityEditor;` e APIs `UnityEditor.*` ficam sob `#if UNITY_EDITOR`.

## Leaks corrigidos (DQ-1.4.x)
- DQ-1.4.1: `SceneRouteDefinitionAsset` (SceneFlow Navigation) extraido para parcial DevQA.
- DQ-1.4.2: `GameNavigationCatalogAsset` (Navigation) removeu `UnityEditor` do runtime via parcial DevQA.
- Padrao aplicado: runtime principal preservado + classe `partial` + arquivo `Dev/*.DevQA.cs` com guards de build/editor.

## Caso DQ-1.4.3
- Leak removido em `SceneRouteResetPolicy` (WorldLifecycle runtime): referencia a `EditorApplication` saiu do arquivo runtime.
- Extracao aplicada para `Modules/WorldLifecycle/Dev/SceneRouteResetPolicy.DevQA.cs` com padrao `partial + DevQA file`.
- Pos-check do runtime retornou sem simbolos Editor/DevQA no arquivo alvo.

## DQ-1.4.4+ (batch leak sweep)
- Limpados 6 runtimes com padrao `partial + DevQA file`: `MenuQuitButtonBinder`, `GameNavigationIntentCatalogAsset`, `GameLoopSceneFlowCoordinator`, `SceneTransitionService`, `SceneBuildIndexRef`, `SceneRouteCatalogAsset`.
- Tooling Editor/DevBuild foi movido para `Modules/**/Dev/*.DevQA.cs` com guard de arquivo `#if UNITY_EDITOR || DEVELOPMENT_BUILD` e APIs editor sob `#if UNITY_EDITOR`.
- Prova de fechamento do sweep: varredura global fora de `Dev/Editor/Legacy` para `UnityEditor|EditorApplication|AssetDatabase|FindAssets|ContextMenu|MenuItem|InitializeOnLoad|RuntimeInitializeOnLoadMethod` retornou 0 matches.

## DQ-1.5 note
- QA tooling lives under `NewScripts/Editor/QA` (Editor-only compilation path).
- Runtime/Core/Infrastructure files were cleaned from direct Editor API usage; editor hooks were moved to `Editor/**` partials.
- `RuntimeInitializeOnLoadMethod` remains allowed in canonical runtime bootstrap points.

## Guard Governance (Contract)
| Zone | Canonical paths | Default guard | Allowed | Disallowed |
|---|---|---|---|---|
| Runtime | everything outside `Dev/**`, `Editor/**`, `Legacy/**`, `Editor/QA/**` | no Dev/Editor guard by default | `RuntimeInitializeOnLoadMethod` with no `UnityEditor` dependency | `using UnityEditor`, `EditorApplication`, `AssetDatabase`, `FindAssets`, `MenuItem`, `ContextMenu`, `InitializeOnLoadMethod` |
| Dev | `Dev/**`, `Modules/**/Dev/**` | file under `#if UNITY_EDITOR || DEVELOPMENT_BUILD` | harness, hotkeys, debug GUI, `*.DevQA.cs` partials | `UnityEditor` APIs outside `#if UNITY_EDITOR` |
| Editor | `Editor/**`, `Modules/**/Editor/**` | editor-only; prefer `#if UNITY_EDITOR` if there is compile risk outside editor | authoring tooling, validation, `MenuItem`, `InitializeOnLoadMethod` | release runtime dependency |
| QA | `Editor/QA/**` | `#if UNITY_EDITOR` by folder and/or explicit guard | editor-only QA harness | player-compilable code |
| Legacy | `Legacy/**` | inherit Dev or Editor rule based on content | isolated compat/tooling | new runtime leaks |

### Do
- Make the runtime class `partial` and move tooling to `Modules/<X>/Dev/<Name>.DevQA.cs`.
- Guard the full Dev file with `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- Keep `using UnityEditor;` and `UnityEditor.*` calls inside `#if UNITY_EDITOR`.

### Don't
- Do not leave `ContextMenu`, `MenuItem`, `AssetDatabase` or `EditorApplication` in runtime files.
- Do not use `NEWSCRIPTS_QA` or `NEWSCRIPTS_DEV` as active compile policy.
- Do not use `InitializeOnLoadMethod` outside `Editor/**`.

### Example
```csharp
// Runtime file
public sealed partial class PauseOverlayController : MonoBehaviour
{
    public void Show() { }
}
```

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
public sealed partial class PauseOverlayController
{
#if UNITY_EDITOR
    [ContextMenu("QA/Show Overlay")]
    private void QaShowOverlay() => Show();
#endif
}
#endif
```

### Official Leak Sweep Commands
- Strict sweep:
  - `rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"`
- Broad inventory:
  - `rg -n "RuntimeInitializeOnLoadMethod" . -g "*.cs"`
  - `rg -n "NEWSCRIPTS_" . -g "*.cs"`

## Status DQ-1.6 (2026-03-07)
- Guard governance is now documented as the canonical per-zone contract (`Runtime` / `Dev` / `Editor` / `QA` / `Legacy`).
- The DQ-1.6 strict sweep confirmed `0 matches` outside `Dev/Editor/Legacy/QA`.
- Allowed bootstrap inventory remains `Core/Logging/DebugUtility.cs` and `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs` as canonical runtime `RuntimeInitializeOnLoadMethod` sites.
- `NEWSCRIPTS_QA` and `NEWSCRIPTS_DEV` remain absent from code; residual `NEWSCRIPTS_MODE` and `NEWSCRIPTS_BASELINE_ASSERTS` are documented as `DEPRECATED` in the DQ-1.6 audit, with no refactor in this step to avoid bootstrap/assert behavior changes.
- Behavior-preserving in Release; DevBuild/Editor keeps QA harness.
## Status DQ-1.7 (2026-03-07)
- `Modules/ContentSwap/Legacy/Dev/Runtime/ContentSwapDevBootstrapper.cs` remains only as a legacy shim and no longer auto-installs via `RuntimeInitializeOnLoadMethod`.
- ContentSwap DevQA remains installed only via `GlobalCompositionRoot.DevQA` -> `RegisterContentSwapQaInstaller()` -> `ContentSwapDevInstaller.EnsureInstalled()`.
- `ContentSwapDevBootstrapper.EnsureInstalled()` remains as an explicit legacy shim, with an `[OBS][LEGACY][DevQA]` log and delegation to `ContentSwapDevInstaller.EnsureInstalled()`.
- Release behavior-preserving; DevBuild/Editor QA harness remains via `GlobalCompositionRoot.DevQA`.