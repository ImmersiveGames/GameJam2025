namespace ImmersiveGames.GameJam2025.Orchestration.GameLoop.RunLifecycle.Core
{
    /// <summary>
    /// Centraliza a lógica de formatação e normalização de "reason" na GameLoop.
    /// </summary>
    public static class GameLoopReasonFormatter
    {
        /// <summary>
        /// Formata uma reason para log/display sem aplicar fallback semântico.
        /// </summary>
        public static string Format(string reason)
            => string.IsNullOrWhiteSpace(reason) ? "<null>" : reason.Trim();

        /// <summary>
        /// Normaliza uma reason obrigatória.
        /// </summary>
        public static string NormalizeRequired(string reason)
            => string.IsNullOrWhiteSpace(reason) ? "Unspecified" : reason.Trim();

        /// <summary>
        /// Normaliza uma reason opcional com fallback.
        /// </summary>
        public static string NormalizeOptional(string reason, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(reason))
            {
                return reason.Trim();
            }

            return string.IsNullOrWhiteSpace(fallback) ? "Unspecified" : fallback.Trim();
        }
    }
}

