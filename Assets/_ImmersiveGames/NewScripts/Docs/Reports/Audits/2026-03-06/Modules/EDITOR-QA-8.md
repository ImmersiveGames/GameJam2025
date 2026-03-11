# EDITOR-QA-8

## Summary
- Problema: drawers editor-only de SceneFlow ficaram com CS0246 apos o prune dos contratos compartilhados `ISceneFlowIdSourceProvider<>`, `SceneFlowIdSourceResult` e `SceneFlowIdSourceUtility`.
- Solucao: reintroduzimos os 3 tipos shared uma unica vez dentro de `Modules/SceneFlow/Editor/Drawers/TransitionStyleIdPropertyDrawer.cs`, no namespace `_ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.IdSources`.
- Ajuste complementar: os 3 providers de SceneFlow passaram para `internal sealed` para manter acessibilidade consistente com os contratos editor-only restaurados.
- Escopo: somente `Assets/_ImmersiveGames/NewScripts/**`.
- Confirmacao: nao toquei em `Assets/_ImmersiveGames/Scripts/**`.

## Static Checks

### Prova de existencia dos tipos
```text
rg -n "internal interface ISceneFlowIdSourceProvider|internal readonly struct SceneFlowIdSourceResult|internal static class SceneFlowIdSourceUtility" Modules/SceneFlow/Editor/Drawers/TransitionStyleIdPropertyDrawer.cs
```

```text
65:    internal interface ISceneFlowIdSourceProvider<TId>
70:    internal readonly struct SceneFlowIdSourceResult
82:    internal static class SceneFlowIdSourceUtility
```

### Prova de referencias dos drawers
```text
rg -n "ISceneFlowIdSourceProvider<|SceneFlowIdSourceResult|SceneFlowIdSourceUtility" Modules/SceneFlow/Editor/Drawers -g "*.cs"
```

