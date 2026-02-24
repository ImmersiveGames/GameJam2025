using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.Scripts.SceneManagement.Configs;
namespace _ImmersiveGames.Scripts.SceneManagement.Transition
{
    /// <summary>
    /// Implementa��o b�sica do planner:
    /// - ScenesToLoad  = targetScenes - currentState. LoadedScenes
    /// - ScenesToUnload = currentState.LoadedScenes - targetScenes,
    ///   desconsiderando cenas persistentes (como a UIGlobalScene e FadeScene).
    /// - TargetActiveScene:
    ///     - usa explicitTargetActiveScene se n�o for vazio;
    ///     - sen�o, usa a primeira cena do targetScenes;
    ///     - se targetScenes estiver vazio, mant�m a ActiveScene atual.
    ///
    /// IMPORTANTE:
    /// - Esta classe N�O herda de MonoBehaviour.
    /// - Pode ser instanciada normalmente com "new" no DependencyBootstrapper.
    /// </summary>
    public sealed class SimpleSceneTransitionPlanner : ISceneTransitionPlanner
    {
        private const string DefaultUIGlobalSceneName = "UIGlobalScene";
        private const string DefaultFadeSceneName = "FadeScene";

        // Cenas que nunca devem ser descarregadas automaticamente
        private readonly HashSet<string> _persistentScenes;

        // Perfil de transi��o default (opcional). Pode ser null.
        private readonly OldSceneTransitionProfile _defaultTransitionProfile;

        /// <summary>
        /// Construtor default:
        /// - Marca "UIGlobalScene" e "FadeScene" como cenas persistentes;
        /// - N�o define perfil de transi��o default (null).
        /// </summary>
        public SimpleSceneTransitionPlanner()
            : this(new[] { DefaultUIGlobalSceneName, DefaultFadeSceneName }, null)
        {
        }

        /// <summary>
        /// Construtor com configura��o expl�cita.
        /// Permite injetar lista de cenas persistentes e um perfil default.
        /// </summary>
        private SimpleSceneTransitionPlanner(
            IEnumerable<string> persistentScenes,
            OldSceneTransitionProfile defaultTransitionProfile)
        {
            _persistentScenes = persistentScenes != null
                ? new HashSet<string>(persistentScenes.Where(s => !string.IsNullOrWhiteSpace(s)))
                : new HashSet<string>();

            _defaultTransitionProfile = defaultTransitionProfile;
        }

        #region ISceneTransitionPlanner (legado)

        public SceneTransitionContext BuildContext(
            SceneState currentState,
            IReadOnlyList<string> targetScenes,
            string explicitTargetActiveScene,
            bool useFade)
        {
            if (currentState == null)
            {
                DebugUtility.LogWarning<SimpleSceneTransitionPlanner>(
                    "[Planner] currentState � null. Retornando contexto vazio.");
                return new SceneTransitionContext(
                    scenesToLoad: new List<string>(),
                    scenesToUnload: new List<string>(),
                    targetActiveScene: string.Empty,
                    useFade: useFade);
            }

            List<string> targetList = targetScenes != null
                ? targetScenes.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList()
                : new List<string>();

            (List<string> toLoad, List<string> toUnload) = ComputeDiff(
                currentState,
                targetList,
                _persistentScenes);

            string targetActive = ResolveActiveScene(
                currentState,
                targetList,
                explicitTargetActiveScene);

            return new SceneTransitionContext(
                scenesToLoad: toLoad,
                scenesToUnload: toUnload,
                targetActiveScene: targetActive,
                useFade: useFade);
        }

        #endregion

        #region ISceneTransitionPlanner (SceneGroupProfile)

