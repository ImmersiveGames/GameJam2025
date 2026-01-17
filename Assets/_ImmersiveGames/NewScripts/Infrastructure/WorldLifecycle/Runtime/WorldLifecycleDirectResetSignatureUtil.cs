using System;
using System.Threading;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime
{
    /// <summary>
    /// Utilitário para gerar uma assinatura sintética de correlação para resets diretos (fora do SceneFlow).
    ///
    /// Motivação:
    /// - Não existe SceneTransitionContext em RequestResetAsync(source).
    /// - Ainda assim, precisamos de uma signature estável o suficiente para correlacionar logs e eventos.
    ///
    /// Regra:
    /// - A signature aqui NÃO participa do completion gate do SceneFlow (não há transição ativa),
    ///   mas é canônica para auditorias e tooling.
    /// </summary>
    public static class WorldLifecycleDirectResetSignatureUtil
    {
        private static readonly string SessionSalt = CreateSessionSalt();
        private static long _seq;

        /// <summary>
        /// Computa uma assinatura machine-readable para um reset direto.
        /// Formato: directReset:scene=&lt;Scene&gt;;src=&lt;Source&gt;;seq=&lt;N&gt;;salt=&lt;S&gt;
        /// </summary>
        public static string Compute(string activeSceneName, string source)
        {
            string scene = NormalizeToken(activeSceneName, fallback: "<unknown>");
            string src = NormalizeToken(source, fallback: "<unspecified>");

            long n = Interlocked.Increment(ref _seq);

            return $"directReset:scene={scene};src={src};seq={n};salt={SessionSalt}";
        }

        private static string CreateSessionSalt()
        {
            // 8 chars from a GUID is enough (evita colisões entre play sessions no Editor).
            string guid = Guid.NewGuid().ToString("N");
            return guid.Length >= 8 ? guid.Substring(0, 8) : guid;
        }

        private static string NormalizeToken(string value, string fallback)
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

            return written == 0 ? fallback : new string(buffer.Slice(0, written));
        }
    }
}
