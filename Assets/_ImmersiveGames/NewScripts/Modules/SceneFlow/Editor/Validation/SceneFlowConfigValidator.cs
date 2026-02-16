using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Bindings;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.Validation
{
    public static class SceneFlowConfigValidator
    {
        private const string IntentCatalogPath = "Assets/Resources/GameNavigationIntentCatalog.asset";
        private const string NavigationCatalogPath = "Assets/Resources/Navigation/GameNavigationCatalog.asset";
        private const string SceneRouteCatalogPath = "Assets/Resources/SceneFlow/SceneRouteCatalog.asset";
        private const string TransitionStyleCatalogPath = "Assets/Resources/Navigation/TransitionStyleCatalog.asset";
        private const string TransitionProfileCatalogPath = "Assets/Resources/SceneFlow/SceneTransitionProfileCatalog.asset";
        private const string DefaultTransitionProfilePath = "Assets/Resources/SceneFlow/Profiles/DefaultTransitionProfile.asset";
        private const string LevelCatalogPath = "Assets/Resources/Navigation/LevelCatalog.asset";
        private const string RuntimeModeConfigPath = "Assets/Resources/RuntimeModeConfig.asset";
        private const string BootstrapConfigPath = "Assets/Resources/NewScriptsBootstrapConfig.asset";

        private const string ReportPath = "Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md";

        private static readonly NavigationIntentId MenuIntentId = NavigationIntentId.FromName("to-menu");
        private static readonly NavigationIntentId GameplayIntentId = NavigationIntentId.FromName("to-gameplay");

        [MenuItem("ImmersiveGames/NewScripts/Config/Validate SceneFlow Config (DataCleanup v1)", priority = 3010)]
        public static void ValidateDataCleanupV1()
        {
            ValidationContext context = new ValidationContext();

            LoadedAssets assets = LoadAssets(context);

            ValidateMandatoryCoreIntents(context, assets.IntentCatalog);
            ValidateGameNavigationCoreSlots(context, assets.NavigationCatalog);
            ValidateSceneRouteCatalogConsistency(context, assets.SceneRouteCatalog, assets.NavigationCatalog);
            ValidateCoreStylesAndProfiles(
                context,
                assets.NavigationCatalog,
                assets.TransitionStyleCatalog,
                assets.TransitionProfileCatalog,
                assets.BootstrapConfig);

            string reportContent = BuildReport(context);
            WriteReport(reportContent);
            AssetDatabase.Refresh();

            if (context.HasFatal)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config] SceneFlow validation failed. Check report: '{ReportPath}'.");
            }

            Debug.Log($"[SceneFlow][Validation] PASS. Report generated at: {ReportPath}");
        }

        private static LoadedAssets LoadAssets(ValidationContext context)
        {
            LoadedAssets assets = new LoadedAssets
            {
                IntentCatalog = LoadAssetAtPath<GameNavigationIntentCatalogAsset>(IntentCatalogPath, context),
                NavigationCatalog = LoadAssetAtPath<GameNavigationCatalogAsset>(NavigationCatalogPath, context),
                SceneRouteCatalog = LoadAssetAtPath<SceneRouteCatalogAsset>(SceneRouteCatalogPath, context),
                TransitionStyleCatalog = LoadAssetAtPath<TransitionStyleCatalogAsset>(TransitionStyleCatalogPath, context),
                TransitionProfileCatalog = LoadAssetAtPath<SceneTransitionProfileCatalogAsset>(TransitionProfileCatalogPath, context),
                DefaultTransitionProfile = LoadAssetAtPath<SceneTransitionProfile>(DefaultTransitionProfilePath, context),
                LevelCatalog = LoadAssetAtPath<LevelCatalogAsset>(LevelCatalogPath, context),
                RuntimeModeConfig = LoadAssetAtPath<RuntimeModeConfig>(RuntimeModeConfigPath, context),
                BootstrapConfig = LoadAssetAtPath<NewScriptsBootstrapConfigAsset>(BootstrapConfigPath, context)
            };

            return assets;
        }

        private static T LoadAssetAtPath<T>(string path, ValidationContext context) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            context.AssetStatuses.Add(new AssetStatus(path, asset != null));

            if (asset == null)
            {
                context.AddFatal($"Asset canônico não encontrado em path obrigatório: '{path}'.");
            }

            return asset;
        }

        private static void ValidateMandatoryCoreIntents(ValidationContext context, GameNavigationIntentCatalogAsset intentCatalog)
        {
            ValidateMandatoryIntentFromCatalog(context, intentCatalog, MenuIntentId);
            ValidateMandatoryIntentFromCatalog(context, intentCatalog, GameplayIntentId);
        }

        private static void ValidateMandatoryIntentFromCatalog(
            ValidationContext context,
            GameNavigationIntentCatalogAsset intentCatalog,
            NavigationIntentId targetIntentId)
        {
            CoreIntentRecord record = new CoreIntentRecord
            {
                IntentId = targetIntentId.Value,
                RouteRefName = "<missing>",
                RouteId = "<missing>"
            };

            if (intentCatalog == null)
            {
                record.Status = "FATAL";
                context.CoreMandatoryIntents.Add(record);
                return;
            }

            SerializedObject serializedObject = new SerializedObject(intentCatalog);
            SerializedProperty coreList = serializedObject.FindProperty("core");
            if (coreList == null || !coreList.isArray)
            {
                record.Status = "FATAL";
                context.AddFatal("GameNavigationIntentCatalogAsset sem bloco 'core' serializado.");
                context.CoreMandatoryIntents.Add(record);
                return;
            }

            SerializedProperty foundEntry = null;
            for (int i = 0; i < coreList.arraySize; i++)
            {
                SerializedProperty entry = coreList.GetArrayElementAtIndex(i);
                SerializedProperty intentIdProp = entry.FindPropertyRelative("intentId");
                string current = ReadTypedIdValue(intentIdProp);
                if (string.Equals(current, targetIntentId.Value, StringComparison.OrdinalIgnoreCase))
                {
                    foundEntry = entry;
                    break;
                }
            }

            if (foundEntry == null)
            {
                record.Status = "FATAL";
                context.AddFatal($"Intent core obrigatória ausente no GameNavigationIntentCatalogAsset: '{targetIntentId.Value}'.");
                context.CoreMandatoryIntents.Add(record);
                return;
            }

            SerializedProperty routeRefProp = foundEntry.FindPropertyRelative("routeRef");
            SceneRouteDefinitionAsset routeRef = routeRefProp != null
                ? routeRefProp.objectReferenceValue as SceneRouteDefinitionAsset
                : null;

            record.RouteRefName = routeRef != null ? routeRef.name : "<null>";
            record.RouteId = routeRef != null ? routeRef.RouteId.Value : "<invalid>";

            bool routeRefValid = routeRef != null;
            bool routeIdValid = routeRef != null && routeRef.RouteId.IsValid;

            if (!routeRefValid || !routeIdValid)
            {
                record.Status = "FATAL";
                context.AddFatal(
                    $"Intent core '{targetIntentId.Value}' inválida: routeRef obrigatório com RouteId válido.");
            }
            else
            {
                record.Status = "OK";
            }

            context.CoreMandatoryIntents.Add(record);
        }

        private static void ValidateGameNavigationCoreSlots(ValidationContext context, GameNavigationCatalogAsset navigationCatalog)
        {
            ValidateCoreSlot(context, navigationCatalog, "menuSlot", "menu");
            ValidateCoreSlot(context, navigationCatalog, "gameplaySlot", "gameplay");
        }

        private static void ValidateCoreSlot(
            ValidationContext context,
            GameNavigationCatalogAsset navigationCatalog,
            string slotPropertyName,
            string slotLabel)
        {
            CoreSlotRecord record = new CoreSlotRecord
            {
                Slot = slotLabel,
                RouteId = "<missing>",
                StyleId = "<missing>",
                Status = "FATAL"
            };

            if (navigationCatalog == null)
            {
                context.CoreSlots.Add(record);
                return;
            }

            SerializedObject serializedObject = new SerializedObject(navigationCatalog);
            SerializedProperty slotProp = serializedObject.FindProperty(slotPropertyName);
            if (slotProp == null)
            {
                context.AddFatal($"GameNavigationCatalogAsset sem slot '{slotPropertyName}'.");
                context.CoreSlots.Add(record);
                return;
            }

            SceneRouteDefinitionAsset routeRef = slotProp.FindPropertyRelative("routeRef")?.objectReferenceValue as SceneRouteDefinitionAsset;
            string styleIdValue = ReadTypedIdValue(slotProp.FindPropertyRelative("styleId"));
            bool styleValid = !string.IsNullOrWhiteSpace(styleIdValue);

            record.RouteId = routeRef != null ? routeRef.RouteId.Value : "<null-routeRef>";
            record.StyleId = styleValid ? styleIdValue : "<invalid-styleId>";

            bool routeRefValid = routeRef != null;
            bool routeIdValid = routeRef != null && routeRef.RouteId.IsValid;

            if (!routeRefValid || !styleValid || !routeIdValid)
            {
                context.AddFatal(
                    $"Core slot '{slotLabel}' inválido: exige routeRef != null, routeRef.RouteId válido e styleId válido.");
                record.Status = "FATAL";
            }
            else
            {
                record.Status = "OK";
            }

            context.CoreSlots.Add(record);
        }

        private static void ValidateSceneRouteCatalogConsistency(
            ValidationContext context,
            SceneRouteCatalogAsset sceneRouteCatalog,
            GameNavigationCatalogAsset navigationCatalog)
        {
            ValidateInlineRoutesAreEmpty(context, sceneRouteCatalog);
            DetectDuplicatedRouteIdsInSceneRouteCatalog(context, sceneRouteCatalog);

            SceneRouteId menuRouteId = GetCoreSlotRouteId(navigationCatalog, "menuSlot");
            SceneRouteId gameplayRouteId = GetCoreSlotRouteId(navigationCatalog, "gameplaySlot");

            ValidateRouteExistsInCatalog(context, sceneRouteCatalog, MenuIntentId.Value, menuRouteId);
            ValidateRouteExistsInCatalog(context, sceneRouteCatalog, GameplayIntentId.Value, gameplayRouteId);
        }

        private static void ValidateInlineRoutesAreEmpty(ValidationContext context, SceneRouteCatalogAsset sceneRouteCatalog)
        {
            if (sceneRouteCatalog == null)
            {
                context.InlineRoutesCount = -1;
                context.InlineRoutesStatus = "FATAL";
                return;
            }

            SerializedObject serializedObject = new SerializedObject(sceneRouteCatalog);
            SerializedProperty inlineRoutes = serializedObject.FindProperty("routes");
            int count = inlineRoutes != null && inlineRoutes.isArray ? inlineRoutes.arraySize : 0;

            context.InlineRoutesCount = count;
            context.InlineRoutesStatus = count == 0 ? "OK" : "FATAL";

            if (count > 0)
            {
                context.AddFatal(
                    $"SceneRouteCatalogAsset contém rotas inline legadas (routes[]). Use somente routeDefinitions e remova/migre routes[]. count={count}.");
            }
        }

        private static void DetectDuplicatedRouteIdsInSceneRouteCatalog(ValidationContext context, SceneRouteCatalogAsset sceneRouteCatalog)
        {
            if (sceneRouteCatalog == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(sceneRouteCatalog);
            Dictionary<string, int> seenRouteIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            SerializedProperty routeDefinitions = serializedObject.FindProperty("routeDefinitions");
            if (routeDefinitions != null && routeDefinitions.isArray)
            {
                for (int i = 0; i < routeDefinitions.arraySize; i++)
                {
                    SceneRouteDefinitionAsset routeAsset = routeDefinitions.GetArrayElementAtIndex(i).objectReferenceValue as SceneRouteDefinitionAsset;
                    if (routeAsset == null)
                    {
                        continue;
                    }

                    RegisterRouteIdOccurrence(context, seenRouteIds, routeAsset.RouteId.Value, $"routeDefinitions[{i}]");
                }
            }

            SerializedProperty inlineRoutes = serializedObject.FindProperty("routes");
            if (inlineRoutes != null && inlineRoutes.isArray)
            {
                for (int i = 0; i < inlineRoutes.arraySize; i++)
                {
                    SerializedProperty entry = inlineRoutes.GetArrayElementAtIndex(i);
                    string routeId = ReadTypedIdValue(entry.FindPropertyRelative("routeId"));
                    if (string.IsNullOrWhiteSpace(routeId))
                    {
                        continue;
                    }

                    RegisterRouteIdOccurrence(context, seenRouteIds, routeId, $"routes[{i}]");
                }
            }
        }

        private static void RegisterRouteIdOccurrence(
            ValidationContext context,
            IDictionary<string, int> seenRouteIds,
            string routeId,
            string source)
        {
            if (string.IsNullOrWhiteSpace(routeId))
            {
                return;
            }

            if (!seenRouteIds.TryGetValue(routeId, out int count))
            {
                seenRouteIds[routeId] = 1;
                return;
            }

            seenRouteIds[routeId] = count + 1;
            context.AddFatal($"RouteId duplicado no SceneRouteCatalogAsset: '{routeId}' (source='{source}').");
        }

        private static SceneRouteId GetCoreSlotRouteId(GameNavigationCatalogAsset navigationCatalog, string slotPropertyName)
        {
            if (navigationCatalog == null)
            {
                return SceneRouteId.None;
            }

            SerializedObject serializedObject = new SerializedObject(navigationCatalog);
            SerializedProperty slotProp = serializedObject.FindProperty(slotPropertyName);
            if (slotProp == null)
            {
                return SceneRouteId.None;
            }

            SceneRouteDefinitionAsset routeRef = slotProp.FindPropertyRelative("routeRef")?.objectReferenceValue as SceneRouteDefinitionAsset;
            return routeRef != null ? routeRef.RouteId : SceneRouteId.None;
        }

        private static void ValidateRouteExistsInCatalog(
            ValidationContext context,
            SceneRouteCatalogAsset sceneRouteCatalog,
            string intentId,
            SceneRouteId routeId)
        {
            if (sceneRouteCatalog == null)
            {
                return;
            }

            if (!routeId.IsValid)
            {
                context.AddFatal($"Intent '{intentId}' sem routeId válido para checagem no SceneRouteCatalog.");
                return;
            }

            bool exists;
            try
            {
                exists = sceneRouteCatalog.TryGet(routeId, out _);
            }
            catch (Exception ex)
            {
                context.AddFatal($"Falha ao validar SceneRouteCatalog.TryGet('{routeId}'): {ex.Message}");
                return;
            }

            if (!exists)
            {
                context.AddFatal($"SceneRouteCatalogAsset não possui rota para intent '{intentId}' (routeId='{routeId}').");
            }
        }

        private static void ValidateCoreStylesAndProfiles(
            ValidationContext context,
            GameNavigationCatalogAsset navigationCatalog,
            TransitionStyleCatalogAsset styleCatalog,
            SceneTransitionProfileCatalogAsset profileCatalog,
            NewScriptsBootstrapConfigAsset bootstrapConfig)
        {
            ValidateCoreSlotStyle(context, navigationCatalog, styleCatalog, profileCatalog, bootstrapConfig, "menuSlot", "menu");
            ValidateCoreSlotStyle(context, navigationCatalog, styleCatalog, profileCatalog, bootstrapConfig, "gameplaySlot", "gameplay");
        }

        private static void ValidateCoreSlotStyle(
            ValidationContext context,
            GameNavigationCatalogAsset navigationCatalog,
            TransitionStyleCatalogAsset styleCatalog,
            SceneTransitionProfileCatalogAsset profileCatalog,
            NewScriptsBootstrapConfigAsset bootstrapConfig,
            string slotPropertyName,
            string slotLabel)
        {
            if (navigationCatalog == null)
            {
                return;
            }

            SerializedObject navSerialized = new SerializedObject(navigationCatalog);
            SerializedProperty slotProp = navSerialized.FindProperty(slotPropertyName);
            if (slotProp == null)
            {
                context.AddFatal($"Slot core inexistente para validação de style: '{slotPropertyName}'.");
                return;
            }

            TransitionStyleId styleId = TransitionStyleId.FromName(ReadTypedIdValue(slotProp.FindPropertyRelative("styleId")));
            if (!styleId.IsValid)
            {
                context.AddFatal($"Slot core '{slotLabel}' sem styleId válido.");
                return;
            }

            if (styleCatalog == null)
            {
                return;
            }

            bool styleFound;
            TransitionStyleDefinition style;
            try
            {
                styleFound = styleCatalog.TryGet(styleId, out style);
            }
            catch (Exception ex)
            {
                context.AddFatal($"Falha ao resolver styleId '{styleId}' no TransitionStyleCatalogAsset: {ex.Message}");
                return;
            }

            if (!styleFound)
            {
                context.AddFatal($"TransitionStyleCatalogAsset não possui styleId usado no slot '{slotLabel}': '{styleId}'.");
                return;
            }

            bool hasProfileByRef = style.Profile != null;
            bool hasProfileByCatalog = false;

            if (!hasProfileByRef)
            {
                if (style.ProfileId.IsValid)
                {
                    if (profileCatalog == null)
                    {
                        context.AddFatal(
                            $"styleId '{styleId}' depende de profileId '{style.ProfileId}', mas SceneTransitionProfileCatalogAsset está ausente.");
                    }
                    else if (!profileCatalog.TryGetProfile(style.ProfileId, out SceneTransitionProfile profileRef) || profileRef == null)
                    {
                        context.AddFatal(
                            $"styleId '{styleId}' possui profileId '{style.ProfileId}' não resolvido no SceneTransitionProfileCatalogAsset.");
                    }
                    else
                    {
                        hasProfileByCatalog = true;
                    }
                }
                else
                {
                    context.AddFatal($"styleId '{styleId}' sem transitionProfile e sem profileId válido.");
                }
            }

            if (style.UseFade && !HasFadeSceneKeyConfigured(bootstrapConfig))
            {
                context.AddWarn(
                    $"Slot '{slotLabel}' usa styleId '{styleId}' com UseFade=true, mas bootstrapConfig.fadeSceneKey não está configurado.");
            }

            if (hasProfileByRef || hasProfileByCatalog)
            {
                return;
            }
        }

        private static bool HasFadeSceneKeyConfigured(NewScriptsBootstrapConfigAsset bootstrapConfig)
        {
            if (bootstrapConfig == null)
            {
                return false;
            }

            SerializedObject serializedObject = new SerializedObject(bootstrapConfig);
            SerializedProperty fadeSceneKeyProp = serializedObject.FindProperty("fadeSceneKey");
            return fadeSceneKeyProp != null && fadeSceneKeyProp.objectReferenceValue != null;
        }

        private static string BuildReport(ValidationContext context)
        {
            StringBuilder sb = new StringBuilder(2048);
            sb.AppendLine("# SceneFlow Config Validation Report (DataCleanup v1)");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"- Unity version: {Application.unityVersion}");
            sb.AppendLine();

            sb.AppendLine("## Assets canônicos");
            sb.AppendLine();
            sb.AppendLine("| Path | Status |");
            sb.AppendLine("|---|---|");
            for (int i = 0; i < context.AssetStatuses.Count; i++)
            {
                AssetStatus status = context.AssetStatuses[i];
                sb.AppendLine($"| `{status.Path}` | {(status.Exists ? "OK" : "NOT FOUND")} |");
            }
            sb.AppendLine();

            sb.AppendLine("## Core mandatory intents");
            sb.AppendLine();
            sb.AppendLine("| intentId | routeRef | routeId | status |");
            sb.AppendLine("|---|---|---|---|");
            for (int i = 0; i < context.CoreMandatoryIntents.Count; i++)
            {
                CoreIntentRecord record = context.CoreMandatoryIntents[i];
                sb.AppendLine($"| `{record.IntentId}` | `{record.RouteRefName}` | `{record.RouteId}` | {record.Status} |");
            }
            sb.AppendLine();

            sb.AppendLine("## Core slots");
            sb.AppendLine();
            sb.AppendLine("| slot | routeId | styleId | status |");
            sb.AppendLine("|---|---|---|---|");
            for (int i = 0; i < context.CoreSlots.Count; i++)
            {
                CoreSlotRecord record = context.CoreSlots[i];
                sb.AppendLine($"| `{record.Slot}` | `{record.RouteId}` | `{record.StyleId}` | {record.Status} |");
            }
            sb.AppendLine();

            sb.AppendLine("## Inline routes policy");
            sb.AppendLine();
            sb.AppendLine($"- Inline routes (routes[]) count: {context.InlineRoutesCount}");
            sb.AppendLine($"- Status: {context.InlineRoutesStatus}");
            sb.AppendLine();

            sb.AppendLine("## Problems");
            sb.AppendLine();
            sb.AppendLine("### FATAL");
            if (context.Fatals.Count == 0)
            {
                sb.AppendLine("- None");
            }
            else
            {
                for (int i = 0; i < context.Fatals.Count; i++)
                {
                    sb.AppendLine($"- {context.Fatals[i]}");
                }
            }
            sb.AppendLine();

            sb.AppendLine("### WARN");
            if (context.Warnings.Count == 0)
            {
                sb.AppendLine("- None");
            }
            else
            {
                for (int i = 0; i < context.Warnings.Count; i++)
                {
                    sb.AppendLine($"- {context.Warnings[i]}");
                }
            }
            sb.AppendLine();

            sb.AppendLine($"VERDICT: {(context.HasFatal ? "FAIL" : "PASS")}");

            return sb.ToString();
        }

        private static void WriteReport(string reportContent)
        {
            string absolutePath = Path.GetFullPath(ReportPath);
            string directory = Path.GetDirectoryName(absolutePath);

            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(absolutePath, reportContent, Encoding.UTF8);
        }

        private static string ReadTypedIdValue(SerializedProperty typedIdProperty)
        {
            if (typedIdProperty == null)
            {
                return string.Empty;
            }

            SerializedProperty rawValueProperty = typedIdProperty.FindPropertyRelative("_value");
            if (rawValueProperty == null)
            {
                return string.Empty;
            }

            return string.IsNullOrWhiteSpace(rawValueProperty.stringValue)
                ? string.Empty
                : rawValueProperty.stringValue.Trim().ToLowerInvariant();
        }

        private sealed class ValidationContext
        {
            public readonly List<AssetStatus> AssetStatuses = new List<AssetStatus>();
            public readonly List<CoreIntentRecord> CoreMandatoryIntents = new List<CoreIntentRecord>();
            public readonly List<CoreSlotRecord> CoreSlots = new List<CoreSlotRecord>();
            public readonly List<string> Fatals = new List<string>();
            public readonly List<string> Warnings = new List<string>();
            public int InlineRoutesCount;
            public string InlineRoutesStatus = "UNKNOWN";

            public bool HasFatal => Fatals.Count > 0;

            public void AddFatal(string message)
            {
                Fatals.Add(message);
            }

            public void AddWarn(string message)
            {
                Warnings.Add(message);
            }
        }

        private sealed class LoadedAssets
        {
            public GameNavigationIntentCatalogAsset IntentCatalog;
            public GameNavigationCatalogAsset NavigationCatalog;
            public SceneRouteCatalogAsset SceneRouteCatalog;
            public TransitionStyleCatalogAsset TransitionStyleCatalog;
            public SceneTransitionProfileCatalogAsset TransitionProfileCatalog;
            public SceneTransitionProfile DefaultTransitionProfile;
            public LevelCatalogAsset LevelCatalog;
            public RuntimeModeConfig RuntimeModeConfig;
            public NewScriptsBootstrapConfigAsset BootstrapConfig;
        }

        private readonly struct AssetStatus
        {
            public AssetStatus(string path, bool exists)
            {
                Path = path;
                Exists = exists;
            }

            public string Path { get; }
            public bool Exists { get; }
        }

        private sealed class CoreIntentRecord
        {
            public string IntentId;
            public string RouteRefName;
            public string RouteId;
            public string Status;
        }

        private sealed class CoreSlotRecord
        {
            public string Slot;
            public string RouteId;
            public string StyleId;
            public string Status;
        }
    }
}
