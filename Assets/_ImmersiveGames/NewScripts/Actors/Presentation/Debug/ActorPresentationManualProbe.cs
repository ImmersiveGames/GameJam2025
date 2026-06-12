using _ImmersiveGames.NewScripts.Actors.Presentation.Adapters;
using _ImmersiveGames.NewScripts.Actors.Presentation.Authoring;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;
using UnityEngine;
using UDebug = UnityEngine.Debug;

namespace _ImmersiveGames.NewScripts.Actors.Presentation.Debug
{
    /// <summary>
    /// Probe manual isolado para validar ActorPresentation sem integrar no SessionActivityPipeline.
    /// Não é produção, não decide lifecycle e não deve ser usado como owner de ActorPresentation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ActorPresentationManualProbe : MonoBehaviour
    {
        private const string LogPrefix = "[OBS][ActorPresentation][Probe]";

        [Header("Inputs")]
        [SerializeField] private ActorPresentationProfileAsset profile;
        [SerializeField] private ActorPresentationEndpoint endpoint;

        [Header("Identity")]
        [SerializeField] private string activityIdentity = "ManualPresentationProbeActivity";
        [SerializeField] private string actorId = "probe_actor_01";
        [SerializeField] private string actorKind = "PlayerActor";

        [Header("Probe Options")]
        [SerializeField] private bool releaseAfterFullProbe = true;
        [SerializeField] private bool clearPreviousInstanceBeforeMaterialize = true;

        private readonly ActorPresentationPlanResolver _planResolver = new ActorPresentationPlanResolver();
        private readonly IActorPresentationMaterializationAdapter _adapter = new UnityActorPresentationMaterializationAdapter();

        private ActorPresentationResolvedPlan _lastResolvedPlan;
        private ActorPresentationRuntimeHandle _lastRuntimeHandle;
        private bool _hasResolvedPlan;
        private bool _hasRuntimeHandle;

        [ContextMenu("ActorPresentation Probe/Resolve Plan")]
        public void ResolvePlanContextMenu()
        {
            ResolvePlan("ManualProbe/ResolvePlan");
        }

        [ContextMenu("ActorPresentation Probe/Materialize")]
        public void MaterializeContextMenu()
        {
            Materialize("ManualProbe/Materialize");
        }

        [ContextMenu("ActorPresentation Probe/Release")]
        public void ReleaseContextMenu()
        {
            Release("ManualProbe/Release");
        }

        [ContextMenu("ActorPresentation Probe/Run Full Probe")]
        public void RunFullProbeContextMenu()
        {
            RunFullProbe("ManualProbe/RunFullProbe");
        }

        [ContextMenu("ActorPresentation Probe/Clear Local Probe State")]
        public void ClearLocalProbeStateContextMenu()
        {
            ClearLocalState();
            UDebug.Log($"{LogPrefix} LocalProbeStateCleared probe='{name}'.", this);
        }

        public ActorPresentationPlanResolutionResult ResolvePlan(string reason)
        {
            var result = _planResolver.Resolve(
                profile,
                endpoint,
                activityIdentity,
                actorId,
                actorKind,
                nameof(ActorPresentationManualProbe),
                reason);

            if (result.IsSuccess)
            {
                _lastResolvedPlan = result.ResolvedPlan;
                _hasResolvedPlan = true;

                UDebug.Log(
                    $"{LogPrefix} PlanResolved " +
                    $"activityIdentity='{_lastResolvedPlan.ActivityIdentity}' " +
                    $"actorId='{_lastResolvedPlan.ActorId}' " +
                    $"actorKind='{_lastResolvedPlan.ActorKind}' " +
                    $"profileId='{_lastResolvedPlan.ProfileId}' " +
                    $"primarySlot='{_lastResolvedPlan.PrimarySlotKind}:{_lastResolvedPlan.PrimarySlotId}' " +
                    $"slotCount='{_lastResolvedPlan.Slots.Count}' " +
                    $"reason='{reason}'.",
                    this);

                return result;
            }

            _hasResolvedPlan = false;
            _lastResolvedPlan = default;

            if (result.IsSkippedOptional)
            {
                UDebug.Log(
                    $"{LogPrefix} PlanSkippedOptional " +
                    $"reasonCode='{result.ReasonCode}' " +
                    $"message='{result.Message}' " +
                    $"reason='{reason}'.",
                    this);

                return result;
            }

            UDebug.LogError(
                $"{LogPrefix} PlanFailed " +
                $"reasonCode='{result.ReasonCode}' " +
                $"message='{result.Message}' " +
                $"reason='{reason}'.",
                this);

            return result;
        }

        public ActorPresentationResult Materialize(string reason)
        {
            if (!_hasResolvedPlan)
            {
                var planResult = ResolvePlan($"{reason}/ResolveBeforeMaterialize");
                if (!planResult.IsSuccess)
                {
                    return ActorPresentationResult.Failed(
                        "actor_presentation_probe_plan_not_resolved",
                        "ActorPresentation probe could not materialize because plan was not resolved.");
                }
            }

            if (clearPreviousInstanceBeforeMaterialize && _hasRuntimeHandle)
            {
                Release($"{reason}/ReleasePreviousInstance");
            }

            var command = new ActorPresentationMaterializationCommand(
                _lastResolvedPlan,
                nameof(ActorPresentationManualProbe),
                reason);

            var result = _adapter.Materialize(command);

            if (result is { Kind: ActorPresentationResultKind.Materialized, ReadyFact: { IsValid: true } })
            {
                _lastRuntimeHandle = result.ReadyFact.RuntimeHandle;
                _hasRuntimeHandle = true;

                UDebug.Log(
                    $"{LogPrefix} Materialized " +
                    $"activityIdentity='{_lastRuntimeHandle.ResolvedPlan.ActivityIdentity}' " +
                    $"actorId='{_lastRuntimeHandle.ResolvedPlan.ActorId}' " +
                    $"profileId='{_lastRuntimeHandle.ResolvedPlan.ProfileId}' " +
                    $"instance='{_lastRuntimeHandle.PresentationInstance.name}' " +
                    $"reason='{reason}'.",
                    this);

                return result;
            }

            if (result.IsSkippedOptional)
            {
                UDebug.Log(
                    $"{LogPrefix} MaterializeSkippedOptional " +
                    $"reasonCode='{result.ReasonCode}' " +
                    $"message='{result.Message}' " +
                    $"reason='{reason}'.",
                    this);

                return result;
            }

            _hasRuntimeHandle = false;
            _lastRuntimeHandle = default;

            UDebug.LogError(
                $"{LogPrefix} MaterializeFailed " +
                $"reasonCode='{result.ReasonCode}' " +
                $"message='{result.Message}' " +
                $"reason='{reason}'.",
                this);

            return result;
        }

        public ActorPresentationResult Release(string reason)
        {
            if (!_hasRuntimeHandle)
            {
                UDebug.Log(
                    $"{LogPrefix} ReleaseSkippedNoRuntimeHandle " +
                    $"reasonCode='actor_presentation_probe_no_runtime_handle' " +
                    $"reason='{reason}'.",
                    this);

                return ActorPresentationResult.Failed(
                    "actor_presentation_probe_no_runtime_handle",
                    "ActorPresentation probe has no runtime handle to release.");
            }

            var command = new ActorPresentationReleaseCommand(
                _lastRuntimeHandle,
                nameof(ActorPresentationManualProbe),
                reason);

            var result = _adapter.Release(command);

            if (result is { Kind: ActorPresentationResultKind.Released, ReleasedFact: { IsValid: true } })
            {
                UDebug.Log(
                    $"{LogPrefix} Released " +
                    $"activityIdentity='{_lastRuntimeHandle.ResolvedPlan.ActivityIdentity}' " +
                    $"actorId='{_lastRuntimeHandle.ResolvedPlan.ActorId}' " +
                    $"profileId='{_lastRuntimeHandle.ResolvedPlan.ProfileId}' " +
                    $"reason='{reason}'.",
                    this);

                _hasRuntimeHandle = false;
                _lastRuntimeHandle = default;

                return result;
            }

            UDebug.LogError(
                $"{LogPrefix} ReleaseFailed " +
                $"reasonCode='{result.ReasonCode}' " +
                $"message='{result.Message}' " +
                $"reason='{reason}'.",
                this);

            return result;
        }

        public void RunFullProbe(string reason)
        {
            UDebug.Log(
                $"{LogPrefix} ProbeStarted " +
                $"probe='{name}' " +
                $"activityIdentity='{activityIdentity}' " +
                $"actorId='{actorId}' " +
                $"actorKind='{actorKind}' " +
                $"reason='{reason}'.",
                this);

            var planResult = ResolvePlan($"{reason}/ResolvePlan");
            if (!planResult.IsSuccess)
            {
                UDebug.LogError(
                    $"{LogPrefix} ProbeFailed " +
                    $"stage='ResolvePlan' " +
                    $"reasonCode='{planResult.ReasonCode}' " +
                    $"message='{planResult.Message}'.",
                    this);
                return;
            }

            var materializeResult = Materialize($"{reason}/Materialize");
            if (materializeResult.Kind != ActorPresentationResultKind.Materialized)
            {
                UDebug.LogError(
                    $"{LogPrefix} ProbeFailed " +
                    $"stage='Materialize' " +
                    $"reasonCode='{materializeResult.ReasonCode}' " +
                    $"message='{materializeResult.Message}'.",
                    this);
                return;
            }

            if (releaseAfterFullProbe)
            {
                var releaseResult = Release($"{reason}/Release");
                if (releaseResult.Kind != ActorPresentationResultKind.Released)
                {
                    UDebug.LogError(
                        $"{LogPrefix} ProbeFailed " +
                        $"stage='Release' " +
                        $"reasonCode='{releaseResult.ReasonCode}' " +
                        $"message='{releaseResult.Message}'.",
                        this);
                    return;
                }
            }

            UDebug.Log(
                $"{LogPrefix} ProbeSucceeded " +
                $"probe='{name}' " +
                $"releaseAfterFullProbe='{releaseAfterFullProbe}' " +
                $"reason='{reason}'.",
                this);
        }

        private void ClearLocalState()
        {
            _hasResolvedPlan = false;
            _hasRuntimeHandle = false;
            _lastResolvedPlan = default;
            _lastRuntimeHandle = default;
        }

        private void OnValidate()
        {
            activityIdentity = Normalize(activityIdentity);
            actorId = Normalize(actorId);
            actorKind = Normalize(actorKind);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
