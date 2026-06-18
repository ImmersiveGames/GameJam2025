using _ImmersiveGames.NewScripts.Actors.Presentation.Adapters;
using _ImmersiveGames.NewScripts.Actors.Presentation.Authoring;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Presentation.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ActorPooledPresentationPreparer : PooledBehaviour
    {
        private const string PoolActivityIdentity = "actor.pool.runtime_spawned_actor.presentation";
        private const string PoolActorId = "actor.projectile.runtime.spawn.pool";
        private const string PoolActorKind = "RuntimeSpawnedActor";
        private const string PreparationReason = "pooled_runtime_spawned_actor_presentation_preparation";
        private const string VisualContractOptionalForRuntimeSpawn = "optional_for_runtime_spawn";

        [SerializeField] private ActorPresentationEndpoint presentationEndpoint;

        private readonly ActorPresentationPlanResolver _planResolver = new();
        private readonly UnityActorPresentationMaterializationAdapter _materializationAdapter = new();

        private ActorPresentationRuntimeHandle _runtimeHandle;
        private bool _hasPreparedPresentation;

        protected override void OnAfterPoolCreated()
        {
            if (_hasPreparedPresentation && _runtimeHandle.IsValid)
            {
                LogAlreadyPrepared();
                return;
            }

            LogPreparationStarted();

            if (!TryResolveEndpoint(out var endpoint, out var profile, out var failureReason, out var failureMessage))
            {
                LogPreparationFailed(failureReason, failureMessage);
                return;
            }

            var resolution = _planResolver.Resolve(
                profile,
                endpoint,
                PoolActivityIdentity,
                PoolActorId,
                PoolActorKind,
                nameof(ActorPooledPresentationPreparer),
                PreparationReason);

            if (resolution.IsFailed)
            {
                LogPreparationFailed(resolution.ReasonCode, resolution.Message);
                return;
            }

            if (resolution.IsSkippedOptional)
            {
                LogPreparationSkipped(resolution.ReasonCode, resolution.Message, profile, endpoint, null);
                LogPreparationCompleted("presentation_optional_and_skipped", profile, endpoint, null);
                return;
            }

            LogPlanResolved(resolution.ResolvedPlan);

            var materialization = _materializationAdapter.Materialize(
                new ActorPresentationMaterializationCommand(
                    resolution.ResolvedPlan,
                    nameof(ActorPooledPresentationPreparer),
                    PreparationReason));

            if (materialization.IsFailed)
            {
                LogPreparationFailed(materialization.ReasonCode, materialization.Message);
                return;
            }

            if (materialization.IsSkippedOptional)
            {
                LogPreparationSkipped(materialization.ReasonCode, materialization.Message, profile, endpoint, null);
                LogPreparationCompleted("materialization_optional_and_skipped", profile, endpoint, null);
                return;
            }

            _runtimeHandle = materialization.ReadyFact.RuntimeHandle;
            _hasPreparedPresentation = _runtimeHandle.IsValid;

            if (!_hasPreparedPresentation)
            {
                LogPreparationFailed(
                    "presentation_runtime_handle_invalid",
                    "ActorPresentation materialization returned an invalid runtime handle.");
                return;
            }

            var presentationInstance = _runtimeHandle.PresentationInstance;
            LogMaterialized(resolution.ResolvedPlan, presentationInstance, endpoint);
            endpoint.RebuildPoolableSpawnOriginSurface(nameof(ActorPooledPresentationPreparer));
            LogPreparationCompleted("presentation_materialized_on_pool_created", profile, endpoint, presentationInstance);
        }

        protected override void OnAfterPoolRent()
        {
            if (_hasPreparedPresentation && _runtimeHandle.IsValid)
            {
                LogRetained(_runtimeHandle.PresentationInstance, "presentation_already_materialized_before_rent");
                return;
            }

            if (presentationEndpoint == null)
            {
                LogPreparationFailed(
                    "presentation_endpoint_missing_at_rent",
                    "ActorPooledPresentationPreparer received rent before presentationEndpoint was configured.");
                return;
            }

            var profile = presentationEndpoint.Profile;
            if (profile == null)
            {
                LogPreparationFailed(
                    "presentation_profile_missing_at_rent",
                    "ActorPooledPresentationPreparer received rent before a presentation profile was configured.");
                return;
            }

            if (profile.IsRequired)
            {
                LogPreparationFailed(
                    "presentation_required_but_not_materialized_before_rent",
                    "ActorPooledPresentationPreparer required presentation materialization before rent but found no prepared runtime handle.");
                return;
            }

            LogPreparationSkipped(
                "presentation_not_materialized_before_rent",
                "ActorPooledPresentationPreparer did not materialize presentation before rent.",
                profile,
                presentationEndpoint,
                null);
        }

        protected override void OnAfterPoolDestroyed()
        {
            _runtimeHandle = default;
            _hasPreparedPresentation = false;
        }

        private bool TryResolveEndpoint(
            out ActorPresentationEndpoint endpoint,
            out ActorPresentationProfileAsset profile,
            out string failureReason,
            out string failureMessage)
        {
            endpoint = presentationEndpoint;
            profile = null;
            failureReason = string.Empty;
            failureMessage = string.Empty;

            if (endpoint == null)
            {
                failureReason = "presentation_endpoint_missing";
                failureMessage = "ActorPooledPresentationPreparer requires ActorPresentationEndpoint on the pooled actor root.";
                return false;
            }

            profile = endpoint.Profile;
            if (profile == null)
            {
                failureReason = "presentation_profile_missing";
                failureMessage = "ActorPooledPresentationPreparer requires ActorPresentationProfileAsset via ActorPresentationEndpoint.";
                return false;
            }

            return true;
        }

        private void LogPreparationStarted()
        {
            string profileId = presentationEndpoint?.Profile?.ProfileId.TrimToEmpty();
            DebugUtility.Log(
                typeof(ActorPooledPresentationPreparer),
                $"event='ActorPooledPresentationPreparationStarted' presentationEndpointPresent='{(presentationEndpoint != null)}' presentationProfileId='{(string.IsNullOrWhiteSpace(profileId) ? "none" : profileId)}' visualContract='{VisualContractOptionalForRuntimeSpawn}' source='{nameof(ActorPooledPresentationPreparer)}' reason='{PreparationReason}'.",
                DebugUtility.Colors.Info);
        }

        private void LogAlreadyPrepared()
        {
            var presentationInstance = _runtimeHandle.PresentationInstance;
            var observation = BuildVisualObservation(presentationInstance, presentationEndpoint);

            DebugUtility.Log(
                typeof(ActorPooledPresentationPreparer),
                $"event='ActorPooledPresentationAlreadyPrepared' presentationInstanceName='{GetInstanceName(presentationInstance)}' instancePath='{BuildInstancePath(presentationInstance)}' rendererCount='{observation.RendererCount}' enabledRendererCount='{observation.EnabledRendererCount}' materialCount='{observation.MaterialCount}' validMaterialCount='{observation.ValidMaterialCount}' presentationEndpointPresent='{observation.PresentationEndpointPresent}' presentationProfileId='{observation.PresentationProfileId}' presentationVisualRootPresent='{observation.PresentationVisualRootPresent}' visualContract='{VisualContractOptionalForRuntimeSpawn}' source='{nameof(ActorPooledPresentationPreparer)}' reason='presentation_already_prepared'.",
                DebugUtility.Colors.Info);
        }

        private void LogPlanResolved(ActorPresentationResolvedPlan plan)
        {
            DebugUtility.Log(
                typeof(ActorPooledPresentationPreparer),
                $"event='ActorPooledPresentationPlanResolved' activityIdentity='{plan.ActivityIdentity}' actorId='{plan.ActorId}' actorKind='{plan.ActorKind}' profileId='{plan.ProfileId}' primarySlotKind='{plan.PrimarySlotKind}' primarySlotId='{plan.PrimarySlotId}' source='{plan.Source}' reason='{plan.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private void LogMaterialized(
            ActorPresentationResolvedPlan plan,
            GameObject presentationInstance,
            ActorPresentationEndpoint endpoint)
        {
            var observation = BuildVisualObservation(presentationInstance, endpoint);

            DebugUtility.Log(
                typeof(ActorPooledPresentationPreparer),
                $"event='ActorPooledPresentationMaterialized' presentationInstanceName='{GetInstanceName(presentationInstance)}' instancePath='{BuildInstancePath(presentationInstance)}' activityIdentity='{plan.ActivityIdentity}' actorId='{plan.ActorId}' actorKind='{plan.ActorKind}' profileId='{plan.ProfileId}' rendererCount='{observation.RendererCount}' enabledRendererCount='{observation.EnabledRendererCount}' materialCount='{observation.MaterialCount}' validMaterialCount='{observation.ValidMaterialCount}' presentationEndpointPresent='{observation.PresentationEndpointPresent}' presentationProfileId='{observation.PresentationProfileId}' presentationVisualRootPresent='{observation.PresentationVisualRootPresent}' visualContract='{VisualContractOptionalForRuntimeSpawn}' source='{nameof(ActorPooledPresentationPreparer)}' reason='presentation_materialized_on_pool_created'.",
                DebugUtility.Colors.Success);
        }

        private void LogRetained(GameObject presentationInstance, string reason)
        {
            var observation = BuildVisualObservation(presentationInstance, presentationEndpoint);

            DebugUtility.Log(
                typeof(ActorPooledPresentationPreparer),
                $"event='ActorPooledPresentationRetained' presentationInstanceName='{GetInstanceName(presentationInstance)}' instancePath='{BuildInstancePath(presentationInstance)}' rendererCount='{observation.RendererCount}' enabledRendererCount='{observation.EnabledRendererCount}' materialCount='{observation.MaterialCount}' validMaterialCount='{observation.ValidMaterialCount}' presentationEndpointPresent='{observation.PresentationEndpointPresent}' presentationProfileId='{observation.PresentationProfileId}' presentationVisualRootPresent='{observation.PresentationVisualRootPresent}' visualContract='{VisualContractOptionalForRuntimeSpawn}' source='{nameof(ActorPooledPresentationPreparer)}' reason='{reason.TrimToEmpty()}'.",
                DebugUtility.Colors.Info);
        }

        private void LogPreparationSkipped(
            string reasonCode,
            string message,
            ActorPresentationProfileAsset profile,
            ActorPresentationEndpoint endpoint,
            GameObject presentationInstance)
        {
            var observation = BuildVisualObservation(presentationInstance, endpoint);
            string profileId = profile?.ProfileId.TrimToEmpty();

            DebugUtility.Log(
                typeof(ActorPooledPresentationPreparer),
                $"event='ActorPooledPresentationPreparationSkipped' reasonCode='{reasonCode.TrimToEmpty()}' message='{message.TrimToEmpty()}' presentationInstanceName='{GetInstanceName(presentationInstance)}' rendererCount='{observation.RendererCount}' enabledRendererCount='{observation.EnabledRendererCount}' materialCount='{observation.MaterialCount}' validMaterialCount='{observation.ValidMaterialCount}' presentationEndpointPresent='{observation.PresentationEndpointPresent}' presentationProfileId='{(string.IsNullOrWhiteSpace(profileId) ? observation.PresentationProfileId : profileId)}' presentationVisualRootPresent='{observation.PresentationVisualRootPresent}' visualContract='{VisualContractOptionalForRuntimeSpawn}' source='{nameof(ActorPooledPresentationPreparer)}' reason='presentation_skipped'.",
                DebugUtility.Colors.Info);
        }

        private void LogPreparationCompleted(
            string reason,
            ActorPresentationProfileAsset profile,
            ActorPresentationEndpoint endpoint,
            GameObject presentationInstance)
        {
            var observation = BuildVisualObservation(presentationInstance, endpoint);
            string profileId = profile?.ProfileId.TrimToEmpty();

            DebugUtility.Log(
                typeof(ActorPooledPresentationPreparer),
                $"event='ActorPooledPresentationPreparationCompleted' presentationInstanceName='{GetInstanceName(presentationInstance)}' rendererCount='{observation.RendererCount}' enabledRendererCount='{observation.EnabledRendererCount}' materialCount='{observation.MaterialCount}' validMaterialCount='{observation.ValidMaterialCount}' presentationEndpointPresent='{observation.PresentationEndpointPresent}' presentationProfileId='{(string.IsNullOrWhiteSpace(profileId) ? observation.PresentationProfileId : profileId)}' presentationVisualRootPresent='{observation.PresentationVisualRootPresent}' visualContract='{VisualContractOptionalForRuntimeSpawn}' source='{nameof(ActorPooledPresentationPreparer)}' reason='{reason.TrimToEmpty()}'.",
                DebugUtility.Colors.Success);
        }

        private void LogPreparationFailed(string reasonCode, string message)
        {
            DebugUtility.LogError(
                typeof(ActorPooledPresentationPreparer),
                $"event='ActorPooledPresentationPreparationFailed' reasonCode='{reasonCode.TrimToEmpty()}' message='{message.TrimToEmpty()}' presentationEndpointPresent='{(presentationEndpoint != null)}' presentationProfileId='{presentationEndpoint?.Profile?.ProfileId.TrimToEmpty()}' visualContract='{VisualContractOptionalForRuntimeSpawn}' source='{nameof(ActorPooledPresentationPreparer)}' reason='presentation_preparation_failed'.");
        }

        private static PresentationObservation BuildVisualObservation(GameObject presentationInstance, ActorPresentationEndpoint endpoint)
        {
            var observation = new PresentationObservation
            {
                PresentationEndpointPresent = endpoint != null,
                PresentationProfileId = endpoint?.Profile?.ProfileId.TrimToEmpty(),
                PresentationVisualRootPresent = false,
                RendererCount = 0,
                EnabledRendererCount = 0,
                MaterialCount = 0,
                ValidMaterialCount = 0
            };

            if (endpoint != null &&
                endpoint.TryGetContainer(ActorPresentationSlotKind.VisualRoot, "visual.root", out var visualRootContainer) &&
                visualRootContainer != null &&
                visualRootContainer.HasContainerTransform)
            {
                observation.PresentationVisualRootPresent = true;
            }

            GameObject root = presentationInstance != null ? presentationInstance : endpoint?.gameObject;
            if (root == null)
            {
                return observation;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
            if (renderers == null)
            {
                return observation;
            }

            observation.RendererCount = renderers.Length;
            for (int index = 0; index < renderers.Length; index++)
            {
                var renderer = renderers[index];
                if (renderer == null)
                {
                    continue;
                }

                if (renderer.enabled)
                {
                    observation.EnabledRendererCount++;
                }

                Material[] sharedMaterials = renderer.sharedMaterials;
                if (sharedMaterials == null)
                {
                    continue;
                }

                observation.MaterialCount += sharedMaterials.Length;
                for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
                {
                    if (sharedMaterials[materialIndex] != null)
                    {
                        observation.ValidMaterialCount++;
                    }
                }
            }

            return observation;
        }

        private static string BuildInstancePath(GameObject presentationInstance)
        {
            if (presentationInstance == null)
            {
                return string.Empty;
            }

            return BuildInstancePath(presentationInstance.transform);
        }

        private static string BuildInstancePath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            System.Collections.Generic.Stack<string> segments = new();
            Transform current = transform;

            while (current != null)
            {
                segments.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", segments);
        }

        private static string GetInstanceName(GameObject instance)
        {
            return instance != null ? instance.name : string.Empty;
        }
private struct PresentationObservation
        {
            public bool PresentationEndpointPresent;
            public bool PresentationVisualRootPresent;
            public string PresentationProfileId;
            public int RendererCount;
            public int EnabledRendererCount;
            public int MaterialCount;
            public int ValidMaterialCount;
        }
    }
}
