using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;
using _ImmersiveGames.NewScripts.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.Players.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline
{
    internal static class PlayerCameraBindingStage
    {
        public static SessionActivityPipeline.PlayerCameraBindingStageResult Resolve(
            SessionActivityIdentity identity,
            IReadOnlyList<CameraBindingRequirement> cameraRequirements,
            IReadOnlyList<PlayerActorIdentityRecord> activeActors,
            ActivityPlayerActorRegistry registry)
        {
            int requiredCameraCount = 0;
            List<CameraBindingRequirement> activityCameraRequirements = new();
            for (int index = 0; index < cameraRequirements.Count; index++)
            {
                CameraBindingRequirement requirement = cameraRequirements[index];
                if (!requirement.IsValid || requirement.CameraBindingKind != ActivityCameraBindingRequirementKind.ActivityCamera)
                {
                    continue;
                }

                activityCameraRequirements.Add(requirement);
                if (requirement.Requirement.IsRequired)
                {
                    requiredCameraCount += 1;
                }
            }

            if (activityCameraRequirements.Count == 0)
            {
                return new SessionActivityPipeline.PlayerCameraBindingStageResult(false, 0, false, default, default, null, "no_activity_camera_requirement");
            }

            for (int reqIndex = 0; reqIndex < activityCameraRequirements.Count; reqIndex++)
            {
                CameraBindingRequirement requirement = activityCameraRequirements[reqIndex];
                for (int actorIndex = 0; actorIndex < activeActors.Count; actorIndex++)
                {
                    PlayerActorIdentityRecord actor = activeActors[actorIndex];
                    if (!actor.IsValid)
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(requirement.TargetId) &&
                        !string.Equals(requirement.TargetId, actor.PlayerActorId, StringComparison.Ordinal) &&
                        !string.Equals(requirement.TargetId, actor.PlayerSlotId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (!registry.TryResolveInstanceForControl(identity, actor.PlayerActorId, out GameObject instance, out PlayerActorIdentityRecord observedIdentity) ||
                        instance == null ||
                        !observedIdentity.IsValid)
                    {
                        continue;
                    }

                    PlayerCameraEndpoint endpoint = ResolveCameraEndpointOrNull(instance, actor);
                    if (endpoint == null || !endpoint.HasValidTargets)
                    {
                        continue;
                    }

                    return new SessionActivityPipeline.PlayerCameraBindingStageResult(true, requiredCameraCount, true, actor, requirement, endpoint, "camera_endpoint_resolved");
                }
            }

            if (activityCameraRequirements.Count == 1 && activeActors.Count == 1)
            {
                PlayerActorIdentityRecord actor = activeActors[0];
                if (registry.TryResolveInstanceForControl(identity, actor.PlayerActorId, out GameObject instance, out _) && instance != null)
                {
                    PlayerCameraEndpoint endpoint = ResolveCameraEndpointOrNull(instance, actor);
                    if (endpoint != null && endpoint.HasValidTargets)
                    {
                        return new SessionActivityPipeline.PlayerCameraBindingStageResult(true, requiredCameraCount, true, actor, activityCameraRequirements[0], endpoint, "camera_endpoint_resolved_single_actor_fallback");
                    }
                }
            }

            return new SessionActivityPipeline.PlayerCameraBindingStageResult(true, requiredCameraCount, false, default, default, null, "player_camera_endpoint_missing");
        }

        private static PlayerCameraEndpoint ResolveCameraEndpointOrNull(GameObject instance, PlayerActorIdentityRecord actor)
        {
            if (instance == null)
            {
                return null;
            }

            PlayerCameraEndpoint[] endpoints = instance.GetComponentsInChildren<PlayerCameraEndpoint>(true);
            if (endpoints == null || endpoints.Length == 0)
            {
                return null;
            }

            if (endpoints.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Camera binding failed: playerActorId='{actor.PlayerActorId}' slotId='{actor.PlayerSlotId}' possui multiplos PlayerCameraEndpoint sem endpoint explicito.");
            }

            return endpoints[0];
        }
    }
}
