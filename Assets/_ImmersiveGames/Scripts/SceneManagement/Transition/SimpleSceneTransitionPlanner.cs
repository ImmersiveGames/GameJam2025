﻿using System.Collections.Generic;
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
    /// </summary>
    public class SimpleSceneTransitionPlanner : ISceneTransitionPlanner
    {
        /// <summary>
        /// Cenas consideradas "persistentes" pelo planner e que
        /// nunca devem ser incluídas em ScenesToUnload.
        /// </summary>
        private static readonly HashSet<string> PersistentScenes = new HashSet<string>
        {
            "UIGlobalScene"
        };

        public SceneTransitionContext BuildContext(
            SceneState currentState,
            IReadOnlyList<string> targetScenes,
            string explicitTargetActiveScene,
            bool useFade)
        {
            if (currentState == null)
            {
                DebugUtility.LogWarning<SimpleSceneTransitionPlanner>(
                    "currentState nulo; usando estado vazio.");
                currentState = new SceneState();
            }

            targetScenes ??= new List<string>();

            // Conjunto das cenas alvo
            var targetSet = new HashSet<string>(targetScenes);

            var scenesToLoad = new List<string>();
            var scenesToUnload = new List<string>();

            // ScenesToLoad = target - loaded
            foreach (var scene in targetSet)
            {
                if (!currentState.LoadedScenes.Contains(scene))
                {
                    scenesToLoad.Add(scene);
                }
            }

            // ScenesToUnload = loaded - target, ignorando cenas persistentes
            foreach (var loadedScene in currentState.LoadedScenes)
            {
                // Nunca descarrega cenas persistentes
                if (PersistentScenes.Contains(loadedScene))
                    continue;

                if (!targetSet.Contains(loadedScene))
                {
                    scenesToUnload.Add(loadedScene);
                }
            }

            var targetActive = ResolveTargetActiveScene(
                currentState,
                targetScenes,
                explicitTargetActiveScene);

            var context = new SceneTransitionContext(
                scenesToLoad,
                scenesToUnload,
                targetActive,
                useFade);

            DebugUtility.Log<SimpleSceneTransitionPlanner>(
                $"Contexto criado: {context}",
                DebugUtility.Colors.Info);

            return context;
        }

        /// <summary>
        /// Resolve qual será a cena ativa após a transição.
        /// </summary>
        private static string ResolveTargetActiveScene(
            SceneState currentState,
            IReadOnlyList<string> targetScenes,
            string explicitTargetActiveScene)
        {
            // 1) Se veio explicitTargetActiveScene, usa ele
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
    }
}
