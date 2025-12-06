using System.Collections.Generic;

namespace _ImmersiveGames.Scripts.SceneManagement.Transition
{
    /// <summary>
    /// Descreve o plano de uma transição de cenas:
    /// quais carregar, quais descarregar, qual será a cena ativa e se deve usar fade.
    /// </summary>
    public struct SceneTransitionContext
    {
        public readonly IReadOnlyList<string> ScenesToLoad;
        public readonly IReadOnlyList<string> ScenesToUnload;
        public readonly string TargetActiveScene;
        public readonly bool UseFade;

        public SceneTransitionContext(
            IReadOnlyList<string> scenesToLoad,
            IReadOnlyList<string> scenesToUnload,
            string targetActiveScene,
            bool useFade)
        {
            ScenesToLoad = scenesToLoad;
            ScenesToUnload = scenesToUnload;
            TargetActiveScene = targetActiveScene;
            UseFade = useFade;
        }

        public override string ToString()
        {
            var loadStr = ScenesToLoad == null ? "null" : string.Join(", ", ScenesToLoad);
            var unloadStr = ScenesToUnload == null ? "null" : string.Join(", ", ScenesToUnload);
            return $"SceneTransitionContext(Load=[{loadStr}], Unload=[{unloadStr}], " +
                $"TargetActive='{TargetActiveScene}', UseFade={UseFade})";
        }
    }
}