#if UNITY_EDITOR || DEVELOPMENT_BUILD || NEWSCRIPTS_QA
using System.Collections;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Navigation;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.WorldLifecycle
{
    public sealed class Marco0PhaseObservabilityQaRunner : MonoBehaviour
    {
        public const string RunKey = "NewScripts.QA.Marco0PhaseObservability.RunRequested";

        private const string RunnerName = "[QA] Marco0PhaseObservabilityQaRunner";
        private const string StartLog = "MARCO0_START";
        private const string EndLog = "MARCO0_END";

        private IGameNavigationService _navigation;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (PlayerPrefs.GetInt(RunKey, 0) != 1)
                return;

            PlayerPrefs.SetInt(RunKey, 0);
            PlayerPrefs.Save();

            var go = new GameObject(RunnerName);
            DontDestroyOnLoad(go);
            go.AddComponent<Marco0PhaseObservabilityQaRunner>();
        }

        private void Start()
        {
            Debug.Log($"{StartLog} runner iniciado (gated by PlayerPrefs RunKey).");
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            yield return WaitForNavigationService(10f);

            if (_navigation != null)
            {
                var task = _navigation.RequestMenuAsync("qa_marco0");
                yield return WaitForTask(task, 20f, "Timeout aguardando RequestMenuAsync.");
            }
            else
            {
                Debug.Log("[QA][Marco0] IGameNavigationService indisponível. Nenhuma navegação será solicitada.");
            }

            Debug.Log($"{EndLog} runner finalizado.");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private IEnumerator WaitForNavigationService(float timeout)
        {
            var deadline = Time.realtimeSinceStartup + timeout;

            while (Time.realtimeSinceStartup < deadline)
            {
                if (_navigation == null &&
                    DependencyManager.Provider.TryGetGlobal<IGameNavigationService>(out var nav) && nav != null)
                {
                    _navigation = nav;
                    yield break;
                }

                yield return null;
            }
        }

        private static IEnumerator WaitForTask(Task task, float timeout, string timeoutMessage)
        {
            if (task == null)
            {
                Debug.LogWarning("[QA][Marco0] Task nula ao aguardar RequestMenuAsync.");
                yield break;
            }

            var deadline = Time.realtimeSinceStartup + timeout;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            if (!task.IsCompleted)
            {
                Debug.LogWarning($"[QA][Marco0] {timeoutMessage}");
            }
        }
    }
}
#endif
