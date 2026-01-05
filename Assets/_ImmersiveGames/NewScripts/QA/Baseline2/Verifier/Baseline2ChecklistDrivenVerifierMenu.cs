#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Baseline2.Verifier
{
    internal static class Baseline2ChecklistDrivenVerifierMenu
    {
        private const string MenuPath = "Tools/NewScripts/Baseline2/Verify Last Run (Checklist-driven)";

        [MenuItem(MenuPath)]
        private static void RunVerification()
        {
            var result = Baseline2ChecklistDrivenVerifier.VerifyLastRun();

            var sb = new StringBuilder(8 * 1024);
            sb.AppendLine("[Baseline2ChecklistDrivenVerifier] Resultado");
            sb.AppendLine($"- {result.Summary}");

            if (result.FailMarkerFound)
                sb.AppendLine($"- FAIL marker: {result.FailMarkerLine}");

            foreach (var block in result.Blocks)
            {
                sb.AppendLine($"\n## {block.Name} -> {block.Status}");
                foreach (var evidence in block.Evidence)
                {
                    var status = evidence.Found ? "FOUND" : "NOT FOUND";
                    var line = evidence.Found ? $"line={evidence.LineNumber}" : "line=--";
                    sb.AppendLine($"- {status} | {line} | `{evidence.RawText}`");
                    if (evidence.Found && !string.IsNullOrEmpty(evidence.Snippet))
                        sb.AppendLine($"  snippet: {evidence.Snippet}");
                }
            }

            Debug.Log(sb.ToString());
        }
    }
}
#endif
