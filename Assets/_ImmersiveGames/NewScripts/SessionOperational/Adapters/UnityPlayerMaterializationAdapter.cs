using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Semantic.Preparation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.GameplayRuntime;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{
    public sealed class UnityPlayerMaterializationAdapter : IPlayerMaterializationAdapter
    {
        private const string RuntimeRootName = "__PrototypePlayersRuntimeRoot";

        public IReadOnlyList<PlayerMaterializationRecord> MaterializePrototypePlayers(PlayerMaterializationCommand command, SessionOperationalRouteCommand routeCommand)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("PlayerMaterializationCommand is invalid.");
            }

            if (!routeCommand.IsValid || routeCommand.RouteOperationId != command.RouteOperationId)
            {
                throw new InvalidOperationException("stale_or_foreign_route: materialization command route does not match active route command.");
            }

            Scene targetScene = SceneManager.GetActiveScene();
            if (!targetScene.IsValid() || !targetScene.isLoaded)
            {
                throw new InvalidOperationException("targetScene is invalid or not loaded for player materialization.");
            }

            Transform root = EnsureRuntimeRoot(targetScene, command.RouteOperationId);
            DestroyPreviousPrototypePlayers(root);
            List<PlayerMaterializationRecord> records = new(command.Requests.Count);

            DebugUtility.Log(typeof(UnityPlayerMaterializationAdapter),
                $"[OBS][SessionOperationalPipeline][PlayerPreparation] event='PlayerMaterializationStarted' pipelineId='{command.Identity.PipelineId}' sessionId='{command.Identity.SessionId}' routeIdentity='{command.Identity.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.Identity.TransitionId}' routeSequence='{command.Identity.RouteSequence}' playerId='*' required='*' optional='*' prefab='*' scene='{targetScene.name}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            for (int i = 0; i < command.Requests.Count; i++)
            {
                PlayerMaterializationRequest request = command.Requests[i];
                if (!request.IsValid)
                {
                    throw new InvalidOperationException($"Player materialization request at index {i} is invalid.");
                }

                if (!request.HasPrefab)
                {
                    if (request.Required)
                    {
                        throw new InvalidOperationException($"[FATAL][Config][PlayerPreparation] required player without prefab. playerId='{request.PlayerId}'.");
                    }

                    DebugUtility.Log(typeof(UnityPlayerMaterializationAdapter),
                        $"[OBS][SessionOperationalPipeline][PlayerPreparation] event='PlayerMaterializationSkipped' pipelineId='{command.Identity.PipelineId}' sessionId='{command.Identity.SessionId}' routeIdentity='{command.Identity.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.Identity.TransitionId}' routeSequence='{command.Identity.RouteSequence}' playerId='{request.PlayerId}' required='false' optional='true' prefab='<none>' scene='{targetScene.name}' source='{command.Source}' reason='optional_without_prefab'.",
                        DebugUtility.Colors.Info);

                    records.Add(new PlayerMaterializationRecord(request.PlayerId, false, false, PlayerMaterializationStatus.Skipped, string.Empty, targetScene.name));
                    continue;
                }

                GameObject instance = UnityEngine.Object.Instantiate(request.Prefab, root);
                instance.name = $"PrototypePlayer::{request.PlayerId}";
                PrototypePlayerRuntimeMarker marker = instance.GetComponent<PrototypePlayerRuntimeMarker>();
                if (marker == null)
                {
                    marker = instance.AddComponent<PrototypePlayerRuntimeMarker>();
                }

                marker.Bind(request.PlayerId, command.Identity.RouteIdentity, command.RouteOperationId, command.Identity.TransitionId, command.Identity.RouteSequence);

                DebugUtility.Log(typeof(UnityPlayerMaterializationAdapter),
                    $"[OBS][SessionOperationalPipeline][PlayerPreparation] event='PlayerMaterialized' pipelineId='{command.Identity.PipelineId}' sessionId='{command.Identity.SessionId}' routeIdentity='{command.Identity.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.Identity.TransitionId}' routeSequence='{command.Identity.RouteSequence}' playerId='{request.PlayerId}' required='{request.Required}' optional='{(!request.Required)}' prefab='{request.Prefab.name}' scene='{targetScene.name}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);

                records.Add(new PlayerMaterializationRecord(request.PlayerId, request.Required, true, PlayerMaterializationStatus.Materialized, instance.name, targetScene.name));
            }

            DebugUtility.Log(typeof(UnityPlayerMaterializationAdapter),
                $"[OBS][SessionOperationalPipeline][PlayerPreparation] event='PlayerMaterializationCompleted' pipelineId='{command.Identity.PipelineId}' sessionId='{command.Identity.SessionId}' routeIdentity='{command.Identity.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.Identity.TransitionId}' routeSequence='{command.Identity.RouteSequence}' playerId='*' required='*' optional='*' prefab='*' scene='{targetScene.name}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            return records;
        }

        private static Transform EnsureRuntimeRoot(Scene scene, string routeOperationId)
        {
            string expectedRootName = RuntimeRootName + "::" + routeOperationId;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && string.Equals(roots[i].name, expectedRootName, StringComparison.Ordinal))
                {
                    return roots[i].transform;
                }
            }

            GameObject root = new(expectedRootName);
            root.transform.SetParent(null, false);
            SceneManager.MoveGameObjectToScene(root, scene);
            return root.transform;
        }

        private static void DestroyPreviousPrototypePlayers(Transform root)
        {
            if (root == null)
            {
                return;
            }

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child != null)
                {
                    UnityEngine.Object.Destroy(child.gameObject);
                }
            }
        }
    }
}
