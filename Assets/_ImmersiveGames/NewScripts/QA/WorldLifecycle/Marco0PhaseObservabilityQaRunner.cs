#if UNITY_EDITOR || DEVELOPMENT_BUILD || NEWSCRIPTS_QA
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.WorldLifecycle
{
    public sealed class Marco0PhaseObservabilityQaRunner : MonoBehaviour
    {
        public const string RunKey = "NewScripts.QA.Marco0PhaseObservability.RunRequested";

        // Guard de sessão: garante 1 execução por Play Mode, mesmo que Bootstrap rode mais de uma vez.
        private const string SessionKey = "NewScripts.QA.Marco0PhaseObservability.RanThisPlay";

        private const string RunnerName = "[QA] Marco0PhaseObservabilityQaRunner";
        private const string StartLog = "MARCO0_START";
        private const string EndLog = "MARCO0_END";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            // Só roda se foi armado explicitamente.
            if (PlayerPrefs.GetInt(RunKey, 0) != 1)
                return;

            // Se já rodou nesta sessão de Play, não roda de novo.
            if (PlayerPrefs.GetInt(SessionKey, 0) == 1)
                return;

            // Consome a flag e marca sessão imediatamente (idempotência).
            PlayerPrefs.SetInt(RunKey, 0);
            PlayerPrefs.SetInt(SessionKey, 1);
            PlayerPrefs.Save();

            var go = new GameObject(RunnerName);
            DontDestroyOnLoad(go);
            go.AddComponent<Marco0PhaseObservabilityQaRunner>();
        }

        private void Start()
        {
            Debug.Log($"{StartLog} runner iniciado (opt-in via RunKey).");
            Debug.Log($"{EndLog} runner finalizado.");
        }

#if UNITY_EDITOR
        // Opcional: limpar o SessionKey automaticamente ao sair do Play, mas isso fica melhor fora do runner.
#endif
    }
}
#endif
