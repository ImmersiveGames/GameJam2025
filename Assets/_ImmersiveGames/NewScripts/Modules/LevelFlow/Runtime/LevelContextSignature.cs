using System;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    /// <summary>
    /// Assinatura de contexto do domínio LevelFlow.
    /// Não deve ser reutilizada como MacroSignature do SceneFlow.
    /// </summary>
    public readonly struct LevelContextSignature : IEquatable<LevelContextSignature>
    {
        public static readonly LevelContextSignature Empty = new(string.Empty);

        public LevelContextSignature(string value)
        {
            Value = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public static LevelContextSignature Create(
            LevelId levelId,
            SceneRouteId routeId,
            string reason,
            string contentId = null)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            string normalizedContentId = string.IsNullOrWhiteSpace(contentId) ? string.Empty : contentId.Trim();

            if (string.IsNullOrWhiteSpace(normalizedContentId))
            {
                return new LevelContextSignature($"level:{levelId}|route:{routeId}|reason:{normalizedReason}");
            }

            return new LevelContextSignature($"level:{levelId}|route:{routeId}|content:{normalizedContentId}|reason:{normalizedReason}");
        }

        public override string ToString() => Value;

        public bool Equals(LevelContextSignature other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is LevelContextSignature other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public static bool operator ==(LevelContextSignature left, LevelContextSignature right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LevelContextSignature left, LevelContextSignature right)
        {
            return !left.Equals(right);
        }
    }
}
