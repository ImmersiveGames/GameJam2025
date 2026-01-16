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
                Baseline21SmokeLastRunTool.ArmCapture(logInstructions: false);
                Debug.Log("Pressione Play para iniciar a captura.");
                return;
            }

            var state = _ImmersiveGames.NewScripts.QA.Baseline2.Baseline21SmokeLastRunShared.LoadState();
            if (_ImmersiveGames.NewScripts.QA.Baseline2.Baseline21SmokeLastRunRuntime.IsCapturing || state.Capturing)
            {
                Baseline21SmokeLastRunTool.StopCaptureAndGenerateReport("GuidedStop");
                Debug.Log("Smoke 2.1 STOPPED");

                var verification = Baseline21ContractDrivenVerifier.VerifyLastRunAndWriteReport();
                Debug.Log($"Verification written: {Baseline21ContractDrivenVerifier.OutputMdAbs}");
                if (verification.Status == Baseline21ContractDrivenVerifier.VerificationStatus.Fail)
                    Debug.LogWarning("Veja Diagnostics no relatório 2.1.");
                return;
            }

            if (state.Armed)
            {
                _ImmersiveGames.NewScripts.QA.Baseline2.Baseline21SmokeLastRunRuntime.TryStartCaptureFromEditor();
                Debug.Log("Smoke 2.1 STARTED");
                return;
            }

            Baseline21SmokeLastRunTool.ArmCapture(logInstructions: false);
            _ImmersiveGames.NewScripts.QA.Baseline2.Baseline21SmokeLastRunRuntime.TryStartCaptureFromEditor();
            Debug.Log("Smoke 2.1 STARTED");
        }
    }
}
#endif
