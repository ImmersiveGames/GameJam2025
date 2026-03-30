using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Policies;
namespace _ImmersiveGames.NewScripts.Game.Gameplay.GameplayReset.Policy
{
    internal interface IActorGroupGameplayResetPolicy
    {
        bool IsStrict { get; }

        bool AllowSceneScan { get; }

        void ReportDegraded(string feature, string reason, string detail = null, string signature = null, string profile = null);
    }

    internal sealed class ActorGroupGameplayResetPolicyAdapter : IActorGroupGameplayResetPolicy
    {
        private readonly IWorldResetPolicy _policy;

        public ActorGroupGameplayResetPolicyAdapter(IWorldResetPolicy policy)
        {
            _policy = policy;
        }

        public bool IsStrict => _policy != null && _policy.IsStrict;

        public bool AllowSceneScan => _policy != null && _policy.AllowSceneScan;

        public void ReportDegraded(string feature, string reason, string detail = null, string signature = null, string profile = null)
        {
            _policy?.ReportDegraded(feature, reason, detail, signature, profile);
        }
    }
}

