using System;
using System.Collections.Generic;
using System.Linq;
namespace _ImmersiveGames.NewScripts.Runtime.World.Reset
{
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
            string scopesLabel = !HasScopes ? "<none>" : string.Join(",", Scopes);
            return $"ResetContext(Reason='{Reason}', Scopes={scopesLabel}, Flags={Flags})";
        }
    }
}

