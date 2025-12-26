using System;
using System.Collections.Generic;
using System.Linq;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Reset
{
    /// <summary>
    /// Escopos suportados para reset parcial (soft reset) do WorldLifecycle.
    /// </summary>
    public enum ResetScope
    {
        World = 0,
        Players = 1,
        Boss = 2,
        Stage = 3,
        Custom = 99
    }

    [Flags]
    public enum ResetFlags
    {
        None = 0,
        SoftReset = 1 << 0,
        HardReset = 1 << 1
    }

    public readonly struct ResetContext
    {
        public ResetContext(string reason, IReadOnlyList<ResetScope> scopes, ResetFlags flags = ResetFlags.None)
        {
            Reason = string.IsNullOrWhiteSpace(reason) ? "WorldLifecycle/SoftReset" : reason;
            Scopes = scopes ?? Array.Empty<ResetScope>();
            Flags = flags;
        }

        public string Reason { get; }

        public IReadOnlyList<ResetScope> Scopes { get; }

        public ResetFlags Flags { get; }

        public bool HasScopes => Scopes is { Count: > 0 };

        public bool ContainsScope(ResetScope scope)
        {
            if (!HasScopes)
            {
                return false;
            }

            return Scopes.Any(t => t == scope);
        }

        public override string ToString()
        {
            var scopesLabel = !HasScopes ? "<none>" : string.Join(",", Scopes);
            return $"ResetContext(Reason='{Reason}', Scopes={scopesLabel}, Flags={Flags})";
        }
    }

    public interface IResetScopeParticipant
    {
        ResetScope Scope { get; }

        int Order { get; }

        System.Threading.Tasks.Task ResetAsync(ResetContext context);
    }
}
