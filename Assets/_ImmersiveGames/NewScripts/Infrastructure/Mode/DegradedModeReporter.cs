using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Infrastructure.Mode
{
    /// <summary>
    /// Reporter canônico de DEGRADED_MODE.
    /// Deduplica por frame para evitar spam em loops/acidentais.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class DegradedModeReporter : IDegradedModeReporter
    {
        private readonly HashSet<(string key, int frame)> _frameDedupe = new();

        public void Report(string feature, string reason, string detail = null, string signature = null, string profile = null)
        {
            feature = Sanitize(feature);
            reason = Sanitize(reason);
            detail = Sanitize(detail);
            signature = Sanitize(signature);
            profile = Sanitize(profile);

            string msg =
                $"DEGRADED_MODE feature='{feature}' reason='{reason}'" +
                (string.IsNullOrWhiteSpace(detail) ? string.Empty : $" detail='{detail}'") +
                (string.IsNullOrWhiteSpace(signature) ? string.Empty : $" signature='{signature}'") +
                (string.IsNullOrWhiteSpace(profile) ? string.Empty : $" profile='{profile}'");

            // Deduplica por frame.
            int frame = Time.frameCount;
            var key = (msg, frame);
            if (!_frameDedupe.Add(key))
            {
                return;
            }

            DebugUtility.LogWarning<DegradedModeReporter>(msg);
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            // Comentário: evita quebra de formato (aspas simples no payload).
            return value.Replace("'", "’").Trim();
        }
    }
}
