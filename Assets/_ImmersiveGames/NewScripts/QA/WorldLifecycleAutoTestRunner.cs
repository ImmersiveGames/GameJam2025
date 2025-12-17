using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.World;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA
{
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class WorldLifecycleAutoTestRunner : MonoBehaviour
    {
        [Header("Runner")]
        [SerializeField] private string label = "WorldLifecycleAutoTestRunner";
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private int warmupFrames = 1;

        [Tooltip("Se verdadeiro, evita rodar o runner mais de uma vez por instância, mesmo que o GameObject seja reativado.")]
        [SerializeField] private bool runOnlyOnce = true;

        [Tooltip("Se verdadeiro, desabilita o componente ao terminar (evita re-run acidental).")]
        [SerializeField] private bool disableAfterRun = true;

        [Tooltip("Se verdadeiro, destrói o GameObject ao terminar (bom para QA descartável).")]
        [SerializeField] private bool destroyAfterRun = false;

        [Header("Test Plan")]
        [SerializeField] private bool runSingleReset = true;
        [SerializeField] private bool runStressReset = true;

        [SerializeField] private int stressCount = 3;

        [Tooltip("Delay entre iterações do stress (em segundos).")]
        [SerializeField] private float stressDelaySeconds = 0.25f;

        [Tooltip("Insere um gap mínimo (yield/delay) entre o SingleReset e o primeiro Stress#1, evitando warnings de 'mesmo frame'.")]
        [SerializeField] private float preStressDelaySeconds = 0.05f;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = true;

        private WorldLifecycleController _controller;
        private string _sceneName;
        private bool _hasRun;

        private void Awake()
        {
            _sceneName = gameObject.scene.name;

            // Detectar duplicatas cedo (o caso mais comum do "rodou duas vezes").
            var runners = FindObjectsByType<WorldLifecycleAutoTestRunner>(FindObjectsSortMode.None);
            if (runners != null && runners.Length > 1)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleAutoTestRunner),
                    $"[QA] {label}: Detectados {runners.Length} runners na cena '{_sceneName}'. " +
                    $"Isso pode causar AutoTest duplicado. Este runner instanceId={GetInstanceID()}");
            }

            _controller = FindFirstObjectByType<WorldLifecycleController>();
            if (_controller == null)
            {
                DebugUtility.LogError(typeof(WorldLifecycleAutoTestRunner),
                    $"[QA] {label}: WorldLifecycleController não encontrado na cena '{_sceneName}'.");
                return;
            }

            // Opção B: runner controla o Start do controller.
            _controller.AutoInitializeOnStart = false;

            if (verboseLogs)
            {
                DebugUtility.Log(typeof(WorldLifecycleAutoTestRunner),
                    $"[QA] {label}: AutoInitializeOnStart=false aplicado no controller (scene='{_sceneName}').");
            }
        }

        private void Start()
        {
            if (!runOnStart)
            {
                if (verboseLogs)
                {
                    DebugUtility.Log(typeof(WorldLifecycleAutoTestRunner),
                        $"[QA] {label}: runOnStart=false, runner inativo (scene='{_sceneName}').");
                }
                return;
            }

            if (runOnlyOnce && _hasRun)
            {
                if (verboseLogs)
                {
                    DebugUtility.LogWarning(typeof(WorldLifecycleAutoTestRunner),
                        $"[QA] {label}: Start ignorado (já executado). scene='{_sceneName}'.");
                }
                return;
            }

            _ = RunAsync();
        }

        private async Task RunAsync()
        {
            if (_controller == null)
            {
                return;
            }

            if (runOnlyOnce && _hasRun)
            {
                return;
            }

            _hasRun = true;

            DebugUtility.Log(typeof(WorldLifecycleAutoTestRunner),
                $"[QA] {label}: AutoTestRunner starting (scene='{_sceneName}', warmupFrames={warmupFrames}).");

            // Warmup frames (estabiliza boot/DI/registries).
            for (int i = 0; i < Mathf.Max(0, warmupFrames); i++)
            {
                await Task.Yield();
            }

            // Reset único
            if (runSingleReset)
            {
                await _controller.ResetWorldAsync($"{label}/SingleReset");
            }

            // Gap mínimo antes do stress (evita warnings de "mesmo frame")
            if (runStressReset)
            {
                if (preStressDelaySeconds > 0f)
                {
                    var ms = Mathf.Max(1, (int)(preStressDelaySeconds * 1000f));
                    await Task.Delay(ms);
                }
                else
                {
                    await Task.Yield();
                }
            }

            // Stress
            if (runStressReset)
            {
                if (stressCount <= 0)
                {
                    DebugUtility.LogWarning(typeof(WorldLifecycleAutoTestRunner),
                        $"[QA] {label}: stressCount inválido ({stressCount}). Ignorando stress.");
                }
                else
                {
                    DebugUtility.Log(typeof(WorldLifecycleAutoTestRunner),
                        $"[QA] {label}: Running stress reset (count={stressCount}, delay={stressDelaySeconds:0.00}s).");

                    for (int i = 0; i < stressCount; i++)
                    {
                        await _controller.ResetWorldAsync($"{label}/Stress#{i + 1}");

                        if (i < stressCount - 1 && stressDelaySeconds > 0f)
                        {
                            var delayMs = Mathf.Max(1, (int)(stressDelaySeconds * 1000f));
                            await Task.Delay(delayMs);
                        }
                        else
                        {
                            // Sempre dá pelo menos 1 yield entre resets para não colar tudo no mesmo frame.
                            await Task.Yield();
                        }
                    }
                }
            }

            DebugUtility.Log(typeof(WorldLifecycleAutoTestRunner),
                $"[QA] {label}: AutoTestRunner finished (scene='{_sceneName}').");

            if (disableAfterRun)
            {
                enabled = false;
            }

            if (destroyAfterRun)
            {
                Destroy(gameObject);
            }
        }

        [ContextMenu("QA/Run AutoTest Now")]
        public void RunNow()
        {
            _ = RunAsync();
        }
    }
}
