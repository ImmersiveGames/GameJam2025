#nullable enable
using System;
namespace _ImmersiveGames.NewScripts.Modules.ContentSwap
{
    /// <summary>
    /// Descrição mínima e rastreável do que significa "qual fase" e "qual conteúdo".
    /// Não é um builder; é apenas um contrato simples para orientar montagem de cenário.
    /// </summary>
    public readonly struct ContentSwapPlan : IEquatable<ContentSwapPlan>
    {
        public readonly string contentId;
        public readonly string contentSignature;

        public ContentSwapPlan(string? contentId, string? contentSignature)
        {
            this.contentId = contentId ?? string.Empty;
            this.contentSignature = contentSignature ?? string.Empty;
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(contentId);

        public override string ToString()
        {
            if (!IsValid)
            {
                return "<none>";
            }
            if (string.IsNullOrWhiteSpace(contentSignature))
            {
                return contentId;
            }
            return $"{contentId} | {contentSignature}";
        }

        public bool Equals(ContentSwapPlan other)
            => string.Equals(contentId, other.contentId, StringComparison.Ordinal) &&
               string.Equals(contentSignature, other.contentSignature, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is ContentSwapPlan other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(contentId, contentSignature);

        public static bool operator ==(ContentSwapPlan left, ContentSwapPlan right) => left.Equals(right);
        public static bool operator !=(ContentSwapPlan left, ContentSwapPlan right) => !left.Equals(right);

        public static ContentSwapPlan None => new(string.Empty, string.Empty);
    }
}
