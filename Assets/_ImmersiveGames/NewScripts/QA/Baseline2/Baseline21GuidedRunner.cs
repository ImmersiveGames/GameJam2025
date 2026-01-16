#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using _ImmersiveGames.NewScripts.QA.Baseline2.Verifier;

namespace _ImmersiveGames.NewScripts.EditorTools.Baseline2
{
    internal static class Baseline21GuidedRunner
    {
        private const string MenuPath = "Tools/NewScripts/QA/Baseline/Run Baseline 2.1 (Guided)";

        [MenuItem(MenuPath)]
        private static void RunGuided()
        {
            if (!EditorApplication.isPlaying)
            {
                Baseline21SmokeLastRunTool.ArmCapture();
                Debug.Log("[Baseline21Guided] Smoke 2.1 armado. Pressione Play para iniciar a captura.");
                return;
            }

            var state = _ImmersiveGames.NewScripts.QA.Baseline2.Baseline21SmokeLastRunShared.LoadState();
            if (_ImmersiveGames.NewScripts.QA.Baseline2.Baseline21SmokeLastRunRuntime.IsCapturing || state.Capturing)
            {
                Baseline21SmokeLastRunTool.StopCaptureAndGenerateReport("GuidedStop");
                Baseline21ContractDrivenVerifier.VerifyLastRunAndWriteReport();
                Debug.Log("[Baseline21Guided] Smoke 2.1 finalizado e verificação contract-driven gerada.");
                return;
            }

            if (state.Armed)
            {
                _ImmersiveGames.NewScripts.QA.Baseline2.Baseline21SmokeLastRunRuntime.TryStartCaptureFromEditor();
                Debug.Log("[Baseline21Guided] Smoke 2.1 iniciado.");
                return;
            }

            Baseline21SmokeLastRunTool.ArmCapture();
            _ImmersiveGames.NewScripts.QA.Baseline2.Baseline21SmokeLastRunRuntime.TryStartCaptureFromEditor();
            Debug.Log("[Baseline21Guided] Smoke 2.1 armado e iniciado.");
        }
    }
}
#endif
