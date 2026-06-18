using System;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime
{
    internal interface IActivityEntryPlacementMarkerLookup
    {
        bool TryResolvePlacementMarker(
            SessionActivityIdentity identity,
            string placementId,
            out Vector3 position,
            out Vector3 eulerAngles,
            out string resolutionReason);
    }

    internal sealed class ActivityEntryPlacementMarkerLookup : IActivityEntryPlacementMarkerLookup
    {
        private readonly ActivityContentRuntimeState _activityContentRuntimeState;

        public ActivityEntryPlacementMarkerLookup(ActivityContentRuntimeState activityContentRuntimeState)
        {
            _activityContentRuntimeState = activityContentRuntimeState ?? throw new ArgumentNullException(nameof(activityContentRuntimeState));
        }

        public bool TryResolvePlacementMarker(
            SessionActivityIdentity identity,
            string placementId,
            out Vector3 position,
            out Vector3 eulerAngles,
            out string resolutionReason)
        {
            position = Vector3.zero;
            eulerAngles = Vector3.zero;
            resolutionReason = string.Empty;

            string normalizedPlacementId = placementId.TrimToEmpty();
            if (!identity.IsValid)
            {
                resolutionReason = "invalid_activity_identity";
                return false;
            }

            if (string.IsNullOrWhiteSpace(normalizedPlacementId))
            {
                resolutionReason = "placement_id_missing";
                return false;
            }

            int matchCount = 0;
            string resolvedSources = string.Empty;
            string missingScenes = string.Empty;
            string searchedSources = string.Empty;

            var loadedSet = _activityContentRuntimeState.CurrentLoadedSet;
            if (loadedSet.IsValid && loadedSet.Identity.CycleKey == identity.CycleKey)
            {
                for (int sceneIndex = 0; sceneIndex < loadedSet.Scenes.Count; sceneIndex++)
                {
                    var record = loadedSet.Scenes[sceneIndex];
                    if (!record.IsValid || string.IsNullOrWhiteSpace(record.SceneName))
                    {
                        continue;
                    }

                    searchedSources = AppendCsv(searchedSources, $"ActivityContent:{record.SceneName}");
                    var scene = SceneManager.GetSceneByName(record.SceneName);
                    if (!scene.IsValid() || !scene.isLoaded)
                    {
                        missingScenes = AppendCsv(missingScenes, record.SceneName);
                        continue;
                    }

                    if (TryResolvePlacementMarkerInScene(
                        scene,
                        normalizedPlacementId,
                        "ActivityContent",
                        ref matchCount,
                        ref position,
                        ref eulerAngles,
                        ref resolvedSources,
                        out resolutionReason))
                    {
                        return false;
                    }
                }
            }
            else
            {
                searchedSources = AppendCsv(searchedSources, "ActivityContent:<missing_or_stale>");
            }

            var routeScene = SceneManager.GetActiveScene();
            if (routeScene.IsValid() && routeScene.isLoaded)
            {
                searchedSources = AppendCsv(searchedSources, $"RouteScene:{routeScene.name}");
                if (TryResolvePlacementMarkerInScene(
                    routeScene,
                    normalizedPlacementId,
                    "RouteScene",
                    ref matchCount,
                    ref position,
                    ref eulerAngles,
                    ref resolvedSources,
                    out resolutionReason))
                {
                    return false;
                }
            }
            else
            {
                searchedSources = AppendCsv(searchedSources, "RouteScene:<invalid_or_unloaded>");
            }

            if (matchCount == 1)
            {
                resolutionReason = $"resolved_from_authorized_placement_sources;source={resolvedSources}";
                return true;
            }

            string missingDetail = string.IsNullOrWhiteSpace(missingScenes)
                ? string.Empty
                : $";missingScenes={missingScenes}";
            resolutionReason = $"no_placement_marker_found_in_authorized_sources;searchedSources={searchedSources}{missingDetail}";
            return false;
        }

        private static bool TryResolvePlacementMarkerInScene(
            Scene scene,
            string normalizedPlacementId,
            string sourceKind,
            ref int matchCount,
            ref Vector3 position,
            ref Vector3 eulerAngles,
            ref string resolvedSources,
            out string failureReason)
        {
            failureReason = string.Empty;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                var root = roots[rootIndex];
                if (root == null)
                {
                    continue;
                }

                PlayerActorPlacementMarker[] markers = root.GetComponentsInChildren<PlayerActorPlacementMarker>(true);
                for (int markerIndex = 0; markerIndex < markers.Length; markerIndex++)
                {
                    var marker = markers[markerIndex];
                    if (marker == null || !marker.IsValid)
                    {
                        continue;
                    }

                    string markerId = marker.PlacementId.TrimToEmpty();
                    if (!string.Equals(markerId, normalizedPlacementId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    matchCount++;
                    if (matchCount > 1)
                    {
                        failureReason = $"duplicate_placement_marker_in_authorized_sources;placementId={normalizedPlacementId};sources={resolvedSources},{sourceKind}:{scene.name}";
                        return true;
                    }

                    position = marker.transform.position;
                    eulerAngles = marker.transform.rotation.eulerAngles;
                    resolvedSources = AppendCsv(resolvedSources, $"{sourceKind}:{scene.name}:{marker.name}");
                }
            }

            return false;
        }

        private static string AppendCsv(string current, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return current ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(current))
            {
                return value.Trim();
            }

            return $"{current},{value.Trim()}";
        }
    }
}
