using System;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.PhaseRuntime
{
    internal static class PhaseFlowSignalVocabulary
    {
        public const string GameplaySessionFlowSource = "GameplaySessionFlow";
        public const string PhaseDefinitionNavigationSource = "PhaseDefinitionNavigation";

        public const string NoContentReason = "no_content";
        public const string ContinueButtonReason = "IntroStage/ContinueButton";
        public const string SupersededReason = "superseded";

        public static bool IsGameplaySessionFlowSource(string? source)
            => string.Equals(NormalizeToken(source), GameplaySessionFlowSource, StringComparison.Ordinal);

        public static string CanonicalizeCompletionSource(string? source)
        {
            string normalized = NormalizeToken(source);
            if (string.IsNullOrEmpty(normalized))
            {
                return GameplaySessionFlowSource;
            }

            if (string.Equals(normalized, GameplaySessionFlowSource, StringComparison.OrdinalIgnoreCase))
            {
                return GameplaySessionFlowSource;
            }

            if (string.Equals(normalized, PhaseDefinitionNavigationSource, StringComparison.OrdinalIgnoreCase))
            {
                return GameplaySessionFlowSource;
            }

            return normalized;
        }

        public static string CanonicalizeCompletionReason(string? reason, bool wasSkipped)
        {
            string normalized = NormalizeToken(reason);
            if (string.IsNullOrEmpty(normalized))
            {
                return wasSkipped ? NoContentReason : "<none>";
            }

            if (string.Equals(normalized, NoContentReason, StringComparison.OrdinalIgnoreCase))
            {
                return NoContentReason;
            }


            if (string.Equals(normalized, ContinueButtonReason, StringComparison.OrdinalIgnoreCase))
            {
                return ContinueButtonReason;
            }

            if (string.Equals(normalized, SupersededReason, StringComparison.OrdinalIgnoreCase))
            {
                return SupersededReason;
            }

            return normalized;
        }

        private static string NormalizeToken(string? value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
