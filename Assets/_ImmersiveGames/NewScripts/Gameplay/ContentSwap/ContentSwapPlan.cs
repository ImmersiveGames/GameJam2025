#nullable enable
using System;

namespace _ImmersiveGames.NewScripts.Gameplay.ContentSwap
{
    /// <summary>
    /// Descrição mínima e rastreável do que significa "qual fase" e "qual conteúdo".
    /// Não é um builder; é apenas um contrato simples para orientar montagem de cenário.
    /// </summary>
    public readonly struct ContentSwapPlan : IEquatable<ContentSwapPlan>
    {
        public readonly string ContentId;
        public readonly string ContentSignature;

        public ContentSwapPlan(string contentId, string contentSignature)
        {
            ContentId = contentId ?? string.Empty;
            ContentSignature = contentSignature ?? string.Empty;
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(ContentId);

        public override string ToString()
        {
            if (!IsValid)
            {
                return "<none>";
            }
            if (string.IsNullOrWhiteSpace(ContentSignature))
            {
                return ContentId;
            }
            return $"{ContentId} | {ContentSignature}";
        }

        public bool Equals(ContentSwapPlan other)
            => string.Equals(ContentId, other.ContentId, StringComparison.Ordinal) &&
               string.Equals(ContentSignature, other.ContentSignature, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is ContentSwapPlan other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(ContentId, ContentSignature);

        public static bool operator ==(ContentSwapPlan left, ContentSwapPlan right) => left.Equals(right);
        public static bool operator !=(ContentSwapPlan left, ContentSwapPlan right) => !left.Equals(right);

        public static ContentSwapPlan None => new(string.Empty, string.Empty);
    }
}
