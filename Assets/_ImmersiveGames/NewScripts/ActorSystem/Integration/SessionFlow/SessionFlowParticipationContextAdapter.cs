using _ImmersiveGames.NewScripts.ActorSystem.Contracts.Inbound;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.Participation.Contracts;
using System;

namespace _ImmersiveGames.NewScripts.ActorSystem.Integration.SessionFlow
{
    /// <summary>
    /// Maps SessionFlow participation semantic state into ActorSystem inbound context.
    /// </summary>
    public sealed class SessionFlowParticipationContextAdapter : IActorSystemSemanticContextProvider
    {
        private readonly IGameplayParticipationFlowService _participationFlowService;

        public SessionFlowParticipationContextAdapter(IGameplayParticipationFlowService participationFlowService)
        {
            _participationFlowService = participationFlowService ?? throw new ArgumentNullException(nameof(participationFlowService));
        }

        public bool TryGetCurrent(out ActorSystemSemanticContext context)
        {
            if (_participationFlowService == null || !_participationFlowService.TryGetCurrent(out ParticipationSnapshot snapshot) || !snapshot.IsValid)
            {
                context = ActorSystemSemanticContext.Empty;
                return false;
            }

            context = new ActorSystemSemanticContext(
                snapshot.SessionSignature,
                snapshot.PhaseSignature,
                snapshot.Signature.Value,
                snapshot.PrimaryParticipantId.Value,
                snapshot.LocalParticipantId.Value,
                snapshot.ParticipantCount);
            return context.IsValid;
        }
    }
}
