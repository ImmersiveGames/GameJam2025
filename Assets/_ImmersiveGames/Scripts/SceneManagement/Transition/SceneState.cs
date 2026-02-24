using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.Scripts.SceneManagement.Transition
{
    /// <summary>
    /// Representa um snapshot do estado atual de cenas no jogo.
    ///
    /// � usado pelo planner para decidir:
    /// - quais cenas devem ser carregadas;
    /// - quais podem ser descarregadas;
    /// - qual � a cena ativa atual.
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

        // Evita spam: suprime logs repetidos no MESMO frame quando o snapshot � id�ntico.
        private static int _lastLoggedFrame = -1;
        private static string _lastLoggedKey;

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

            LogSnapshotIfNeeded(state);

            return state;
        }

        private static void LogSnapshotIfNeeded(SceneState state)
        {
            // Gera uma chave determin�stica para comparar snapshots.
            // Ordenar evita diferen�as por ordem de itera��o do HashSet.
            string loaded = string.Join(", ", state.LoadedScenes.OrderBy(s => s));
            string key = loaded + "|" + state.ActiveSceneName;

            int frame = Time.frameCount;

            // Se for o mesmo frame e o snapshot � igual, n�o loga novamente.
            if (_lastLoggedFrame == frame && _lastLoggedKey == key)
                return;

            _lastLoggedFrame = frame;
            _lastLoggedKey = key;

            DebugUtility.LogVerbose<SceneState>(
                "[SceneState] Snapshot criado. Loaded=[" +
                loaded +
                "] | Active='" + state.ActiveSceneName + "'");
        }

        public override string ToString()
        {
            return "SceneState(Loaded=[" +
                   string.Join(", ", LoadedScenes) +
                   "], Active='" + ActiveSceneName + "')";
        }
    }
}


