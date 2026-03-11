# QA-1

## Summary
- Objetivo: inventariar tooling Editor/QA em `Assets/_ImmersiveGames/NewScripts/**`, aplicar A1/A2/A3 por candidato e podar apenas o que fosse seguro, sem tocar em `Assets/_ImmersiveGames/Scripts/**`.
- Resultado: nenhum move/delete seguro novo. O layout ja estava canonico no escopo `Editor/**` e `Modules/**/Editor/**`, sem `QA/**` de topo para migrar.
- Runtime/pipeline/owners preservados.
- Confirmacao: nao toquei em `Assets/_ImmersiveGames/Scripts/**`.

## Inventory

### Editor roots
```text
Editor\Core\Events\EventBusUtil.Editor.cs
Editor\Core\Logging\HardFailFastH1.Editor.cs
Editor\Infrastructure\Composition\GlobalCompositionRoot.Editor.cs
Modules\ContentSwap\Editor\ContentSwapQaMenuItems.cs
Modules\GameLoop\IntroStage\Editor\IntroStageQaMenuItems.cs
Modules\Navigation\Editor\Drawers\NavigationIntentIdPropertyDrawer.cs
Modules\Navigation\Editor\Tools\GameNavigationCatalogNormalizer.cs
Modules\SceneFlow\Editor\Drawers\SceneFlowProfileIdPropertyDrawer.cs
Modules\SceneFlow\Editor\Drawers\SceneRouteIdPropertyDrawer.cs
Modules\SceneFlow\Editor\Drawers\TransitionStyleIdPropertyDrawer.cs
Modules\SceneFlow\Editor\Validation\SceneFlowConfigValidator.cs
```

### Command outputs
```text
rg -n "MenuItem\(|ContextMenu\(|UnityEditor|AssetDatabase|FindAssets|EditorApplication" . -g "*.cs"
```

```text
Editor-side matches found in canonical files plus Dev/QA context menus. Canonical editor files in scope:
.\Editor\Infrastructure\Composition\GlobalCompositionRoot.Editor.cs
.\Editor\Core\Logging\HardFailFastH1.Editor.cs
.\Editor\Core\Events\EventBusUtil.Editor.cs
.\Modules\Navigation\Editor\Tools\GameNavigationCatalogNormalizer.cs
.\Modules\Navigation\Editor\Drawers\NavigationIntentIdPropertyDrawer.cs
.\Modules\GameLoop\IntroStage\Editor\IntroStageQaMenuItems.cs
.\Modules\SceneFlow\Editor\Validation\SceneFlowConfigValidator.cs
.\Modules\SceneFlow\Editor\Drawers\SceneFlowProfileIdPropertyDrawer.cs
.\Modules\SceneFlow\Editor\Drawers\SceneRouteIdPropertyDrawer.cs
.\Modules\SceneFlow\Editor\Drawers\TransitionStyleIdPropertyDrawer.cs
.\Modules\ContentSwap\Editor\ContentSwapQaMenuItems.cs
```

```text
rg -n "Tools/|ImmersiveGames/|QA/" . -g "*.cs"
```

```text
Menu paths in canonical editor files:
.\Modules\GameLoop\IntroStage\Editor\IntroStageQaMenuItems.cs:12:        private const string SelectQaMenuPath = "ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Select QA_IntroStage Object";
.\Modules\GameLoop\IntroStage\Editor\IntroStageQaMenuItems.cs:39:        [MenuItem("ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Complete (Force)", priority = 1291)]
.\Modules\GameLoop\IntroStage\Editor\IntroStageQaMenuItems.cs:62:        [MenuItem("ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Skip (Force)", priority = 1292)]
.\Modules\Navigation\Editor\Tools\GameNavigationCatalogNormalizer.cs:30:        [MenuItem("ImmersiveGames/NewScripts/QA/Navigation/Normalize Catalogs", priority = 1510)]
.\Modules\ContentSwap\Editor\ContentSwapQaMenuItems.cs:11:        [MenuItem("ImmersiveGames/NewScripts/QA/ContentSwap/Select QA_ContentSwap Object", priority = 1310)]
.\Modules\SceneFlow\Editor\Validation\SceneFlowConfigValidator.cs:34:        [MenuItem("ImmersiveGames/NewScripts/Tools/SceneFlow/Validate Config", priority = 1410)]
```

```text
rg -n "InitializeOnLoadMethod|InitializeOnLoad\b|RuntimeInitializeOnLoadMethod" . -g "*.cs"
```

```text
.\Core\Logging\DebugUtility.cs:62:        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
.\Editor\Core\Events\EventBusUtil.Editor.cs:8:        [InitializeOnLoadMethod]
.\Infrastructure\Composition\GlobalCompositionRoot.Entry.cs:61:        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
```

## A1/A2/A3 Matrix

