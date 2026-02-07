using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime
{
    /// <summary>
    /// Define as propriedades de um estilo de transição.
    /// </summary>
    public readonly struct TransitionStyleDefinition
    {
        public SceneFlowProfileId ProfileId { get; }
        public bool UseFade { get; }

        public TransitionStyleDefinition(SceneFlowProfileId profileId, bool useFade)
        {
            ProfileId = profileId;
            UseFade = useFade;
        }

        public override string ToString()
            => $"profile='{ProfileId}', useFade={UseFade}";
    }
}
