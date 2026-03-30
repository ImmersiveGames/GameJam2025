namespace _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode
{
    /// <summary>
    /// Provider de modo de execução que respeita o RuntimeModeConfig.
    ///
    /// Regra:
    /// - se config não existir ou ModeOverride=Auto -> delega para fallback (UnityRuntimeModeProvider).
    /// - ForceStrict/ForceRelease -> força o modo.
    ///
    /// Observação:
    /// - Mantém compatibilidade: módulos continuam dependendo só de IRuntimeModeProvider.
    /// </summary>
    public sealed class ConfigurableRuntimeModeProvider : IRuntimeModeProvider
    {
        private readonly IRuntimeModeProvider _fallback;
        private readonly RuntimeModeConfig _config;

        public ConfigurableRuntimeModeProvider(IRuntimeModeProvider fallback, RuntimeModeConfig config)
        {
            _fallback = fallback ?? new UnityRuntimeModeProvider();
            _config = config;
        }

        public RuntimeMode Current
        {
            get
            {
                if (_config == null)
                {
                    return _fallback.Current;
                }

                switch (_config.modeOverride)
                {
                    case RuntimeModeOverride.ForceStrict:
                        return RuntimeMode.Strict;
                    case RuntimeModeOverride.ForceRelease:
                        return RuntimeMode.Release;
                    case RuntimeModeOverride.Auto:
                    default:
                        return _fallback.Current;
                }
            }
        }

        public bool IsStrict => Current == RuntimeMode.Strict;
    }
}
