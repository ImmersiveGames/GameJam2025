namespace _ImmersiveGames.NewScripts.Infrastructure.Mode
{
    /// <summary>
    /// Resolve o modo de runtime baseado nas flags padrão do Unity.
    /// - Strict: UNITY_EDITOR || DEVELOPMENT_BUILD
    /// - Release: caso contrário
    /// </summary>
    public sealed class UnityRuntimeModeProvider : IRuntimeModeProvider
    {
        public RuntimeMode Current
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return RuntimeMode.Strict;
#else
                return RuntimeMode.Release;
#endif
            }
        }

        public bool IsStrict => Current == RuntimeMode.Strict;
    }
}
