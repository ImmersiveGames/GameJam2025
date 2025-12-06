using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SceneManagement.Transition
{
    /// <summary>
    /// Implementação básica do planner:
    /// - ScenesToLoad  = targetScenes - currentState.LoadedScenes
    /// - ScenesToUnload = currentState.LoadedScenes - targetScenes
    /// - TargetActiveScene:
    ///     - usa explicitTargetActiveScene se não for vazio;
    ///     - senão, usa a primeira cena do targetScenes;
    ///     - se targetScenes estiver vazio, mantém a ActiveScene atual.
    /// </summary>
    public class SimpleSceneTransitionPlanner : ISceneTransitionPlanner
    {
        public SceneTransitionContext BuildContext(
            SceneState currentState,
            IReadOnlyList<string> targetScenes,
            string explicitTargetActiveScene,
            bool useFade)
        {
            if (currentState == null)
            {
                Debug.LogError("[SimpleSceneTransitionPlanner] currentState é null.");
                return default;
            }

            targetScenes ??= new List<string>();

            // Normaliza lista de alvo em um HashSet para diffs
            var targetSet = new HashSet<string>(targetScenes);

            var scenesToLoad = new List<string>();
            var scenesToUnload = new List<string>();

            // Quais precisamos carregar? (estão no target, mas não estão carregadas ainda)
            foreach (var targetScene in targetSet)
            {
                if (!currentState.LoadedScenes.Contains(targetScene))
                    scenesToLoad.Add(targetScene);
            }

            // Quais precisamos descarregar? (estão carregadas, mas não fazem parte do target)
            foreach (var loaded in currentState.LoadedScenes)
            {
                if (!targetSet.Contains(loaded))
                    scenesToUnload.Add(loaded);
            }

            // Determinar cena ativa alvo
            string targetActiveScene = DetermineTargetActiveScene(
                currentState,
                targetScenes,
                explicitTargetActiveScene);

            var context = new SceneTransitionContext(
                scenesToLoad,
                scenesToUnload,
                targetActiveScene,
                useFade);

            Debug.Log($"[SimpleSceneTransitionPlanner] Contexto criado: {context}");

            return context;
        }

        private string DetermineTargetActiveScene(
            SceneState currentState,
            IReadOnlyList<string> targetScenes,
            string explicitTargetActiveScene)
        {
            // 1) Preferência: valor explícito
            if (!string.IsNullOrWhiteSpace(explicitTargetActiveScene))
                return explicitTargetActiveScene;

            // 2) Se há cenas alvo, usa a primeira
            if (targetScenes != null && targetScenes.Count > 0)
                return targetScenes[0];

            // 3) Se não há alvo, mantém a atual (não é o caso típico, mas é seguro)
            if (!string.IsNullOrWhiteSpace(currentState.ActiveSceneName))
                return currentState.ActiveSceneName;

            // 4) Último fallback: vazio (sem cena ativa definida)
            return string.Empty;
        }
    }
}
