using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime
{
    /// <summary>
    /// Define as propriedades de um estilo de transição.
    /// </summary>
    public readonly struct TransitionStyleDefinition
    {
        public SceneTransitionProfile Profile { get; }
        public SceneFlowProfileId ProfileId { get; }
        public bool UseFade { get; }

        public TransitionStyleDefinition(SceneTransitionProfile profile, SceneFlowProfileId profileId, bool useFade)
        {
            Profile = profile;
            ProfileId = profileId;
            UseFade = useFade;
        }

        public override string ToString()
            => $"profile='{ProfileId}', useFade={UseFade}";
    }
}
