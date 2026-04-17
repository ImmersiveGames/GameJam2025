using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
using _ImmersiveGames.NewScripts.SceneFlow.Authoring.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.NavigationDispatch.NavigationMacro;
using ImmersiveGames.GameJam2025.Modules.Navigation;
using UnityEditor;
using UnityEngine;

namespace ImmersiveGames.GameJam2025.Modules.SceneFlow.Editor.Validation
{
    public static class SceneFlowConfigValidator
    {
        private const string NavigationCatalogPath = "Assets/Resources/Navigation/GameNavigationCatalog.asset";
        private const string BootstrapConfigPath = "Assets/Resources/BootstrapConfig.asset";
        private const string ReportPath = "Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-Config-ValidationReport.md";

        [MenuItem("ImmersiveGames/NewScripts/Tools/SceneFlow/Validate Config", priority = 1410)]
        public static void ValidateDataCleanupV1()
        {
            ValidationContext context = new ValidationContext();
            LoadedAssets assets = LoadAssets(context);

            HashSet<string> knownRouteIds = ValidateSceneRouteDefinitions(context);
            ValidateMandatoryCoreIntents(context);
            if (assets.NavigationCatalog != null)
            {
                ValidateGameNavigationCoreSlots(context, assets.NavigationCatalog, knownRouteIds);
            }

            if (assets.BootstrapConfig != null)
            {
                ValidateBootstrapConfig(context, assets.BootstrapConfig);
                ValidateAllTransitionStyleAssets(context, assets.BootstrapConfig);
            }

            string reportContent = BuildReport(context);
            WriteReport(reportContent);
            AssetDatabase.Refresh();

            if (context.HasFatal)
            {
                throw new InvalidOperationException($"[FATAL][Config] SceneFlow validation failed. Check report: '{ReportPath}'.");
            }
        }

        private static LoadedAssets LoadAssets(ValidationContext context)
        {
            return new LoadedAssets
            {
                NavigationCatalog = LoadRequiredAssetAtPath<GameNavigationCatalogAsset>(NavigationCatalogPath, context),
                BootstrapConfig = LoadRequiredAssetAtPath<BootstrapConfigAsset>(BootstrapConfigPath, context)
            };
        }

        private static HashSet<string> ValidateSceneRouteDefinitions(ValidationContext context)
        {
            HashSet<string> seenRouteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] guids = AssetDatabase.FindAssets("t:SceneRouteDefinitionAsset");

            if (guids.Length == 0)
            {
                context.AddFatal("Nenhum SceneRouteDefinitionAsset encontrado.");
                return seenRouteIds;
            }

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                SceneRouteDefinitionAsset asset = AssetDatabase.LoadAssetAtPath<SceneRouteDefinitionAsset>(path);
                context.AssetStatuses.Add(new AssetStatus(path, asset != null));

                if (asset == null)
                {
                    continue;
                }

                if (!asset.RouteId.IsValid)
                {
                    context.AddFatal($"SceneRouteDefinitionAsset invalida sem RouteId. asset='{asset.name}', path='{path}'.");
                    context.RouteDefinitions.Add(new RouteDefinitionRecord
                    {
                        AssetName = asset.name,
                        RouteId = "<invalid>",
                        RouteKind = asset.RouteKind.ToString(),
                        Status = "FATAL"
                    });
                    continue;
                }

                bool routeValid = true;
                if (!seenRouteIds.Add(asset.RouteId.Value))
                {
                    context.AddFatal($"RouteId duplicado entre SceneRouteDefinitionAsset: '{asset.RouteId.Value}'.");
                    routeValid = false;
                }

                try
                {
                    asset.ValidateRoutePolicyOrFailFast();
                }
                catch (Exception ex)
                {
                    context.AddFatal($"SceneRouteDefinitionAsset invalida. asset='{asset.name}', routeId='{asset.RouteId}', detail='{ex.Message}'.");
                    routeValid = false;
                }

                context.RouteDefinitions.Add(new RouteDefinitionRecord
                {
                    AssetName = asset.name,
                    RouteId = asset.RouteId.Value,
                    RouteKind = asset.RouteKind.ToString(),
                    Status = routeValid ? "OK" : "FATAL"
                });
            }

