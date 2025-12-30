using System;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Gate;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    /// <summary>
    /// Self-test de ref-count do ISimulationGateService.
    /// Valida que:
    /// - 2 Acquire do mesmo token não aumentam o número de tokens ativos (apenas o refcount interno).
    /// - Após Dispose #1, token segue ativo.
    /// - Após Dispose #2, o estado volta exatamente ao baseline (antes do teste).
    ///
    /// Importante: este teste NÃO assume que o gate ficará aberto no final,
    /// pois durante transições pode existir outro token ativo (ex.: flow.scene_transition).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GateRefCountSelfTest : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private bool runOnStart = true;

        [Tooltip("Token usado no teste. Mantenha estável para facilitar leitura de logs.")]
        [SerializeField] private string token = "qa.test.refcount";

        [Tooltip("Se true, escreve logs mais detalhados.")]
        [SerializeField] private bool verbose = true;

        [Inject] private ISimulationGateService _gate;

        private void Awake()
        {
            // Best-effort: se esta cena não injeta automaticamente todos os MBs, tenta se auto-injetar.
            TryResolveGate();
        }

        private void Start()
        {
            if (!runOnStart)
                return;

            if (!TryResolveGate())
            {
                Debug.LogError($"[GateRefCountSelfTest] ISimulationGateService não resolvido. " +
                               $"scene='{SceneManager.GetActiveScene().name}'. " +
                               $"Verifique se este MonoBehaviour está no escopo de injeção (ou se o tipo do campo é o correto).");
                return;
            }

            Run();
        }

        private bool TryResolveGate()
        {
            if (_gate != null)
                return true;

            var provider = DependencyManager.Provider;
            if (provider == null)
                return false;

            // 1) tenta auto-injeção (caso a cena não injete automaticamente esse MB)
            try
            {
                provider.InjectDependencies(this);
            }
            catch
            {
                // Ignora: alguns contextos podem não permitir, mas ainda tentamos TryGet.
            }

            if (_gate != null)
                return true;

            // 2) fallback por lookup global (mesmo que o [Inject] não tenha rodado)
            if (provider.TryGetGlobal<ISimulationGateService>(out var gate))
            {
                _gate = gate;
                return true;
            }

            // 3) fallback por TryGet (global/scene/object conforme implementação)
            if (provider.TryGet<ISimulationGateService>(out gate))
            {
                _gate = gate;
                return true;
            }

            return false;
        }

        private void Run()
        {
            // Baseline (pode estar fechado por outros tokens durante transição).
            var baselineActive = _gate.ActiveTokenCount;
            var baselineOpen = _gate.IsOpen;
            var baselineTokenActive = _gate.IsTokenActive(token);

            Log($"BASELINE | IsOpen={baselineOpen} Active={baselineActive} TokenActive({token})={baselineTokenActive}");

            IDisposable h1 = null;
            IDisposable h2 = null;

            try
            {
                // Acquire #1
                h1 = _gate.Acquire(token);
                var after1Active = _gate.ActiveTokenCount;
                var after1Open = _gate.IsOpen;
                var after1TokenActive = _gate.IsTokenActive(token);

                var expectedAfter1 = baselineActive + (baselineTokenActive ? 0 : 1);

                Log($"Acquire #1 | IsOpen={after1Open} Active={after1Active} TokenActive={after1TokenActive} " +
                    $"(expected Active={expectedAfter1})");

                if (!after1TokenActive)
                    Fail("Após Acquire #1, token deveria estar ativo.");

                if (after1Active != expectedAfter1)
                    Fail($"Após Acquire #1, ActiveTokenCount inesperado. Esperado={expectedAfter1} Atual={after1Active}");

                // Acquire #2 (mesmo token)
                h2 = _gate.Acquire(token);
                var after2Active = _gate.ActiveTokenCount;
                var after2Open = _gate.IsOpen;
                var after2TokenActive = _gate.IsTokenActive(token);

                var expectedAfter2 = expectedAfter1;

                Log($"Acquire #2 | IsOpen={after2Open} Active={after2Active} TokenActive={after2TokenActive} " +
                    $"(expected Active={expectedAfter2})");

                if (!after2TokenActive)
                    Fail("Após Acquire #2, token deveria permanecer ativo.");

                if (after2Active != expectedAfter2)
                    Fail($"Após Acquire #2, ActiveTokenCount não deveria aumentar. Esperado={expectedAfter2} Atual={after2Active}");

                // Dispose #1
                h1.Dispose();
                h1 = null;

                var afterD1Active = _gate.ActiveTokenCount;
                var afterD1Open = _gate.IsOpen;
                var afterD1TokenActive = _gate.IsTokenActive(token);

                Log($"Dispose #1 | IsOpen={afterD1Open} Active={afterD1Active} TokenActive={afterD1TokenActive}");

                if (!afterD1TokenActive)
                    Fail("Após Dispose #1, token deveria continuar ativo (refcount > 0).");

                if (afterD1Active != expectedAfter2)
                    Fail($"Após Dispose #1, ActiveTokenCount não deveria mudar. Esperado={expectedAfter2} Atual={afterD1Active}");

                // Dispose #2
                h2.Dispose();
                h2 = null;

                var afterD2Active = _gate.ActiveTokenCount;
                var afterD2Open = _gate.IsOpen;
                var afterD2TokenActive = _gate.IsTokenActive(token);

                Log($"Dispose #2 | IsOpen={afterD2Open} Active={afterD2Active} TokenActive={afterD2TokenActive} " +
                    $"(expected back to baseline IsOpen={baselineOpen} Active={baselineActive} TokenActive={baselineTokenActive})");

                if (afterD2TokenActive != baselineTokenActive)
                    Fail($"Após Dispose #2, TokenActive deveria voltar ao baseline ({baselineTokenActive}). Atual={afterD2TokenActive}");

                if (afterD2Active != baselineActive)
                    Fail($"Após Dispose #2, ActiveTokenCount deveria voltar ao baseline ({baselineActive}). Atual={afterD2Active}");

                if (afterD2Open != baselineOpen)
                    Fail($"Após Dispose #2, IsOpen deveria voltar ao baseline ({baselineOpen}). Atual={afterD2Open}");

                Pass("OK: refcount do token e ActiveTokenCount retornaram ao baseline.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GateRefCountSelfTest] EXCEPTION: {ex}");
            }
            finally
            {
                try { h1?.Dispose(); } catch { /* ignore */ }
                try { h2?.Dispose(); } catch { /* ignore */ }
            }
        }

        private void Log(string msg)
        {
            if (!verbose)
                return;

            Debug.Log($"[GateRefCountSelfTest] {msg}");
        }

        private void Pass(string msg) => Debug.Log($"[GateRefCountSelfTest] PASS: {msg}");
        private void Fail(string msg) => Debug.LogError($"[GateRefCountSelfTest] FAIL: {msg}");
    }
}
