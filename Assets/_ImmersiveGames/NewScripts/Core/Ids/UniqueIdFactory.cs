using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Core.Ids
{
    public interface IUniqueIdFactory
    {
        string GenerateId(GameObject owner, string prefix = null);
        int GetInstanceCount(string actorName);
    }

    /// <summary>
    /// NewScripts generic unique ID generator.
    /// Goals:
    /// - No gameplay semantics (no Player/NPC/Obj).
    /// - Strong uniqueness per runtime instance.
    /// - Optional suffix via prefix parameter (for sub-ids), without changing identity semantics.
    ///
    /// Note: "Readable names" should be handled by IActor / UI layers, not by this factory.
    /// </summary>
    public sealed class UniqueIdFactory : IUniqueIdFactory
    {
        private readonly Dictionary<string, int> _instanceCounts = new(StringComparer.Ordinal);
        private long _globalCounter;

        // Short session salt to reduce collisions across play sessions in the editor (domain reload off, etc.)
        private readonly string _sessionSalt = CreateSessionSalt();

        public string GenerateId(GameObject owner, string prefix = null)
        {
            // owner is only used for optional per-name counts / debugging readability.
            string baseName = owner != null ? owner.name : "NullOwner";

            int count = _instanceCounts.GetValueOrDefault(baseName, 0);

            count++;
            _instanceCounts[baseName] = count;

            long seq = Interlocked.Increment(ref _globalCounter);

            // Strong unique core (session + monotonic counter).
            // The baseName is only for debugging readability.
            string idCore = $"A_{_sessionSalt}_{seq}";

            string hint = SanitizeToken(baseName);
            if (!string.IsNullOrEmpty(hint))
            {
                idCore = $"{idCore}_{hint}";
            }

            if (!string.IsNullOrWhiteSpace(prefix))
            {
                string suffix = SanitizeToken(prefix);
                if (!string.IsNullOrEmpty(suffix))
                {
                    return $"{idCore}_{suffix}";
                }
            }

            return idCore;
        }

        public int GetInstanceCount(string actorName)
        {
            if (string.IsNullOrEmpty(actorName))
            {
                return 0;
            }

            return _instanceCounts.GetValueOrDefault(actorName, 0);
        }

        private static string CreateSessionSalt()
        {
            // 8 chars from a Guid is enough for a session salt (avoid range operator for compatibility).
            string guid = Guid.NewGuid().ToString("N");
            return guid.Length >= 8 ? guid[..8] : guid;
        }

        private static string SanitizeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            // Keep it stable, short, and file/log friendly.
            // Replace whitespace with '-', remove problematic chars, and cap length.
            int cap = Math.Min(value.Length, 24);
            Span<char> buffer = stackalloc char[cap];

            int written = 0;
            for (int i = 0; i < value.Length && written < buffer.Length; i++)
            {
                char c = value[i];

                if (char.IsWhiteSpace(c))
                {
                    buffer[written++] = '-';
                    continue;
                }

                if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
                {
                    buffer[written++] = c;
                }
            }

            return written == 0 ? string.Empty : new string(buffer[..written]);
        }
    }
}