        public SceneTransitionContext BuildContext(
            SceneState currentState,
            SceneGroupProfile targetGroup)
        {
            if (currentState == null)
            {
                DebugUtility.LogWarning<SimpleSceneTransitionPlanner>(
                    "[Planner] BuildContext(currentState, targetGroup): currentState � null. " +
                    "Retornando contexto vazio.");
                return new SceneTransitionContext(
                    scenesToLoad: new List<string>(),
                    scenesToUnload: new List<string>(),
                    targetActiveScene: string.Empty,
                    useFade: false);
            }

            if (targetGroup == null)
            {
                DebugUtility.LogWarning<SimpleSceneTransitionPlanner>(
                    "[Planner] BuildContext(currentState, targetGroup): targetGroup � null. " +
                    "Retornando contexto vazio.");
                return new SceneTransitionContext(
                    scenesToLoad: new List<string>(),
                    scenesToUnload: new List<string>(),
                    targetActiveScene: string.Empty,
                    useFade: false);
            }

            List<string> targetScenes = targetGroup.SceneNames?
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList()
                ?? new List<string>();

            (List<string> toLoad, List<string> toUnload) = ComputeDiff(
                currentState,
                targetScenes,
                _persistentScenes);

            string explicitTargetActiveScene = !string.IsNullOrWhiteSpace(targetGroup.ActiveSceneName)
                ? targetGroup.ActiveSceneName
                : null;

            string targetActive = ResolveActiveScene(
                currentState,
                targetScenes,
                explicitTargetActiveScene);

            // Decide o uso de fade:
            // - Se perfil existir, UseFade dele prevalece;
            // - Sen�o, usa ForceUseFade do grupo;
            // - Fallback: true.
            var profile = targetGroup.TransitionProfile ?? _defaultTransitionProfile;
            bool useFade = profile != null
                ? profile.UseFade
                : targetGroup.ForceUseFade;

            var context = new SceneTransitionContext(
                scenesToLoad: toLoad,
                scenesToUnload: toUnload,
                targetActiveScene: targetActive,
                useFade: useFade,
                transitionProfile: profile,
                fromGroupProfile: null,
                toGroupProfile: targetGroup);

            DebugUtility.Log<SimpleSceneTransitionPlanner>(
                "[Planner] Contexto criado a partir de SceneGroupProfile: " + context,
                DebugUtility.Colors.Info);

            return context;
        }

        public SceneTransitionContext BuildContext(
            SceneState currentState,
            SceneGroupProfile fromGroup,
            SceneGroupProfile toGroup)
        {
            var baseContext = BuildContext(currentState, toGroup);

            return new SceneTransitionContext(
                scenesToLoad: baseContext.scenesToLoad,
                scenesToUnload: baseContext.scenesToUnload,
                targetActiveScene: baseContext.targetActiveScene,
                useFade: baseContext.useFade,
                transitionProfile: baseContext.transitionProfile,
                fromGroupProfile: fromGroup,
                toGroupProfile: toGroup);
        }

        #endregion

        #region Helpers

        private static (List<string> toLoad, List<string> toUnload) ComputeDiff(
            SceneState currentState,
            IReadOnlyList<string> targetScenes,
            IReadOnlyCollection<string> persistentScenes)
        {
            var targetSet = new HashSet<string>(targetScenes ?? new List<string>());

            HashSet<string> persistentSet = persistentScenes != null
                ? new HashSet<string>(persistentScenes)
                : new HashSet<string>();

            // Tudo que est� no alvo e n�o est� carregado ? Load
            var toLoad = targetSet
                .Where(scene => !currentState.LoadedScenes.Contains(scene))
                .ToList();

            // Tudo que est� carregado, n�o est� no alvo e n�o � persistente ? Unload
            var toUnload = currentState.LoadedScenes
                .Where(loaded => !targetSet.Contains(loaded) && !persistentSet.Contains(loaded))
                .ToList();

            return (toLoad, toUnload);
        }

        private static string ResolveActiveScene(
            SceneState currentState,
            IReadOnlyList<string> targetScenes,
            string explicitTargetActiveScene)
        {
            if (!string.IsNullOrWhiteSpace(explicitTargetActiveScene))
                return explicitTargetActiveScene;

            if (targetScenes is { Count: > 0 })
                return targetScenes[0];

            if (!string.IsNullOrWhiteSpace(currentState.ActiveSceneName))
                return currentState.ActiveSceneName;

            return string.Empty;
        }

        #endregion
    }
}


