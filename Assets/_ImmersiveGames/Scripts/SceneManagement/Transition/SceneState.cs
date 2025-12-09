using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.Scripts.SceneManagement.Transition
{
    /// <summary>
    /// Representa um snapshot do estado atual de cenas no jogo.
    /// 
    /// É usado pelo planner para decidir:
    /// - quais cenas devem ser carregadas;
    /// - quais podem ser descarregadas;
    /// - qual é a cena ativa atual.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SceneState
    {
        /// <summary>
        /// Conjunto de nomes de cenas atualmente carregadas.
        /// </summary>
        public HashSet<string> LoadedScenes { get; } = new HashSet<string>();

        /// <summary>
        /// Nome da cena ativa no momento do snapshot.
        /// </summary>
        public string ActiveSceneName { get; private set; }

        private SceneState()
        {
        }

        /// <summary>
        /// Captura o estado atual de cenas da Unity (SceneManager).
        /// </summary>
        public static SceneState Capture()
        {
            var state = new SceneState();

            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded)
                {
                    state.LoadedScenes.Add(scene.name);
                }
            }

            var activeScene = SceneManager.GetActiveScene();
            state.ActiveSceneName = activeScene.IsValid() ? activeScene.name : string.Empty;

            DebugUtility.LogVerbose<SceneState>(
                "[SceneState] Snapshot criado. Loaded=[" +
                string.Join(", ", state.LoadedScenes) +
                "] | Active='" + state.ActiveSceneName + "'");

            return state;
        }

        public override string ToString()
        {
            return "SceneState(Loaded=[" +
                   string.Join(", ", LoadedScenes) +
                   "], Active='" + ActiveSceneName + "')";
        }
    }
}
