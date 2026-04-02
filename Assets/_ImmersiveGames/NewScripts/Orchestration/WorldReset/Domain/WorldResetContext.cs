using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.WorldReset.Domain
{
    public readonly struct WorldResetContext
    {
        public WorldResetContext(string reason, IReadOnlyList<WorldResetScope> scopes, WorldResetFlags flags = WorldResetFlags.None)
            : this(
                ResetKind.Macro,
                reason,
                scopes,
                flags,
                string.Empty,
                string.Empty,
                WorldResetOrigin.Unknown)
        {
        }

        public WorldResetContext(
            ResetKind kind,
            string reason,
            IReadOnlyList<WorldResetScope> scopes,
            WorldResetFlags flags,
            string targetScene,
            string contextSignature,
            WorldResetOrigin origin)
        {
            Kind = kind;
            Reason = string.IsNullOrWhiteSpace(reason) ? "WorldLifecycle/SoftReset" : reason;
            Scopes = scopes ?? Array.Empty<WorldResetScope>();
            Flags = flags;
            TargetScene = string.IsNullOrWhiteSpace(targetScene) ? string.Empty : targetScene.Trim();
            ContextSignature = string.IsNullOrWhiteSpace(contextSignature) ? string.Empty : contextSignature.Trim();
            Origin = origin;
        }

        public ResetKind Kind { get; }
        public string Reason { get; }
        public IReadOnlyList<WorldResetScope> Scopes { get; }
        public WorldResetFlags Flags { get; }
        public string TargetScene { get; }
        public string ContextSignature { get; }
        public WorldResetOrigin Origin { get; }

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
            return $"WorldResetContext(Kind='{Kind}', Reason='{Reason}', TargetScene='{TargetScene}', ContextSignature='{ContextSignature}', Origin='{Origin}', Scopes={scopesLabel}, Flags={Flags})";
        }
    }
}