| File | Decision | A1 | A2 | A3 | Action |
|---|---|---|---|---|---|
| `Editor/Core/Events/EventBusUtil.Editor.cs` | KEEP | FAIL: runtime/core partial callsites exist | PASS: `0 matches` | n/a | none |
| `Editor/Core/Logging/HardFailFastH1.Editor.cs` | KEEP | FAIL: runtime callsites exist in Navigation/LevelFlow/WorldLifecycle/SceneFlow | PASS: `0 matches` | n/a | none |
| `Editor/Infrastructure/Composition/GlobalCompositionRoot.Editor.cs` | KEEP | FAIL: composition/root partial callsites exist | PASS: `0 matches` | n/a | none |
| `Modules/ContentSwap/Editor/ContentSwapQaMenuItems.cs` | KEEP | PASS: `0 matches` | PASS: `0 matches` | PASS: `1 occurrence` | canonical owner |
| `Modules/GameLoop/IntroStage/Editor/IntroStageQaMenuItems.cs` | KEEP | PASS: `0 matches` | PASS: `0 matches` | PASS: unique menu paths | canonical owner |
| `Modules/Navigation/Editor/Drawers/NavigationIntentIdPropertyDrawer.cs` | KEEP | PASS: `0 matches` | PASS: `0 matches` | n/a (`CustomPropertyDrawer`) | canonical drawer |
| `Modules/Navigation/Editor/Tools/GameNavigationCatalogNormalizer.cs` | KEEP | PASS: `0 matches` | PASS: `0 matches` | PASS: `1 occurrence` | canonical owner |
| `Modules/SceneFlow/Editor/Drawers/SceneFlowProfileIdPropertyDrawer.cs` | KEEP | PASS: `0 matches` | PASS: `0 matches` | n/a (`CustomPropertyDrawer`) | canonical drawer |
| `Modules/SceneFlow/Editor/Drawers/SceneRouteIdPropertyDrawer.cs` | KEEP | PASS: `0 matches` | PASS: `0 matches` | n/a (`CustomPropertyDrawer`) | canonical drawer |
| `Modules/SceneFlow/Editor/Drawers/TransitionStyleIdPropertyDrawer.cs` | KEEP | PASS: `0 matches` | PASS: `0 matches` | n/a (`CustomPropertyDrawer`) | canonical drawer/shared contracts |
| `Modules/SceneFlow/Editor/Validation/SceneFlowConfigValidator.cs` | KEEP | PASS: `0 matches` | PASS: `0 matches` | PASS: `1 occurrence` | canonical owner |

## Candidate Evidence

### HardFailFastH1.Editor.cs
```text
rg -n "HardFailFastH1" . -g "*.cs" -g "!**/Editor/**" -g "!**/Dev/**" -g "!**/QA/**" -g "!**/Legacy/**"
```

```text
.\Core\Logging\HardFailFastH1.cs:6:    public static partial class HardFailFastH1
.\Modules\SceneFlow\Transition\Runtime\MacroLevelPrepareCompletionGate.cs:61:            HardFailFastH1.Trigger(typeof(MacroLevelPrepareCompletionGate),
.\Modules\Navigation\GameNavigationService.cs:82:                HardFailFastH1.Trigger(typeof(GameNavigationService),
```

### GlobalCompositionRoot.Editor.cs
```text
rg -n "GlobalCompositionRoot" . -g "*.cs" -g "!**/Editor/**" -g "!**/Dev/**" -g "!**/QA/**" -g "!**/Legacy/**"
```

```text
.\Infrastructure\Composition\GlobalCompositionRoot.WorldLifecycle.cs:6:    public static partial class GlobalCompositionRoot
.\Infrastructure\Composition\GlobalCompositionRoot.SceneFlow.cs:20:    public static partial class GlobalCompositionRoot
.\Infrastructure\Composition\GlobalCompositionRoot.Navigation.cs:13:    public static partial class GlobalCompositionRoot
```

### Unique menu proof
```text
rg --fixed-strings "ImmersiveGames/NewScripts/QA/ContentSwap/Select QA_ContentSwap Object" . -g "*.cs"
.\Modules\ContentSwap\Editor\ContentSwapQaMenuItems.cs:11:        [MenuItem("ImmersiveGames/NewScripts/QA/ContentSwap/Select QA_ContentSwap Object", priority = 1310)]

rg --fixed-strings "ImmersiveGames/NewScripts/QA/Navigation/Normalize Catalogs" . -g "*.cs"
.\Modules\Navigation\Editor\Tools\GameNavigationCatalogNormalizer.cs:30:        [MenuItem("ImmersiveGames/NewScripts/QA/Navigation/Normalize Catalogs", priority = 1510)]

rg --fixed-strings "ImmersiveGames/NewScripts/Tools/SceneFlow/Validate Config" . -g "*.cs"
.\Modules\SceneFlow\Editor\Validation\SceneFlowConfigValidator.cs:34:        [MenuItem("ImmersiveGames/NewScripts/Tools/SceneFlow/Validate Config", priority = 1410)]
```

## Moves / Deletes
- Nenhum move novo.
- Nenhum delete novo.
- Nenhuma pasta vazia adicional para prune em `Editor/**` ou `Modules/**/Editor/**`.

## Final Checks

### Leak sweep
```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/QA/**" -g "!**/Legacy/**"
0 matches
```

### Gates
```text
PASS Gate A
PASS Gate A2
PASS Gate B
```
