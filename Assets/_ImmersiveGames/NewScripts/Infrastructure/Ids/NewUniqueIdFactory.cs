using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils;

namespace _ImmersiveGames.NewScripts.Infrastructure.Ids
{
    /// <summary>
    /// NewScripts generic unique ID generator.
    /// Goals:
    /// - No gameplay semantics (no Player/NPC/Obj).
    /// - Strong uniqueness per runtime instance.
    /// - Optional suffix via prefix parameter (for sub-ids), without changing identity semantics.
    ///
    /// Note: "Readable names" should be handled by IActor / UI layers, not by this factory.
    /// </summary>
    public sealed class NewUniqueIdFactory : IUniqueIdFactory
    {
        private readonly Dictionary<string, int> _instanceCounts = new(StringComparer.Ordinal);
        private long _globalCounter;

        // Short session salt to reduce collisions across play sessions in the editor (domain reload off, etc.)
        private readonly string _sessionSalt = CreateSessionSalt();

        public string GenerateId(GameObject owner, string prefix = null)
        {
            // We do not use owner components (no GetComponent / gameplay coupling).
            // owner is only used for optional per-name counts.
            string baseName = owner != null ? owner.name : "NullOwner";

            int count = _instanceCounts.GetValueOrDefault(baseName, 0);

            count++;
            _instanceCounts[baseName] = count;

            long seq = Interlocked.Increment(ref _globalCounter);

            // Strong unique core (session + monotonic counter).
            // The baseName and per-name count are only for debugging readability.
            string idCore = $"A_{_sessionSalt}_{seq}";

            // Optional human hint (safe + bounded). Does not affect uniqueness.
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
            // 8 chars from a Guid is enough for a session salt.
            return Guid.NewGuid().ToString("N")[..8];
        }

        private static string SanitizeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            // Keep it stable, short, and file/log friendly.
            // Replace whitespace with '-', remove problematic chars, and cap length.
            Span<char> buffer = stackalloc char[Math.Min(value.Length, 24)];
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
                // ignore everything else
            }

            return written == 0 ? string.Empty : new string(buffer[..written]);
        }
    }
}
