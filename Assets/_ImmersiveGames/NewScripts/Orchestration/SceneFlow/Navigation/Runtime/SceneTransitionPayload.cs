namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime
{
    /// <summary>
    /// Payload semântico da navegação.
    ///
    /// F3 (Route como fonte única):
    /// - NÃO contém dados de cena (load/unload/active).
    /// - Scene data é resolvido exclusivamente por SceneRouteDefinition.
    /// </summary>
    public sealed class SceneTransitionPayload
    {
        public static SceneTransitionPayload Empty { get; } = new SceneTransitionPayload();

        private SceneTransitionPayload() { }

        public override string ToString() => "<empty>";
    }
}
