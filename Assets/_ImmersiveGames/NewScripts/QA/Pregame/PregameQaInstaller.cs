#nullable enable
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Pregame
{
    public sealed class PregameQaInstaller : MonoBehaviour
    {
        private static PregameQaInstaller _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (_instance != null)
            {
                return;
            }

            var go = new GameObject("QA.Pregame");
            _instance = go.AddComponent<PregameQaInstaller>();
            go.AddComponent<PregameQaContextMenu>();
            DontDestroyOnLoad(go);

            DebugUtility.Log<PregameQaInstaller>(
                "[QA][Pregame] PregameQaContextMenu instalado (DontDestroyOnLoad).",
                DebugUtility.Colors.Info);
        }
    }
}
