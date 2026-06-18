namespace _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode
{
    /// <summary>
    /// Provider de modo de execução que respeita o RuntimeModeConfig.
    ///
    /// Regra:
    /// - se config não existir ou ModeOverride=Auto -> delega para provider padrão (UnityRuntimeModeProvider).
    /// - ForceStrict/ForceRelease -> força o modo.
    ///
    /// Observação:
    /// - Módulos dependem somente de IRuntimeModeProvider.
    /// </summary>
    public sealed class ConfigurableRuntimeModeProvider : IRuntimeModeProvider
    {
        private readonly IRuntimeModeProvider _defaultProvider;
        private readonly RuntimeModeConfig _config;

        public ConfigurableRuntimeModeProvider(IRuntimeModeProvider defaultProvider, RuntimeModeConfig config)
        {
            _defaultProvider = defaultProvider ?? new UnityRuntimeModeProvider();
            _config = config;
        }

        public RuntimeMode Current
        {
            get
            {
                if (_config == null)
                {
                    return _defaultProvider.Current;
                }

                switch (_config.modeOverride)
                {
                    case RuntimeModeOverride.ForceStrict:
                        return RuntimeMode.Strict;
                    case RuntimeModeOverride.ForceRelease:
                        return RuntimeMode.Release;
                    case RuntimeModeOverride.Auto:
                    default:
                        return _defaultProvider.Current;
                }
            }
        }

        public bool IsStrict => Current == RuntimeMode.Strict;
    }
}
