using System;
using System.Threading;

namespace _ImmersiveGames.NewScripts.Infrastructure.Util
{
    /// <summary>
    /// Utilitário centralizado para normalização de strings e geração de signatures.
    /// Unifica lógica de normalização para:
    /// - Identificadores (profiles, scene names) → case-insensitive (trim + lower)
    /// - Signatures (resets diretos, correlação) → com sanitização adicional
    ///
    /// Contrato:
    /// - SceneFlow usa SceneTransitionContext.ContextSignature (canônico).
    /// - Direct resets (sem transição) usam assinatura sintética com session salt.
    /// - Profiles e identifiers usam normalização case-insensitive.
    /// </summary>
    public static class SignatureUtil
    {
        /// <summary>
        /// Normaliza um identificador/profile para uso case-insensitive.
        /// Remove whitespace e converte para minúsculas.
        /// Usado para: profiles, scene names, identifiers, etc.
        /// </summary>
        public static string NormalizeIdentifier(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }

        /// <summary>
        /// Normaliza um token para uso em assinaturas (remove whitespace, caracteres inválidos, limita tamanho).
        /// Diferente de NormalizeIdentifier por incluir sanitização adicional.
        /// Aplicável a cenas, sources, profiles em contextos de signature.
        /// </summary>
        public static string NormalizeTokenForSignature(string value, string fallback = "<null>")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            string trimmed = value.Trim();

            // Mantém a string amigável para log/grep: remove whitespace e caracteres problemáticos.
            // Não é um sanitizador genérico — é apenas um normalizador defensivo.
            const int cap = 64;
            int limit = Math.Min(trimmed.Length, cap);

            Span<char> buffer = stackalloc char[limit];
            int written = 0;

            for (int i = 0; i < trimmed.Length && written < buffer.Length; i++)
            {
                char c = trimmed[i];

                if (char.IsWhiteSpace(c))
                {
                    buffer[written++] = '-';
                    continue;
                }

                if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == ':' || c == '/' || c == '.')
                {
                    buffer[written++] = c;
                }
            }

            return written == 0 ? fallback : new string(buffer[..written]);
        }

        /// <summary>
        /// Cria uma assinatura sintética para resets diretos (fora do SceneFlow).
        /// Formato: directReset:scene=&lt;Scene&gt;;src=&lt;Source&gt;;seq=&lt;N&gt;;salt=&lt;S&gt;
        /// </summary>
        public static string ComputeDirectResetSignature(string activeSceneName, string source)
        {
            string scene = NormalizeTokenForSignature(activeSceneName, fallback: "<unknown>");
            string src = NormalizeTokenForSignature(source, fallback: "<unspecified>");

            long n = Interlocked.Increment(ref _directResetSequence);

            return $"directReset:scene={scene};src={src};seq={n};salt={DirectResetSessionSalt}";
        }

        private static readonly string DirectResetSessionSalt = CreateSessionSalt();
        private static long _directResetSequence;

        private static string CreateSessionSalt()
        {
            // 8 chars from a GUID is enough (evita colisões entre play sessions, no Editor).
            string guid = Guid.NewGuid().ToString("N");
            return guid.Length >= 8 ? guid[..8] : guid;
        }
    }
}
