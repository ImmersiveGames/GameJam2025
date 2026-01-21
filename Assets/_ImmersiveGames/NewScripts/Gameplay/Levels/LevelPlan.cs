#nullable enable
using System;
using _ImmersiveGames.NewScripts.Gameplay.Phases;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels
{
    /// <summary>
    /// Plano de nível: progressão do jogo (Level) + conteúdo a ser trocado via ContentSwap (Phase).
    /// </summary>
    public readonly struct LevelPlan : IEquatable<LevelPlan>
    {
        public string LevelId { get; }
        public string PhaseId { get; }
        public string ContentSignature { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(LevelId) && !string.IsNullOrWhiteSpace(PhaseId);

        public LevelPlan(string levelId, string phaseId, string contentSignature)
        {
            LevelId = string.IsNullOrWhiteSpace(levelId) ? string.Empty : levelId.Trim();
            PhaseId = string.IsNullOrWhiteSpace(phaseId) ? string.Empty : phaseId.Trim();
            ContentSignature = string.IsNullOrWhiteSpace(contentSignature) ? string.Empty : contentSignature.Trim();
        }

        public PhasePlan ToPhasePlan()
        {
            return new PhasePlan(PhaseId, ContentSignature);
        }

        public bool Equals(LevelPlan other)
        {
            return string.Equals(LevelId, other.LevelId, StringComparison.Ordinal)
                && string.Equals(PhaseId, other.PhaseId, StringComparison.Ordinal)
                && string.Equals(ContentSignature, other.ContentSignature, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
            => obj is LevelPlan other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(LevelId, PhaseId, ContentSignature);

        public static bool operator ==(LevelPlan left, LevelPlan right) => left.Equals(right);
        public static bool operator !=(LevelPlan left, LevelPlan right) => !left.Equals(right);

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(ContentSignature))
            {
                return $"{LevelId}:{PhaseId}";
            }

            return $"{LevelId}:{PhaseId} | {ContentSignature}";
        }

        public static LevelPlan None => new(string.Empty, string.Empty, string.Empty);
    }
}
