using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Gate;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    [DisallowMultipleComponent]
    public sealed class GateDebugHud : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private bool showHud = true;
        [SerializeField] private int fontSize = 16;

        private ISimulationGateService _gate;

        private void Update()
        {
            if (_gate == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _gate);
            }
        }

        private void OnGUI()
        {
            if (!showHud)
            {
                return;
            }

            if (_gate == null)
            {
                GUI.Label(new Rect(10, 10, 800, 40), "GateDebugHud: ISimulationGateService não resolvido.");
                return;
            }

            var style = new GUIStyle(GUI.skin.label) { fontSize = fontSize };

            bool pause = _gate.IsTokenActive(SimulationGateTokens.Pause);
            bool transition = _gate.IsTokenActive(SimulationGateTokens.SceneTransition);
            bool softReset = _gate.IsTokenActive(SimulationGateTokens.SoftReset);
            bool loading = _gate.IsTokenActive(SimulationGateTokens.Loading);
            bool cinematic = _gate.IsTokenActive(SimulationGateTokens.Cinematic);

            string text =
                $"[Gate] IsOpen={_gate.IsOpen} | ActiveTokenCount={_gate.ActiveTokenCount}\n" +
                $"Tokens: Pause={pause}, SceneTransition={transition}, SoftReset={softReset}, Loading={loading}, Cinematic={cinematic}";

            GUI.Label(new Rect(10, 10, 900, 80), text, style);
        }
    }
}
