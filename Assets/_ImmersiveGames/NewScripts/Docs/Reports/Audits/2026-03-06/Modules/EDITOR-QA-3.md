# EDITOR-QA-3

## Summary
- auditDateDir: `2026-03-06`
- Scope: prune verification for editor/QA tooling already substituted by the canonical files below, plus final uniqueness and gate proofs.
- Canonical files confirmed:
  - `Modules/GameLoop/IntroStage/Editor/IntroStageQaMenuItems.cs`
  - `Modules/ContentSwap/Editor/ContentSwapQaMenuItems.cs`
  - `Modules/Navigation/Editor/Tools/GameNavigationCatalogNormalizer.cs`
- Net result in this round: no new deletes were required beyond the already-safe removals from `EDITOR-QA-2`; one canonical file was text-normalized so the menu-path uniqueness proof is exact.
- No file under `Assets/_ImmersiveGames/Scripts/**` was touched.
- No runtime, pipeline, owners, or payload code changed.

## Final Candidate Table
| FilePath | TypeName(s) | MenuItem paths | Meta GUID | DuplicatesOf | CallsitesOutsideEditor | AssetRefs | Decision | Reason |
|---|---|---|---|---|---|---|---|---|
| `Modules/GameLoop/IntroStage/Editor/IntroStageQaMenuItems.cs` | `IntroStageQaMenuItems` | `ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Select QA_IntroStage Object`; `ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Complete (Force)`; `ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Skip (Force)` | `7a5695e8cce465c4181acc5a706c5efd` | Canonical current tool | `0` | `0` | `KEEP` | Canonical replacement for old IntroStage editor menu tooling; literal menu path normalized through a const so the uniqueness grep returns one hit. |
| `Modules/ContentSwap/Editor/ContentSwapQaMenuItems.cs` | `ContentSwapQaMenuItems` | `ImmersiveGames/NewScripts/QA/ContentSwap/Select QA_ContentSwap Object` | `dbeab39e7e2b4d4bb55f05ca4e1ee495` | Canonical current tool | `0` | `0` | `KEEP` | Canonical replacement for the editor menu action that used to live mixed into dev tooling. |
| `Modules/Navigation/Editor/Tools/GameNavigationCatalogNormalizer.cs` | `GameNavigationCatalogNormalizer` | `ImmersiveGames/NewScripts/QA/Navigation/Normalize Catalogs` | `f526c71f820b463caa74adf84f4673bf` | Canonical current tool | `0` | `0` | `KEEP` | Canonical editor-only normalizer already moved out of `Dev/Editor`. |
| `Modules/GameLoop/IntroStage/Dev/Editor/IntroStageDevTools.cs` | `IntroStageDevTools` | legacy select helper | `a93c29780d15bdc499c929c46cc67349` | `Modules/GameLoop/IntroStage/Editor/IntroStageQaMenuItems.cs` | `0 compile refs` | `0` | `DELETE (already applied)` | A1 passed: only docs/history plus one log string mention in `Infrastructure/Composition/GlobalCompositionRoot.DevQA.cs`; A2 passed: no asset refs; A3 passed: functionality replaced by the canonical IntroStage editor menu file. |
| `Modules/SceneFlow/Editor/Validation/TransitionStyleProfileRefMigrator.cs` | `TransitionStyleProfileRefMigrator` | none | `8deef1a76e544f4dae8866f3e0afb25b` | DataCleanup v1 archive | `0 compile refs` | `0` | `DELETE (already applied)` | A1 passed: no active `.cs` callsites; A2 passed: no asset refs; A3 passed: explicitly archived as obsolete DataCleanup v1 in `EDITOR-QA-2`. |
| `Modules/SceneFlow/Editor/Validation/SceneFlowConfigReserializer.cs` | `SceneFlowConfigReserializer` | none | `654f7f1cf2b74f2ca15e654c87b247aa` | DataCleanup v1 archive | `0 compile refs` | `0` | `DELETE (already applied)` | A1 passed: no active `.cs` callsites; A2 passed: no asset refs; A3 passed: explicitly archived as obsolete DataCleanup v1 in `EDITOR-QA-2`. |

## Commands and Proofs
### Canonical menu uniqueness
```text
rg -n --fixed-strings 'ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Select QA_IntroStage Object' . -g '*.cs'
.\Modules\GameLoop\IntroStage\Editor\IntroStageQaMenuItems.cs:12:        private const string SelectQaMenuPath = "ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Select QA_IntroStage Object";

rg -n --fixed-strings 'ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Complete (Force)' . -g '*.cs'
.\Modules\GameLoop\IntroStage\Editor\IntroStageQaMenuItems.cs:39:        [MenuItem("ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Complete (Force)", priority = 1291)]

rg -n --fixed-strings 'ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Skip (Force)' . -g '*.cs'
.\Modules\GameLoop\IntroStage\Editor\IntroStageQaMenuItems.cs:62:        [MenuItem("ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Skip (Force)", priority = 1292)]

rg -n --fixed-strings 'ImmersiveGames/NewScripts/QA/ContentSwap/Select QA_ContentSwap Object' . -g '*.cs'
.\Modules\ContentSwap\Editor\ContentSwapQaMenuItems.cs:11:        [MenuItem("ImmersiveGames/NewScripts/QA/ContentSwap/Select QA_ContentSwap Object", priority = 1310)]

rg -n --fixed-strings 'ImmersiveGames/NewScripts/QA/Navigation/Normalize Catalogs' . -g '*.cs'
.\Modules\Navigation\Editor\Tools\GameNavigationCatalogNormalizer.cs:30:        [MenuItem("ImmersiveGames/NewScripts/QA/Navigation/Normalize Catalogs", priority = 1510)]
```

### Candidate proofs
```text
rg -n "\bIntroStageDevTools\b" . -g "*.cs" -g "*.md"
Result: docs/history only plus one log string in .\Infrastructure\Composition\GlobalCompositionRoot.DevQA.cs; no compile callsites.

rg -n "a93c29780d15bdc499c929c46cc67349" -g "*.unity" -g "*.prefab" -g "*.asset" .
Result: 0 matches

rg -n "\bTransitionStyleProfileRefMigrator\b|\bSceneFlowConfigReserializer\b" . -g "*.cs" -g "*.md"
Result: only `EDITOR-QA-2.md` history entries; no active `.cs` callsites.

rg -n "8deef1a76e544f4dae8866f3e0afb25b|654f7f1cf2b74f2ca15e654c87b247aa" -g "*.unity" -g "*.prefab" -g "*.asset" .
Result: 0 matches
```

## Deleted Files
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\IntroStage\Dev\Editor\IntroStageDevTools.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\IntroStage\Dev\Editor\IntroStageDevTools.cs.meta`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\Validation\TransitionStyleProfileRefMigrator.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\Validation\TransitionStyleProfileRefMigrator.cs.meta`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\Validation\SceneFlowConfigReserializer.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\Validation\SceneFlowConfigReserializer.cs.meta`

## Moved Files
- None in `EDITOR-QA-3`.
- Canonical move history remains the one already applied in `EDITOR-QA-2`.

## Post-checks
### Leak sweep
```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" Assets/_ImmersiveGames/NewScripts -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
Result: 0 matches
```

### Gates
```text
PASS Gate A
PASS Gate A2
PASS Gate B
```

## Scope confirmation
- Nenhum arquivo em `Assets/_ImmersiveGames/Scripts/**` foi tocado.
- Nenhuma mudanca em runtime/pipeline/owners.
