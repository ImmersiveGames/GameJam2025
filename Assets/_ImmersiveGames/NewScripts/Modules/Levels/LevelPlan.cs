#nullable enable
using System;
using _ImmersiveGames.NewScripts.Modules.ContentSwap;
namespace _ImmersiveGames.NewScripts.Modules.Levels
{
    /// <summary>
    /// Plano de nível: progressão do jogo (Level) + conteúdo a ser trocado via ContentSwap.
    /// </summary>
    public readonly struct LevelPlan : IEquatable<LevelPlan>
    {
        public string LevelId { get; }
        public string ContentId { get; }
        public string ContentSignature { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(LevelId) && !string.IsNullOrWhiteSpace(ContentId);

        public LevelPlan(string levelId, string contentId, string contentSignature)
        {
            LevelId = string.IsNullOrWhiteSpace(levelId) ? string.Empty : levelId.Trim();
            ContentId = string.IsNullOrWhiteSpace(contentId) ? string.Empty : contentId.Trim();
            ContentSignature = string.IsNullOrWhiteSpace(contentSignature) ? string.Empty : contentSignature.Trim();
        }

        public ContentSwapPlan ToContentSwapPlan()
        {
            return new ContentSwapPlan(ContentId, ContentSignature);
        }

        public bool Equals(LevelPlan other)
        {
            return string.Equals(LevelId, other.LevelId, StringComparison.Ordinal)
                && string.Equals(ContentId, other.ContentId, StringComparison.Ordinal)
                && string.Equals(ContentSignature, other.ContentSignature, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
            => obj is LevelPlan other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(LevelId, ContentId, ContentSignature);

        public static bool operator ==(LevelPlan left, LevelPlan right) => left.Equals(right);
        public static bool operator !=(LevelPlan left, LevelPlan right) => !left.Equals(right);

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(ContentSignature))
            {
                return $"{LevelId}:{ContentId}";
            }

            return $"{LevelId}:{ContentId} | {ContentSignature}";
        }

        public static LevelPlan None => new(string.Empty, string.Empty, string.Empty);
    }
}
