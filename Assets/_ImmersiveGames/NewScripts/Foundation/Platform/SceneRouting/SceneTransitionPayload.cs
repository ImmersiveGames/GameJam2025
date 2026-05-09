namespace _ImmersiveGames.NewScripts.Foundation.Platform.SceneRouting
{
    public enum SceneTransitionGameplayEntryKind
    {
        None = 0,
        InitialEntry = 1,
        Reentry = 2
    }

    /// <summary>
    /// Payload semântico da navegação.
    ///
    /// F3 (Route como fonte única):
    /// - NÃO contém dados de cena (load/unload/active).
    /// - Scene data é resolvido exclusivamente por SceneRouteDefinition.
    ///
    /// O marcador de gameplay entry é contrato de origem do rail gameplay.
    /// Ele não substitui route data, reset policy nem reason textual.
    /// </summary>
    public sealed class SceneTransitionPayload
    {
        public static SceneTransitionPayload Empty { get; } = new SceneTransitionPayload(SceneTransitionGameplayEntryKind.None);
        public static SceneTransitionPayload GameplayInitialEntry { get; } = new SceneTransitionPayload(SceneTransitionGameplayEntryKind.InitialEntry);
        public static SceneTransitionPayload GameplayReentry { get; } = new SceneTransitionPayload(SceneTransitionGameplayEntryKind.Reentry);

        public SceneTransitionGameplayEntryKind GameplayEntryKind { get; }
        public bool IsGameplayInitialEntry => GameplayEntryKind == SceneTransitionGameplayEntryKind.InitialEntry;
        public bool IsGameplayReentry => GameplayEntryKind == SceneTransitionGameplayEntryKind.Reentry;

        private SceneTransitionPayload(SceneTransitionGameplayEntryKind gameplayEntryKind)
        {
            GameplayEntryKind = gameplayEntryKind;
        }

        public static SceneTransitionPayload ForGameplayEntry(SceneTransitionGameplayEntryKind gameplayEntryKind)
        {
            return gameplayEntryKind switch
            {
                SceneTransitionGameplayEntryKind.InitialEntry => GameplayInitialEntry,
                SceneTransitionGameplayEntryKind.Reentry => GameplayReentry,
                _ => Empty
            };
        }

        public override string ToString()
        {
            return GameplayEntryKind == SceneTransitionGameplayEntryKind.None
                ? "<empty>"
                : $"gameplayEntryKind='{GameplayEntryKind}'";
        }
    }
}