```text
Modules/SceneFlow/Editor/Drawers\TransitionStyleIdPropertyDrawer.cs:13:    internal sealed class TransitionStyleIdSourceProvider : ISceneFlowIdSourceProvider<TransitionStyleId>
Modules/SceneFlow/Editor/Drawers\TransitionStyleIdPropertyDrawer.cs:15:        public SceneFlowIdSourceResult Collect()
Modules/SceneFlow/Editor/Drawers\TransitionStyleIdPropertyDrawer.cs:47:                    SceneFlowIdSourceUtility.AddAndTrackDuplicate(values, duplicates, raw.stringValue);
Modules/SceneFlow/Editor/Drawers\TransitionStyleIdPropertyDrawer.cs:51:            return SceneFlowIdSourceUtility.BuildResult(values, duplicates);
Modules/SceneFlow/Editor/Drawers\TransitionStyleIdPropertyDrawer.cs:65:    internal interface ISceneFlowIdSourceProvider<TId>
Modules/SceneFlow/Editor/Drawers\TransitionStyleIdPropertyDrawer.cs:67:        SceneFlowIdSourceResult Collect();
Modules/SceneFlow/Editor/Drawers\TransitionStyleIdPropertyDrawer.cs:70:    internal readonly struct SceneFlowIdSourceResult
Modules/SceneFlow/Editor/Drawers\TransitionStyleIdPropertyDrawer.cs:75:        public SceneFlowIdSourceResult(IReadOnlyList<string> values, IReadOnlyList<string> duplicateValues)
Modules/SceneFlow/Editor/Drawers\TransitionStyleIdPropertyDrawer.cs:82:    internal static class SceneFlowIdSourceUtility
Modules/SceneFlow/Editor/Drawers\TransitionStyleIdPropertyDrawer.cs:118:        public static SceneFlowIdSourceResult BuildResult(IEnumerable<string> values, IEnumerable<string> duplicates)
Modules/SceneFlow/Editor/Drawers\TransitionStyleIdPropertyDrawer.cs:149:            return new SceneFlowIdSourceResult(v, d);
Modules/SceneFlow/Editor/Drawers\TransitionStyleIdPropertyDrawer.cs:173:            string currentNormalized = SceneFlowIdSourceUtility.Normalize(rawValueProperty.stringValue);
Modules/SceneFlow/Editor/Drawers\TransitionStyleIdPropertyDrawer.cs:240:            string currentNormalized = SceneFlowIdSourceUtility.Normalize(rawValueProperty.stringValue);
Modules/SceneFlow/Editor/Drawers\TransitionStyleIdPropertyDrawer.cs:373:                    SceneFlowIdSourceResult source = Provider.Collect();
Modules/SceneFlow/Editor/Drawers\SceneRouteIdPropertyDrawer.cs:13:    internal sealed class SceneRouteIdSourceProvider : ISceneFlowIdSourceProvider<SceneRouteId>
Modules/SceneFlow/Editor/Drawers\SceneRouteIdPropertyDrawer.cs:15:        public SceneFlowIdSourceResult Collect()
Modules/SceneFlow/Editor/Drawers\SceneRouteIdPropertyDrawer.cs:23:            return SceneFlowIdSourceUtility.BuildResult(allValues, duplicates);
Modules/SceneFlow/Editor/Drawers\SceneRouteIdPropertyDrawer.cs:41:                if (!SceneFlowIdSourceUtility.AddAndTrackDuplicate(routeDefinitionIds, duplicates, routeId))
Modules/SceneFlow/Editor/Drawers\SceneRouteIdPropertyDrawer.cs:46:                SceneFlowIdSourceUtility.AddValue(allValues, routeId);
Modules/SceneFlow/Editor/Drawers\SceneRouteIdPropertyDrawer.cs:84:                SceneFlowIdSourceUtility.AddValue(allValues, routeAsset.RouteId.Value);
Modules/SceneFlow/Editor/Drawers\SceneRouteIdPropertyDrawer.cs:109:                SceneFlowIdSourceUtility.AddAndTrackDuplicate(inlineRouteIds, duplicates, value);
Modules/SceneFlow/Editor/Drawers\SceneRouteIdPropertyDrawer.cs:110:                SceneFlowIdSourceUtility.AddValue(allValues, value);
Modules/SceneFlow/Editor/Drawers\SceneRouteIdPropertyDrawer.cs:135:            string currentNormalized = SceneFlowIdSourceUtility.Normalize(rawValueProperty.stringValue);
Modules/SceneFlow/Editor/Drawers\SceneRouteIdPropertyDrawer.cs:188:            string currentNormalized = SceneFlowIdSourceUtility.Normalize(rawValueProperty.stringValue);
Modules/SceneFlow/Editor/Drawers\SceneRouteIdPropertyDrawer.cs:295:                    SceneFlowIdSourceResult source = Provider.Collect();
Modules/SceneFlow/Editor/Drawers\SceneFlowProfileIdPropertyDrawer.cs:14:    internal sealed class SceneFlowProfileIdSourceProvider : ISceneFlowIdSourceProvider<SceneFlowProfileId>
Modules/SceneFlow/Editor/Drawers\SceneFlowProfileIdPropertyDrawer.cs:16:        public SceneFlowIdSourceResult Collect()
Modules/SceneFlow/Editor/Drawers\SceneFlowProfileIdPropertyDrawer.cs:25:            return SceneFlowIdSourceUtility.BuildResult(allValues, duplicates);
Modules/SceneFlow/Editor/Drawers\SceneFlowProfileIdPropertyDrawer.cs:30:            SceneFlowIdSourceUtility.AddValue(allValues, SceneFlowProfileId.Startup.Value);
Modules/SceneFlow/Editor/Drawers\SceneFlowProfileIdPropertyDrawer.cs:31:            SceneFlowIdSourceUtility.AddValue(allValues, SceneFlowProfileId.Frontend.Value);
Modules/SceneFlow/Editor/Drawers\SceneFlowProfileIdPropertyDrawer.cs:32:            SceneFlowIdSourceUtility.AddValue(allValues, SceneFlowProfileId.Gameplay.Value);
Modules/SceneFlow/Editor/Drawers\SceneFlowProfileIdPropertyDrawer.cs:64:                    SceneFlowIdSourceUtility.AddValue(allValues, raw.stringValue);
Modules/SceneFlow/Editor/Drawers\SceneFlowProfileIdPropertyDrawer.cs:101:                    SceneFlowIdSourceUtility.AddAndTrackDuplicate(profileCatalogIds, duplicates, value);
Modules/SceneFlow/Editor/Drawers\SceneFlowProfileIdPropertyDrawer.cs:102:                    SceneFlowIdSourceUtility.AddValue(allValues, value);
Modules/SceneFlow/Editor/Drawers\SceneFlowProfileIdPropertyDrawer.cs:128:            string currentNormalized = SceneFlowIdSourceUtility.Normalize(rawValueProperty.stringValue);
Modules/SceneFlow/Editor/Drawers\SceneFlowProfileIdPropertyDrawer.cs:195:            string currentNormalized = SceneFlowIdSourceUtility.Normalize(rawValueProperty.stringValue);
Modules/SceneFlow/Editor/Drawers\SceneFlowProfileIdPropertyDrawer.cs:328:                    SceneFlowIdSourceResult source = Provider.Collect();
```

### Leak sweep
```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/QA/**" -g "!**/Legacy/**"
```

```text
0 matches
```