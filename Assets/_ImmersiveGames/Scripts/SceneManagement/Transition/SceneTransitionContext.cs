using System.Collections.Generic;

namespace _ImmersiveGames.Scripts.SceneManagement.Transition
{
    /// <summary>
    /// Descreve o plano de uma transição de cenas:
    /// quais carregar, quais descarregar, qual será a cena ativa e se deve usar fade.
    /// </summary>
    public readonly struct SceneTransitionContext
    {
        // Fields "legados" (mantidos para compatibilidade)
        public readonly IReadOnlyList<string> scenesToLoad;
        public readonly IReadOnlyList<string> scenesToUnload;
        public readonly string targetActiveScene;
        public readonly bool useFade;

        // Propriedades modernas (usadas pelo SceneTransitionService)
        public IReadOnlyList<string> ScenesToLoad => scenesToLoad;
        public IReadOnlyList<string> ScenesToUnload => scenesToUnload;
        public string TargetActiveScene => targetActiveScene;
        public bool UseFade => useFade;

        public SceneTransitionContext(
            IReadOnlyList<string> scenesToLoad,
            IReadOnlyList<string> scenesToUnload,
            string targetActiveScene,
            bool useFade)
        {
            this.scenesToLoad = scenesToLoad;
            this.scenesToUnload = scenesToUnload;
            this.targetActiveScene = targetActiveScene;
            this.useFade = useFade;
        }

        public override string ToString()
        {
            string loadStr = scenesToLoad == null ? "null" : string.Join(", ", scenesToLoad);
            string unloadStr = scenesToUnload == null ? "null" : string.Join(", ", scenesToUnload);

            return $"SceneTransitionContext(Load=[{loadStr}], Unload=[{unloadStr}], " +
                $"TargetActive='{targetActiveScene}', UseFade={useFade})";
        }
    }
}