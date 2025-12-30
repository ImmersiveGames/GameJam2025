using System;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Gate;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    /// <summary>
    /// Self-test para diagnosticar "ownership" do token de pausa.
    /// Mantém um handle externo ativo para o token 'state.pause' e orienta o operador
    /// a abrir/fechar o PauseOverlay, validando que:
    /// - O overlay não "rouba" ownership nem libera handles que não são dele.
    /// - O gate só reabre quando TODOS os owners liberam.
    ///
    /// Controles (runtime):
    /// - Hotkeys (opcional): Acquire / Release / PrintStatus
    /// - OnGUI (opcional): botões simples na tela
    /// - ContextMenu: opções via menu do componente no Inspector (útil no Editor)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseOwnershipSelfTest : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private bool acquireExternalOnStart = true;
        [SerializeField] private string pauseToken = "state.pause";
        [SerializeField] private bool verbose = true;

        [Header("Runtime Controls (QA)")]
        [SerializeField] private bool enableHotkeys = true;
        [SerializeField] private KeyCode acquireKey = KeyCode.F7;
        [SerializeField] private KeyCode releaseKey = KeyCode.F8;
        [SerializeField] private KeyCode printStatusKey = KeyCode.F9;

        [Header("OnGUI Debug (optional)")]
        [SerializeField] private bool showOnGui = false;
        [SerializeField] private Vector2 onGuiPosition = new Vector2(12, 12);

        [Header("Safety")]
        [Tooltip("Quando true, libera o handle externo ao desabilitar o objeto (evita 'vazar' pause em unload/disable).")]
        [SerializeField] private bool releaseOnDisable = true;

        [Inject] private ISimulationGateService _gate;

        private IDisposable _externalHandle;
        private bool _acquired;

        private void Awake()
        {
            TryResolveGate();
        }

        private void Start()
        {
            if (!TryResolveGate())
            {
                Debug.LogError($"[PauseOwnershipSelfTest] ISimulationGateService não resolvido. " +
                               $"scene='{SceneManager.GetActiveScene().name}'.");
                return;
            }

            if (acquireExternalOnStart)
                AcquireExternal();
        }

        private void Update()
        {
            if (!enableHotkeys)
                return;

            // Evita spam se o serviço ainda não está resolvido.
            if (!TryResolveGate())
                return;

            if (Input.GetKeyDown(acquireKey))
                AcquireExternal();

            if (Input.GetKeyDown(releaseKey))
                ReleaseExternal();

            if (Input.GetKeyDown(printStatusKey))
                PrintStatus("Hotkey/PrintStatus");
        }

        private bool TryResolveGate()
        {
            if (_gate != null)
                return true;

            var provider = DependencyManager.Provider;
            if (provider == null)
                return false;

            try
            {
                provider.InjectDependencies(this);
            }
            catch
            {
                // ignore
            }

            if (_gate != null)
                return true;

            if (provider.TryGetGlobal<ISimulationGateService>(out var gate))
            {
                _gate = gate;
                return true;
            }

            if (provider.TryGet<ISimulationGateService>(out gate))
            {
                _gate = gate;
                return true;
            }

            return false;
        }

        [ContextMenu("QA/PauseOwnershipSelfTest/AcquireExternal")]
        public void AcquireExternal()
        {
            if (!TryResolveGate())
            {
                Debug.LogError("[PauseOwnershipSelfTest] AcquireExternal falhou: ISimulationGateService não resolvido.");
                return;
            }

            if (_acquired)
            {
                Log("External handle já está ACQUIRED (ignorado).");
                PrintStatus("AcquireExternal/Ignored");
                return;
            }

            _externalHandle = _gate.Acquire(pauseToken);
            _acquired = true;

            Log($"[PauseTest] External handle ACQUIRED ({pauseToken}). " +
                $"IsOpen={_gate.IsOpen} Active={_gate.ActiveTokenCount} TokenActive={_gate.IsTokenActive(pauseToken)}");
            Log("[PauseTest] Agora: abra/feche o PauseOverlay. O jogo deve permanecer pausado até ReleaseExternal().");

            PrintStatus("AcquireExternal");
        }

        [ContextMenu("QA/PauseOwnershipSelfTest/ReleaseExternal")]
        public void ReleaseExternal()
        {
            if (!_acquired)
            {
                Log("External handle não está ativo (ignorado).");
                PrintStatus("ReleaseExternal/Ignored");
                return;
            }

            try
            {
                _externalHandle?.Dispose();
            }
            finally
            {
                _externalHandle = null;
                _acquired = false;
            }

            Log($"[PauseTest] External handle RELEASED ({pauseToken}). " +
                $"IsOpen={_gate.IsOpen} Active={_gate.ActiveTokenCount} TokenActive={_gate.IsTokenActive(pauseToken)}");

            PrintStatus("ReleaseExternal");
        }

        [ContextMenu("QA/PauseOwnershipSelfTest/PrintStatus")]
        public void PrintStatusContextMenu()
        {
            if (!TryResolveGate())
            {
                Debug.LogWarning("[PauseOwnershipSelfTest] PrintStatus: ISimulationGateService não resolvido.");
                return;
            }

            PrintStatus("ContextMenu/PrintStatus");
        }

        private void PrintStatus(string source)
        {
            if (!verbose)
                return;

            if (_gate == null)
            {
                Debug.Log($"[PauseOwnershipSelfTest] [PauseTest] Status ({source}): gate=NULL acquired={_acquired}");
                return;
            }

            Debug.Log(
                $"[PauseOwnershipSelfTest] [PauseTest] Status ({source}) => " +
                $"scene='{SceneManager.GetActiveScene().name}', " +
                $"token='{pauseToken}', acquired={_acquired}, " +
                $"IsOpen={_gate.IsOpen}, Active={_gate.ActiveTokenCount}, TokenActive={_gate.IsTokenActive(pauseToken)}");
        }

        private void OnDisable()
        {
            if (!releaseOnDisable)
                return;

            // Importante para evitar "vazar pause" em unload/disable.
            if (_acquired)
                ReleaseExternal();
        }

        private void OnDestroy()
        {
            // Mantém compatibilidade e garante liberação caso OnDisable não rode.
            if (_acquired)
                ReleaseExternal();
        }

        private void OnGUI()
        {
            if (!showOnGui)
                return;

            // UI simples e deliberadamente "feia": é QA.
            const float width = 360f;
            const float height = 22f;
            var x = onGuiPosition.x;
            var y = onGuiPosition.y;

            GUI.Box(new Rect(x, y, width, 118f), "PauseOwnershipSelfTest (QA)");

            y += 24f;
            if (GUI.Button(new Rect(x + 8f, y, width - 16f, height), $"AcquireExternal ({pauseToken})"))
                AcquireExternal();

            y += 26f;
            if (GUI.Button(new Rect(x + 8f, y, width - 16f, height), $"ReleaseExternal ({pauseToken})"))
                ReleaseExternal();

            y += 26f;
            if (GUI.Button(new Rect(x + 8f, y, width - 16f, height), "PrintStatus"))
                PrintStatus("OnGUI/PrintStatus");

            y += 26f;
            var hint =
                $"Hotkeys: Acquire={acquireKey}, Release={releaseKey}, Status={printStatusKey} | acquired={_acquired}";
            GUI.Label(new Rect(x + 8f, y, width - 16f, height), hint);
        }

        private void Log(string msg)
        {
            if (!verbose)
                return;

            Debug.Log($"[PauseOwnershipSelfTest] {msg}");
        }
    }
}
