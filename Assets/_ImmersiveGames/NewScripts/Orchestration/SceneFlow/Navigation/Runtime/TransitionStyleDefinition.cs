using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime
{
    /// <summary>
    /// Define as propriedades efetivas de um estilo de transicao.
    /// Dados textuais sao apenas observabilidade.
    /// </summary>
    public readonly struct TransitionStyleDefinition
    {
        public SceneTransitionProfile Profile { get; }
        public bool UseFade { get; }
        public string StyleLabel { get; }
        public string ProfileLabel { get; }

        public TransitionStyleDefinition(SceneTransitionProfile profile, bool useFade, string styleLabel, string profileLabel)
        {
            Profile = profile;
            UseFade = useFade;
            StyleLabel = string.IsNullOrWhiteSpace(styleLabel) ? string.Empty : styleLabel.Trim();
            ProfileLabel = string.IsNullOrWhiteSpace(profileLabel) ? string.Empty : profileLabel.Trim();
        }

        public override string ToString()
            => $"style='{StyleLabel}', profile='{ProfileLabel}', useFade={UseFade}";
    }
}
