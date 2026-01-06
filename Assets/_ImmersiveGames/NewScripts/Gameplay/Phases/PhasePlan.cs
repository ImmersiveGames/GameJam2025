#nullable enable
using System;

namespace _ImmersiveGames.NewScripts.Gameplay.Phases
{
    /// <summary>
    /// Descrição mínima e rastreável do que significa "qual fase" e "qual conteúdo".
    /// Não é um builder; é apenas um contrato simples para orientar montagem de cenário.
    /// </summary>
    public readonly struct PhasePlan : IEquatable<PhasePlan>
    {
        public readonly string PhaseId;
        public readonly string ContentSignature;

        public PhasePlan(string phaseId, string contentSignature)
        {
            PhaseId = phaseId ?? string.Empty;
            ContentSignature = contentSignature ?? string.Empty;
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(PhaseId);

        public override string ToString()
        {
            if (!IsValid) return "<none>";
            if (string.IsNullOrWhiteSpace(ContentSignature)) return PhaseId;
            return $"{PhaseId} | {ContentSignature}";
        }

        public bool Equals(PhasePlan other)
            => string.Equals(PhaseId, other.PhaseId, StringComparison.Ordinal) &&
               string.Equals(ContentSignature, other.ContentSignature, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is PhasePlan other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(PhaseId, ContentSignature);

        public static bool operator ==(PhasePlan left, PhasePlan right) => left.Equals(right);
        public static bool operator !=(PhasePlan left, PhasePlan right) => !left.Equals(right);

        public static PhasePlan None => new(string.Empty, string.Empty);
    }
}
