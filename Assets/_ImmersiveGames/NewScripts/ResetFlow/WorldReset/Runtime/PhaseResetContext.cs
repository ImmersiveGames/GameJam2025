using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using System.Threading;
using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime
{
    public readonly struct PhaseResetContext
    {
        public PhaseResetContext(
            PhaseDefinitionAsset phaseDefinitionRef,
            SceneRouteId macroRouteId,
            PhaseContextSignature phaseSignature,
            string resetSignature = null)
        {
            PhaseDefinitionRef = phaseDefinitionRef;
            MacroRouteId = macroRouteId;
            PhaseSignature = phaseSignature;
            ResetSignature = string.IsNullOrWhiteSpace(resetSignature) ? string.Empty : resetSignature.Trim();
        }

        public PhaseDefinitionAsset PhaseDefinitionRef { get; }
        public SceneRouteId MacroRouteId { get; }
        public PhaseContextSignature PhaseSignature { get; }
        public string ResetSignature { get; }
        public bool IsValid => PhaseDefinitionRef != null && MacroRouteId.IsValid && PhaseSignature.IsValid;
    }

    public readonly struct PhaseResetHandoffRequest
    {
        public PhaseResetHandoffRequest(
            PhaseResetContext resetContext,
            string activeScene,
            string reason,
            string source)
        {
            ResetContext = resetContext;
            ActiveScene = string.IsNullOrWhiteSpace(activeScene) ? string.Empty : activeScene.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
        }

        public PhaseResetContext ResetContext { get; }
        public string ActiveScene { get; }
        public string Reason { get; }
        public string Source { get; }
        public bool IsValid => ResetContext.IsValid && !string.IsNullOrWhiteSpace(ActiveScene) && !string.IsNullOrWhiteSpace(Reason);
    }

    public interface IPhaseResetOperationalHandoffService
    {
        Task ExecuteAsync(PhaseResetHandoffRequest request, CancellationToken ct);
    }

    public readonly struct PhaseResetCompletedEvent : IEvent
    {
        public PhaseResetCompletedEvent(PhaseResetContext resetContext, string reason, string source)
        {
            ResetContext = resetContext;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
        }

        public PhaseResetContext ResetContext { get; }
        public string Reason { get; }
        public string Source { get; }

        public bool IsValid => ResetContext.IsValid;
    }
}

