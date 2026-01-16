#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Baseline2.Verifier
{
    internal static class Baseline21ContractDrivenVerifierMenu
    {
        private const string MenuPathVerifyAndWrite = "Tools/NewScripts/QA/Baseline/Advanced/Verify 2.1 (Contract-driven) + Write MD";

        [MenuItem(MenuPathVerifyAndWrite)]
        private static void RunVerificationAndWriteReport()
        {
            var result = Baseline21ContractDrivenVerifier.VerifyLastRunAndWriteReport();
            Debug.Log(BuildConsoleOutput(result, wroteReport: true));
        }

        private static string BuildConsoleOutput(Baseline21ContractDrivenVerifier.VerificationResult result, bool wroteReport)
        {
            var sb = new System.Text.StringBuilder(8 * 1024);

            sb.AppendLine("[Baseline21ContractDrivenVerifier] Resultado");
            sb.AppendLine($"- {result.Summary}");

            if (wroteReport)
                sb.AppendLine($"- Report written: {Baseline21ContractDrivenVerifier.OutputMdAbs}");

            foreach (var domain in result.Domains)
            {
                sb.AppendLine();
                sb.AppendLine($"## {domain.Name} -> {domain.Status} (found={domain.EvidenceFound.Count}, missing={domain.EvidenceMissing.Count})");

                if (domain.Status != Baseline21ContractDrivenVerifier.VerificationStatus.Fail)
                    continue;

                foreach (var missing in domain.EvidenceMissing)
                    sb.AppendLine($"- MISSING | `{missing}`");
            }

            return sb.ToString();
        }
    }
}
#endif
