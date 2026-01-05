#if UNITY_EDITOR
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Baseline2.Verifier
{
    internal static class Baseline2ChecklistDrivenVerifierMenu
    {
        private const string MenuPathVerify = "Tools/NewScripts/Baseline2/Verify Last Run (Checklist-driven)";
        private const string MenuPathVerifyAndWrite = "Tools/NewScripts/Baseline2/Verify Last Run (Checklist-driven) + Write MD Report";

        [MenuItem(MenuPathVerify)]
        private static void RunVerification()
        {
            var result = Baseline2ChecklistDrivenVerifier.VerifyLastRun();
            Debug.Log(BuildConsoleOutput(result, wroteReport: false));
        }

        [MenuItem(MenuPathVerifyAndWrite)]
        private static void RunVerificationAndWriteReport()
        {
            var result = Baseline2ChecklistDrivenVerifier.VerifyLastRunAndWriteReport(includeFoundEvidenceInMd: false);
            Debug.Log(BuildConsoleOutput(result, wroteReport: true));
        }

        private static string BuildConsoleOutput(Baseline2ChecklistDrivenVerifier.VerificationResult result, bool wroteReport)
        {
            var sb = new StringBuilder(8 * 1024);

            sb.AppendLine("[Baseline2ChecklistDrivenVerifier] Resultado");
            sb.AppendLine($"- {result.Summary}");

            if (wroteReport)
                sb.AppendLine($"- Report written: {Baseline2ChecklistDrivenVerifier.OutputMdAbs}");

            if (result.FailMarkerFound)
                sb.AppendLine($"- FAIL marker line {result.FailMarkerLineNumber}: {result.FailMarkerLine}");

            // Saída compacta:
            // - Para PASS: imprime só status agregado do bloco.
            // - Para FAIL: lista apenas faltantes.
            foreach (var block in result.Blocks)
            {
                sb.AppendLine();
                sb.AppendLine($"## {block.Name} -> {block.Status} ({block.FoundCount}/{block.EvidenceCount})");

                if (block.Status != Baseline2ChecklistDrivenVerifier.VerificationStatus.Fail)
                    continue;

                foreach (var ev in block.Evidence.Where(e => !e.Found))
                {
                    sb.AppendLine($"- NOT FOUND | `{ev.RawText}`");
                }
            }

            return sb.ToString();
        }
    }
}
#endif