            return seenRouteIds;
        }

        private static T LoadRequiredAssetAtPath<T>(string path, ValidationContext context) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            context.AssetStatuses.Add(new AssetStatus(path, asset != null));
            if (asset == null)
            {
                context.AddFatal($"Asset canonico nao encontrado em path obrigatorio: '{path}'.");
            }

            return asset;
        }

        private static void ValidateMandatoryCoreIntents(ValidationContext context)
        {
            foreach (NavigationIntentId intent in GameNavigationIntents.RequiredCore)
            {
                context.CoreMandatoryIntents.Add(new CoreIntentRecord { IntentId = intent.Value, Status = intent.IsValid ? "OK" : "FATAL" });
                if (!intent.IsValid)
                {
                    context.AddFatal("Intent core obrigatoria invalida na fonte canonica em codigo.");
                }
            }
        }

        private static void ValidateGameNavigationCoreSlots(
            ValidationContext context,
            GameNavigationCatalogAsset navigationCatalog,
            HashSet<string> knownRouteIds)
        {
            ValidateCoreSlot(context, navigationCatalog, "menuSlot", "menu", knownRouteIds);
            ValidateCoreSlot(context, navigationCatalog, "gameplaySlot", "gameplay", knownRouteIds);
        }

        private static void ValidateCoreSlot(
            ValidationContext context,
            GameNavigationCatalogAsset navigationCatalog,
            string slotPropertyName,
            string slotLabel,
            HashSet<string> knownRouteIds)
        {
            SerializedObject serializedObject = new SerializedObject(navigationCatalog);
            SerializedProperty slotProp = serializedObject.FindProperty(slotPropertyName);
            SceneRouteDefinitionAsset routeRef = slotProp?.FindPropertyRelative("routeRef")?.objectReferenceValue as SceneRouteDefinitionAsset;
            TransitionStyleAsset styleRef = slotProp?.FindPropertyRelative("transitionStyleRef")?.objectReferenceValue as TransitionStyleAsset;

            bool hasRoute = routeRef != null && routeRef.RouteId.IsValid && knownRouteIds.Contains(routeRef.RouteId.Value);
            bool hasStyle = styleRef != null && styleRef.Profile != null;
            bool ok = hasRoute && hasStyle;

            context.CoreSlots.Add(new CoreSlotRecord
            {
                Slot = slotLabel,
                RouteId = routeRef != null ? routeRef.RouteId.Value : "<missing>",
                StyleRef = styleRef != null ? styleRef.name : "<missing>",
                ProfileRef = styleRef != null && styleRef.Profile != null ? styleRef.Profile.name : "<missing>",
                Status = ok ? "OK" : "FATAL"
            });

            if (!ok)
            {
                context.AddFatal($"Core slot '{slotLabel}' exige routeRef valido e transitionStyleRef com profileRef valido.");
            }
        }

        private static void ValidateBootstrapConfig(ValidationContext context, BootstrapConfigAsset bootstrapConfig)
        {
            if (bootstrapConfig.NavigationCatalog == null)
            {
                context.AddFatal("Bootstrap sem navigationCatalog valido.");
            }

            ValidateStartupTransition(context, bootstrapConfig);
        }

        private static void ValidateStartupTransition(ValidationContext context, BootstrapConfigAsset bootstrapConfig)
        {
            TransitionStyleAsset startupStyle = bootstrapConfig.StartupTransitionStyleRef;
            if (startupStyle == null || startupStyle.Profile == null)
            {
                context.AddFatal("Bootstrap sem startupTransitionStyleRef valido.");
                return;
            }

            if (bootstrapConfig.LoadingHudSceneKey == null || string.IsNullOrWhiteSpace(bootstrapConfig.LoadingHudSceneKey.SceneName))
            {
                context.AddFatal("Bootstrap sem loadingHudSceneKey valido.");
            }

            if (startupStyle.UseFade && (bootstrapConfig.FadeSceneKey == null || string.IsNullOrWhiteSpace(bootstrapConfig.FadeSceneKey.SceneName)))
            {
                context.AddWarn("startupTransitionStyleRef usa fade, mas bootstrapConfig.fadeSceneKey nao esta configurado.");
            }
        }

        private static void ValidateAllTransitionStyleAssets(ValidationContext context, BootstrapConfigAsset bootstrapConfig)
        {
            string[] guids = AssetDatabase.FindAssets("t:TransitionStyleAsset");
            if (guids.Length == 0)
            {
                context.AddFatal("Nenhum TransitionStyleAsset encontrado.");
                return;
            }

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                TransitionStyleAsset styleAsset = AssetDatabase.LoadAssetAtPath<TransitionStyleAsset>(path);
                if (styleAsset == null) continue;

                bool hasFatal = false;
                if (styleAsset.Profile == null)
                {
                    context.AddFatal($"TransitionStyleAsset sem profileRef obrigatorio. asset='{styleAsset.name}'.");
                    hasFatal = true;
                }

                if (styleAsset.UseFade && (bootstrapConfig.FadeSceneKey == null || string.IsNullOrWhiteSpace(bootstrapConfig.FadeSceneKey.SceneName)))
                {
                    context.AddWarn($"TransitionStyleAsset '{styleAsset.name}' usa fade, mas bootstrapConfig.fadeSceneKey nao esta configurado.");
                }

                context.Styles.Add(new StyleValidationRecord
                {
                    StyleAsset = styleAsset.name,
                    UseFade = styleAsset.UseFade,
                    ProfileRef = styleAsset.Profile != null ? styleAsset.Profile.name : "<null>",
                    Status = hasFatal ? "FATAL" : "OK"
                });
            }
        }

        private static string BuildReport(ValidationContext context)
        {
            StringBuilder sb = new StringBuilder(2048);
            sb.AppendLine("# SceneFlow Config Validation Report");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"- Unity version: {Application.unityVersion}");
            sb.AppendLine();
            sb.AppendLine("## Assets canonicos");
            sb.AppendLine();
            sb.AppendLine("| Path | Status |");
            sb.AppendLine("|---|---|");
            foreach (AssetStatus status in context.AssetStatuses)
            {
                sb.AppendLine($"| `{status.Path}` | {(status.Exists ? "OK" : "NOT FOUND")} |");
            }

            sb.AppendLine();
            sb.AppendLine("## Scene route definitions");
            sb.AppendLine();
            sb.AppendLine("| asset | routeId | routeKind | status |");
            sb.AppendLine("|---|---|---|---|");
            foreach (RouteDefinitionRecord record in context.RouteDefinitions)
            {
                sb.AppendLine($"| `{record.AssetName}` | `{record.RouteId}` | `{record.RouteKind}` | {record.Status} |");
            }

            sb.AppendLine();
            sb.AppendLine("## Core mandatory intents");
            sb.AppendLine();
            sb.AppendLine("| intentId | status |");
            sb.AppendLine("|---|---|");
            foreach (CoreIntentRecord record in context.CoreMandatoryIntents)
            {
                sb.AppendLine($"| `{record.IntentId}` | {record.Status} |");
            }

            sb.AppendLine();
            sb.AppendLine("## Core slots");
            sb.AppendLine();
            sb.AppendLine("| slot | routeId | styleRef | profileRef | status |");
            sb.AppendLine("|---|---|---|---|---|");
            foreach (CoreSlotRecord record in context.CoreSlots)
            {
                sb.AppendLine($"| `{record.Slot}` | `{record.RouteId}` | `{record.StyleRef}` | `{record.ProfileRef}` | {record.Status} |");
            }

            sb.AppendLine();
            sb.AppendLine("## Transition styles");
            sb.AppendLine();
            sb.AppendLine("| styleAsset | useFade | profileRef | status |");
            sb.AppendLine("|---|---|---|---|");
            foreach (StyleValidationRecord record in context.Styles)
            {
                sb.AppendLine($"| `{record.StyleAsset}` | `{record.UseFade}` | `{record.ProfileRef}` | {record.Status} |");
            }

            sb.AppendLine();
            sb.AppendLine("### FATAL");
            if (context.Fatals.Count == 0) sb.AppendLine("- None"); else foreach (string fatal in context.Fatals) sb.AppendLine($"- {fatal}");
            sb.AppendLine();
            sb.AppendLine("### WARN");
            if (context.Warnings.Count == 0) sb.AppendLine("- None"); else foreach (string warn in context.Warnings) sb.AppendLine($"- {warn}");
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

        private sealed class ValidationContext
        {
            public readonly List<AssetStatus> AssetStatuses = new List<AssetStatus>();
            public readonly List<RouteDefinitionRecord> RouteDefinitions = new List<RouteDefinitionRecord>();
            public readonly List<CoreIntentRecord> CoreMandatoryIntents = new List<CoreIntentRecord>();
            public readonly List<CoreSlotRecord> CoreSlots = new List<CoreSlotRecord>();
            public readonly List<StyleValidationRecord> Styles = new List<StyleValidationRecord>();
            public readonly List<string> Fatals = new List<string>();
            public readonly List<string> Warnings = new List<string>();
            public bool HasFatal => Fatals.Count > 0;
            public void AddFatal(string message) => Fatals.Add(message);
            public void AddWarn(string message) => Warnings.Add(message);
        }

        private sealed class LoadedAssets
        {
            public GameNavigationCatalogAsset NavigationCatalog;
            public BootstrapConfigAsset BootstrapConfig;
        }

        private readonly struct AssetStatus
        {
            public AssetStatus(string path, bool exists) { Path = path; Exists = exists; }
            public string Path { get; }
            public bool Exists { get; }
        }

        private sealed class RouteDefinitionRecord
        {
            public string AssetName;
            public string RouteId;
            public string RouteKind;
            public string Status;
        }

        private sealed class CoreIntentRecord { public string IntentId; public string Status; }
        private sealed class CoreSlotRecord { public string Slot; public string RouteId; public string StyleRef; public string ProfileRef; public string Status; }
        private sealed class StyleValidationRecord { public string StyleAsset; public bool UseFade; public string ProfileRef; public string Status; }
    }
}

