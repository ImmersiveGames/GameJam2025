using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.Scripts.SceneManagement.Transition
{
    /// <summary>
    /// Representa um snapshot do estado atual de cenas no jogo.
    /// </summary>
    public class SceneState
    {
        /// <summary>
        /// Conjunto de nomes de cenas atualmente carregadas.
        /// </summary>
        public HashSet<string> LoadedScenes { get; } = new HashSet<string>();

        /// <summary>
        /// Nome da cena ativa atual (SceneManager. GetActiveScene()).
        /// Pode ser string. Empty se nenhuma cena válida estiver ativa.
        /// </summary>
        public string ActiveSceneName { get; private set; } = string.Empty;

        public SceneState() { }

        /// <summary>
        /// Cria um SceneState lendo diretamente o SceneManager.
        /// Não usa reflexão nem corrotinas.
        /// </summary>
        public static SceneState FromSceneManager()
        {
            var state = new SceneState();

            // Coleta todas as cenas carregadas
            int count = SceneManager.sceneCount;
            for (int i = 0; i < count; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded)
                    state.LoadedScenes.Add(scene.name);
            }

            // Cena ativa
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isLoaded)
                state.ActiveSceneName = activeScene.name;
            else
                state.ActiveSceneName = string.Empty;

            Debug.Log(
                "[SceneState] Snapshot criado. " +
                "Loaded=[" + string.Join(", ", state.LoadedScenes) + "] | " +
                "Active='" + state.ActiveSceneName + "'"
            );

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
