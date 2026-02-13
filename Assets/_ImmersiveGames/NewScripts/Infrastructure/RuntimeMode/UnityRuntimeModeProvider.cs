namespace _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode
{
    /// <summary>
    /// Resolve o modo de runtime baseado nas flags padrão do Unity.
    /// - Strict: qualquer execução fora do Editor (inclui Player Build)
    /// - Strict: UNITY_EDITOR && DEVELOPMENT_BUILD
    /// - Release: UNITY_EDITOR sem DEVELOPMENT_BUILD
    /// </summary>
    public sealed class UnityRuntimeModeProvider : IRuntimeModeProvider
    {
        public RuntimeMode Current
        {
            get
            {
#if !UNITY_EDITOR
                return RuntimeMode.Strict;
#elif DEVELOPMENT_BUILD
                return RuntimeMode.Strict;
#else
                return RuntimeMode.Release;
#endif
            }
        }

        public bool IsStrict => Current == RuntimeMode.Strict;
    }
}
