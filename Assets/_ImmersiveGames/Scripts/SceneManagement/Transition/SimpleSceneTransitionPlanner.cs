using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.SceneManagement.Configs;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SceneManagement.Transition
{
    /// <summary>
    /// Implementação básica do planner:
    /// - ScenesToLoad  = targetScenes - currentState.LoadedScenes
    /// - ScenesToUnload = currentState.LoadedScenes - targetScenes,
    ///   desconsiderando cenas persistentes (como a UIGlobalScene).
    /// - TargetActiveScene:
    ///     - usa explicitTargetActiveScene se não for vazio;
    ///     - senão, usa a primeira cena do targetScenes;
    ///     - se targetScenes estiver vazio, mantém a ActiveScene atual.
    /// 
    /// Fase 1:
    /// - Suporta tanto o modo legado (listas de strings) quanto o modo novo
    ///   baseado em SceneGroupProfile / SceneTransitionProfile.
    /// 
    /// IMPORTANTE:
    /// - Esta classe NÃO herda de MonoBehaviour.
    /// - Pode ser instanciada normalmente com "new" no DependencyBootstrapper.
    /// </summary>
    public sealed class SimpleSceneTransitionPlanner : ISceneTransitionPlanner
    {
        // Cenas que nunca devem ser descarregadas automaticamente
        private readonly HashSet<string> _persistentScenes;

        // Perfil de transição default (opcional). Pode ser null.
        private readonly SceneTransitionProfile _defaultTransitionProfile;

        /// <summary>
        /// Construtor default:
        /// - Marca "UIGlobalScene" como cena persistente;
        /// - Não define perfil de transição default (null).
        /// </summary>
        public SimpleSceneTransitionPlanner()
            : this(new[] { "UIGlobalScene" }, null)
        {
        }

        /// <summary>
        /// Construtor com configuração explícita.
        /// Permite injetar lista de cenas persistentes e um perfil default.
        /// </summary>
        public SimpleSceneTransitionPlanner(
            IEnumerable<string> persistentScenes,
            SceneTransitionProfile defaultTransitionProfile)
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
                    "[Planner] currentState é null. Retornando contexto vazio.");
                return new SceneTransitionContext(
                    scenesToLoad: new List<string>(),
                    scenesToUnload: new List<string>(),
                    targetActiveScene: string.Empty,
                    useFade: useFade);
            }

            var targetList = targetScenes != null
                ? targetScenes.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList()
                : new List<string>();

            var (toLoad, toUnload) = ComputeDiff(
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
                    "[Planner] BuildContext(currentState, targetGroup): currentState é null. " +
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
                    "[Planner] BuildContext(currentState, targetGroup): targetGroup é null. " +
                    "Retornando contexto vazio.");
                return new SceneTransitionContext(
                    scenesToLoad: new List<string>(),
                    scenesToUnload: new List<string>(),
                    targetActiveScene: string.Empty,
                    useFade: false);
            }

            var targetScenes = targetGroup.SceneNames?
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList()
                ?? new List<string>();

            var (toLoad, toUnload) = ComputeDiff(
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
            // - Senão, usa ForceUseFade do grupo;
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
            var toLoad = new List<string>();
            var toUnload = new List<string>();

            var targetSet = new HashSet<string>(targetScenes ?? new List<string>());
            var persistentSet = persistentScenes != null
                ? new HashSet<string>(persistentScenes)
                : new HashSet<string>();

            // Tudo que está no target e não está carregado -> Load
            foreach (var scene in targetSet)
            {
                if (!currentState.LoadedScenes.Contains(scene))
                {
                    toLoad.Add(scene);
                }
            }

            // Tudo que está carregado, não está no target e não é persistente -> Unload
            foreach (var loaded in currentState.LoadedScenes)
            {
                if (!targetSet.Contains(loaded) && !persistentSet.Contains(loaded))
                {
                    toUnload.Add(loaded);
                }
            }

            return (toLoad, toUnload);
        }

        private static string ResolveActiveScene(
            SceneState currentState,
            IReadOnlyList<string> targetScenes,
            string explicitTargetActiveScene)
        {
            // 1) Se o chamador definiu explicitamente, usa isso
            if (!string.IsNullOrWhiteSpace(explicitTargetActiveScene))
                return explicitTargetActiveScene;

            // 2) Se há cenas alvo, usa a primeira
            if (targetScenes is { Count: > 0 })
                return targetScenes[0];

            // 3) Se não há alvo, mantém a atual (não é o caso típico, mas é seguro)
            if (!string.IsNullOrWhiteSpace(currentState.ActiveSceneName))
                return currentState.ActiveSceneName;

            // 4) Último fallback: vazio (sem cena ativa definida)
            return string.Empty;
        }

        #endregion
    }
}
