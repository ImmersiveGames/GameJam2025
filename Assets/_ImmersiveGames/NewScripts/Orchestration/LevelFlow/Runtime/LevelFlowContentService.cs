using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using System.Collections.Generic;
using System.Text;

namespace _ImmersiveGames.NewScripts.Orchestration.LevelFlow.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LevelFlowContentService : ILevelFlowContentService
    {
        public LevelCollectionAsset ResolveLevelCollectionOrFail(SceneRouteDefinitionAsset routeAsset, SceneRouteId macroRouteId, string signature, string reason)
        {
            if (routeAsset == null)
            {
                FailFastConfig(macroRouteId, SceneRouteKind.Unspecified, signature, reason, "Gameplay route asset is null.");
            }

            if (routeAsset.RouteKind != SceneRouteKind.Gameplay)
            {
                FailFastConfig(macroRouteId, routeAsset.RouteKind, signature, reason, "LevelFlow content contract invoked for non-gameplay route.");
            }

            if (routeAsset.RouteId != macroRouteId)
            {
                FailFastConfig(macroRouteId, routeAsset.RouteKind, signature, reason, $"RouteRef mismatch routeRefRouteId='{routeAsset.RouteId}'.");
            }

            if (routeAsset.LevelCollection == null)
            {
                FailFastConfig(macroRouteId, routeAsset.RouteKind, signature, reason, "Gameplay route without LevelCollection.");
            }

            if (!routeAsset.LevelCollection.TryValidateRuntime(out string collectionError))
            {
                FailFastConfig(macroRouteId, routeAsset.RouteKind, signature, reason, $"Gameplay route LevelCollection invalid. detail='{collectionError}'.");
            }

            return routeAsset.LevelCollection;
        }

        public GameplayContentManifest ResolveGameplayContentManifestOrFail(
            LevelDefinitionAsset levelRef,
            SceneRouteId macroRouteId,
            string signature,
            string reason)
        {
            if (levelRef == null)
            {
                FailFastConfig(macroRouteId, SceneRouteKind.Gameplay, signature, reason, "LevelRef is null.");
            }

            levelRef.ValidateOrFailFast($"GameplayContentManifest routeId='{macroRouteId}' reason='{reason}'");

            GameplayContentManifest manifest = levelRef.ContentManifest;
            if (manifest == null)
            {
                FailFastConfig(macroRouteId, SceneRouteKind.Gameplay, signature, reason, $"Gameplay content manifest is null for levelRef='{levelRef.name}'.");
            }

            if (!manifest.TryValidateRuntime(out string manifestError))
            {
                FailFastConfig(macroRouteId, SceneRouteKind.Gameplay, signature, reason, $"Gameplay content manifest invalid for levelRef='{levelRef.name}'. detail='{manifestError}'.");
            }

            LogGameplayContentManifestAccepted(levelRef, manifest);

            return manifest;
        }

        public LevelDefinitionAsset ResolveSelectedLevelDefinitionOrFail(
            LevelCollectionAsset levelCollection,
            bool useSnapshot,
            GameplayStartSnapshot snapshot,
            SceneRouteId macroRouteId,
            SceneRouteKind routeKind,
            string signature,
            string reason)
        {
            if (levelCollection == null)
            {
                FailFastConfig(macroRouteId, routeKind, signature, reason, "LevelCollection is null.");
            }

            if (useSnapshot && snapshot.LevelRef != null)
            {
                return snapshot.LevelRef;
            }

            LevelDefinitionAsset defaultLevel = levelCollection.GetDefaultOrNull();
            if (defaultLevel == null)
            {
                FailFastConfig(macroRouteId, routeKind, signature, reason, "Gameplay route LevelCollection default (index 0) is missing.");
            }

            return defaultLevel;
        }

        public LevelDefinitionAsset ResolveNextLevelOrFail(GameplayStartSnapshot snapshot, string reason)
        {
            if (!TryValidateSnapshot(snapshot, out string detail))
            {
                FailFastConfig(snapshot.MacroRouteId, SceneRouteKind.Gameplay, BuildLevelSignature(snapshot.LevelRef, snapshot.MacroRouteId, reason), reason, detail);
            }

            LevelCollectionAsset collection = snapshot.MacroRouteRef.LevelCollection;
            if (collection == null)
            {
                FailFastConfig(snapshot.MacroRouteId, snapshot.MacroRouteRef.RouteKind, BuildLevelSignature(snapshot.LevelRef, snapshot.MacroRouteId, reason), reason, "LevelCollection is null.");
            }

            if (!collection.TryValidateRuntime(out string collectionError))
            {
                FailFastConfig(snapshot.MacroRouteId, snapshot.MacroRouteRef.RouteKind, BuildLevelSignature(snapshot.LevelRef, snapshot.MacroRouteId, reason), reason, $"Gameplay route LevelCollection invalid. detail='{collectionError}'.");
            }

            int currentIndex = -1;
            for (int i = 0; i < collection.Levels.Count; i++)
            {
                if (ReferenceEquals(collection.Levels[i], snapshot.LevelRef))
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex < 0)
            {
                FailFastConfig(snapshot.MacroRouteId, snapshot.MacroRouteRef.RouteKind, BuildLevelSignature(snapshot.LevelRef, snapshot.MacroRouteId, reason), reason, $"Current levelRef not found in route collection. levelRef='{snapshot.LevelRef.name}'.");
            }

            int nextIndex = (currentIndex + 1) % collection.Levels.Count;
            LevelDefinitionAsset nextLevelRef = collection.Levels[nextIndex];
            if (nextLevelRef == null)
            {
                FailFastConfig(snapshot.MacroRouteId, snapshot.MacroRouteRef.RouteKind, BuildLevelSignature(snapshot.LevelRef, snapshot.MacroRouteId, reason), reason, $"Resolved null next level at index='{nextIndex}'.");
            }

            return nextLevelRef;
        }

        public string BuildLevelSignature(LevelDefinitionAsset levelRef, SceneRouteId routeId, string reason)
        {
            string levelName = levelRef != null ? levelRef.name : "<null>";
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "LevelFlow/Unspecified" : reason.Trim();
            return $"level:{levelName}|route:{routeId}|reason:{normalizedReason}";
        }

        public string BuildLocalContentId(LevelDefinitionAsset levelRef, string contentId = null)
        {
            return LevelFlowContentDefaults.Normalize(contentId, levelRef);
        }

        private static bool TryValidateSnapshot(GameplayStartSnapshot snapshot, out string detail)
        {
            detail = string.Empty;

            if (!snapshot.IsValid)
            {
                detail = "Missing runtime gameplay snapshot.";
                return false;
            }

            if (!snapshot.HasLevelRef || snapshot.LevelRef == null)
            {
                detail = "Current gameplay snapshot has no levelRef.";
                return false;
            }

            if (!snapshot.MacroRouteId.IsValid)
            {
                detail = "Current gameplay snapshot has invalid macroRouteId.";
                return false;
            }

            if (snapshot.MacroRouteRef == null)
            {
                detail = "Current gameplay snapshot has null macroRouteRef.";
                return false;
            }

            if (snapshot.MacroRouteRef.RouteId != snapshot.MacroRouteId)
            {
                detail = $"RouteRef mismatch routeId='{snapshot.MacroRouteId}' routeRefRouteId='{snapshot.MacroRouteRef.RouteId}'.";
                return false;
            }

            if (snapshot.MacroRouteRef.RouteKind != SceneRouteKind.Gameplay)
            {
                detail = $"Snapshot routeKind invalid for LevelFlow. routeKind='{snapshot.MacroRouteRef.RouteKind}'.";
                return false;
            }

            return true;
        }

        private static void FailFastConfig(SceneRouteId routeId, SceneRouteKind routeKind, string signature, string reason, string configReason)
        {
            HardFailFastH1.Trigger(typeof(LevelFlowContentService),
                $"[FATAL][H1][LevelFlow] Content contract error. routeId='{routeId}' routeKind='{routeKind}' signature='{signature}' reason='{reason}' detail='{configReason}'");
        }

        private static void LogGameplayContentManifestAccepted(LevelDefinitionAsset levelRef, GameplayContentManifest manifest)
        {
            if (manifest == null)
            {
                return;
            }

            IReadOnlyList<GameplayContentEntry> entries = manifest.Entries;
            int entryCount = entries != null ? entries.Count : 0;
            bool isEmpty = entryCount == 0;
            string ids = BuildEntryIdSummary(entries);
            string roles = BuildRoleSummary(entries);

            DebugUtility.Log<LevelFlowContentService>(
                $"[OBS][LevelFlow] GameplayContentManifestAccepted levelRef='{levelRef.name}' entries={entryCount} empty={isEmpty.ToString().ToLowerInvariant()} ids={ids} roles={roles}.",
                DebugUtility.Colors.Info);
        }

        private static string BuildEntryIdSummary(IReadOnlyList<GameplayContentEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return "[]";
            }

            StringBuilder builder = new StringBuilder();
            builder.Append('[');
            for (int i = 0; i < entries.Count; i++)
            {
                GameplayContentEntry entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                if (builder.Length > 1)
                {
                    builder.Append(',');
                }

                builder.Append(entry.EntryId);
            }

            builder.Append(']');
            return builder.ToString();
        }

        private static string BuildRoleSummary(IReadOnlyList<GameplayContentEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return "[]";
            }

            bool hasMain = false;
            bool hasAux = false;
            bool hasPrototype = false;

            for (int i = 0; i < entries.Count; i++)
            {
                GameplayContentEntry entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                switch (entry.Role)
                {
                    case GameplayContentEntryRole.Main:
                        hasMain = true;
                        break;
                    case GameplayContentEntryRole.Aux:
                        hasAux = true;
                        break;
                    case GameplayContentEntryRole.Prototype:
                        hasPrototype = true;
                        break;
                }
            }

            StringBuilder builder = new StringBuilder();
            builder.Append('[');

            bool first = true;
            if (hasMain)
            {
                AppendRole(builder, "Main", ref first);
            }

            if (hasAux)
            {
                AppendRole(builder, "Aux", ref first);
            }

            if (hasPrototype)
            {
                AppendRole(builder, "Prototype", ref first);
            }

            builder.Append(']');
            return builder.ToString();
        }

        private static void AppendRole(StringBuilder builder, string role, ref bool first)
        {
            if (!first)
            {
                builder.Append(',');
            }

            builder.Append(role);
            first = false;
        }
    }
}
