using System;
using System.Collections.Generic;
using System.Linq;
namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldReset
{
    public readonly struct WorldResetContext
    {
        public WorldResetContext(string reason, IReadOnlyList<WorldResetScope> scopes, WorldResetFlags flags = WorldResetFlags.None)
        {
            Reason = string.IsNullOrWhiteSpace(reason) ? "WorldLifecycle/SoftReset" : reason;
            Scopes = scopes ?? Array.Empty<WorldResetScope>();
            Flags = flags;
        }

        public string Reason { get; }

        public IReadOnlyList<WorldResetScope> Scopes { get; }

        public WorldResetFlags Flags { get; }

        public bool HasScopes => Scopes is { Count: > 0 };

        public bool ContainsScope(WorldResetScope scope)
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
            return $"WorldResetContext(Reason='{Reason}', Scopes={scopesLabel}, Flags={Flags})";
        }
    }
}

