# EDITOR-QA-9

## Summary
- Objetivo: sweep final de tooling Editor/QA em `Assets/_ImmersiveGames/NewScripts/**`, com prune apenas se houvesse prova A1+A2+A3 e sem tocar em `Assets/_ImmersiveGames/Scripts/**`.
- Resultado: nenhum delete seguro novo. O batch ficou em evidence freeze com hardening validado para SceneFlow drawers e inventario final de candidatos editor-only mantidos como `KEEP`.
- Compile hardening: os contratos shared `ISceneFlowIdSourceProvider<TId>`, `SceneFlowIdSourceResult` e `SceneFlowIdSourceUtility` seguem definidos uma unica vez no arquivo canonico `Modules/SceneFlow/Editor/Drawers/TransitionStyleIdPropertyDrawer.cs`.
- Confirmacao: nao toquei em `Assets/_ImmersiveGames/Scripts/**`.

## Baseline Evidence (Pre)

### Leak sweep estrito
```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu|InitializeOnLoadMethod" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/QA/**" -g "!**/Legacy/**"
```

```text
.\Infrastructure\Composition\GlobalCompositionRoot.Entry.cs:61:        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
.\Core\Logging\DebugUtility.cs:62:        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
```

Observacao: os 2 hits sao de `RuntimeInitializeOnLoadMethod` por substring de regex, nao de leak de `UnityEditor` fora de `Editor/**`.

### Gates canonicos
```text
Tools/Gates/Run-NewScripts-RgGates.ps1
PASS Gate A
PASS Gate A2
PASS Gate B
```

## SceneFlow Shared Contracts

### Definicao unica
```text
rg -n "interface\s+ISceneFlowIdSourceProvider<|struct\s+SceneFlowIdSourceResult|class\s+SceneFlowIdSourceUtility" Modules/SceneFlow/Editor -g "*.cs"
```

```text
Modules/SceneFlow/Editor\Drawers\TransitionStyleIdPropertyDrawer.cs:65:    internal interface ISceneFlowIdSourceProvider<TId>
Modules/SceneFlow/Editor\Drawers\TransitionStyleIdPropertyDrawer.cs:70:    internal readonly struct SceneFlowIdSourceResult
Modules/SceneFlow/Editor\Drawers\TransitionStyleIdPropertyDrawer.cs:82:    internal static class SceneFlowIdSourceUtility
```

### Prova de uso pelos drawers
```text
rg -n "ISceneFlowIdSourceProvider<|SceneFlowIdSourceResult|SceneFlowIdSourceUtility" Modules/SceneFlow/Editor -g "*.cs"
```

```text
Modules/SceneFlow/Editor\Drawers\TransitionStyleIdPropertyDrawer.cs:13:    internal sealed class TransitionStyleIdSourceProvider : ISceneFlowIdSourceProvider<TransitionStyleId>
Modules/SceneFlow/Editor\Drawers\SceneRouteIdPropertyDrawer.cs:13:    internal sealed class SceneRouteIdSourceProvider : ISceneFlowIdSourceProvider<SceneRouteId>
Modules/SceneFlow/Editor\Drawers\SceneFlowProfileIdPropertyDrawer.cs:14:    internal sealed class SceneFlowProfileIdSourceProvider : ISceneFlowIdSourceProvider<SceneFlowProfileId>
Modules/SceneFlow/Editor\Drawers\TransitionStyleIdPropertyDrawer.cs:173:            string currentNormalized = SceneFlowIdSourceUtility.Normalize(rawValueProperty.stringValue);
Modules/SceneFlow/Editor\Drawers\SceneRouteIdPropertyDrawer.cs:135:            string currentNormalized = SceneFlowIdSourceUtility.Normalize(rawValueProperty.stringValue);
Modules/SceneFlow/Editor\Drawers\SceneFlowProfileIdPropertyDrawer.cs:128:            string currentNormalized = SceneFlowIdSourceUtility.Normalize(rawValueProperty.stringValue);
```

## Editor-Only Utility Inventory

| File | Kind | Hooks | A1 | A2 | A3 | Decision | Reason |
|---|---|---|---|---|---|---|---|
| `Editor/Core/Logging/HardFailFastH1.Editor.cs` | partial editor bridge | none | FAIL | PASS (`0` asset refs for guid `b95a32c874ef1d60`) | PASS | KEEP | `HardFailFastH1` tem callsites runtime reais em Navigation, LevelFlow, WorldLifecycle e SceneFlow; deletar quebraria o partial editor stop-play hook. |
| `Editor/Infrastructure/Composition/GlobalCompositionRoot.Editor.cs` | partial editor bridge | none | FAIL | PASS (`0` asset refs for guid `d159cb4a2ef86307`) | PASS | KEEP | `GlobalCompositionRoot` continua integrado ao cluster runtime/composition e fornece helpers editor-only usados pelo partial root. |

### A1 evidence
```text
rg -n "HardFailFastH1|HardFailFastH1\.Editor" . -g "*.cs"
```

```text
.\Modules\Navigation\Runtime\MacroRestartCoordinator.cs:128:                        HardFailFastH1.Trigger(typeof(MacroRestartCoordinator),
.\Modules\Navigation\GameNavigationService.cs:82:                HardFailFastH1.Trigger(typeof(GameNavigationService),
.\Modules\WorldLifecycle\Runtime\WorldResetCommands.cs:104:            HardFailFastH1.Trigger(typeof(WorldResetCommands),
.\Core\Logging\HardFailFastH1.cs:6:    public static partial class HardFailFastH1
.\Editor\Core\Logging\HardFailFastH1.Editor.cs:6:    public static partial class HardFailFastH1
```

```text
rg -n "GlobalCompositionRoot|GlobalCompositionRoot\.Editor" . -g "*.cs"
```

```text
.\Editor\Infrastructure\Composition\GlobalCompositionRoot.Editor.cs:6:    public static partial class GlobalCompositionRoot
.\Infrastructure\Composition\GlobalCompositionRoot.Entry.cs:37:    public static partial class GlobalCompositionRoot
.\Infrastructure\Composition\GlobalCompositionRoot.SceneFlow.cs:20:    public static partial class GlobalCompositionRoot
.\Infrastructure\Composition\GlobalCompositionRoot.Navigation.cs:13:    public static partial class GlobalCompositionRoot
```

### A2 evidence
```text
rg -n "b95a32c874ef1d60" . -g "*.unity" -g "*.prefab" -g "*.asset"
0 matches
```

```text
rg -n "d159cb4a2ef86307" . -g "*.unity" -g "*.prefab" -g "*.asset"
0 matches
```

## Empty Folder Prune
- Nenhuma pasta vazia adicional foi encontrada dentro de `NewScripts/**/Editor/**`.
- Nenhum `folder.meta` orphan adicional foi removido neste batch.

## Post-checks

### Leak sweep estrito (post)
```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu|InitializeOnLoadMethod" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/QA/**" -g "!**/Legacy/**"
```

```text
.\Infrastructure\Composition\GlobalCompositionRoot.Entry.cs:61:        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
.\Core\Logging\DebugUtility.cs:62:        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
```

### Gates
```text
PASS Gate A
PASS Gate A2
PASS Gate B
```
