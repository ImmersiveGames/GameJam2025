using System;
using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    /// <summary>
    /// Escopos suportados para reset parcial (soft reset) do WorldLifecycle.
    /// Novos escopos podem ser adicionados conforme grupos de gameplay forem habilitados.
    /// </summary>
    public enum ResetScope
    {
        World = 0,
        Players = 1,
        Boss = 2,
        Stage = 3,
        Custom = 99
    }

    /// <summary>
    /// Flags opcionais para contextualizar o reset.
    /// Permite distinguir soft reset de outras estratégias sem proliferar overloads.
    /// </summary>
    [Flags]
    public enum ResetFlags
    {
        None = 0,
        SoftReset = 1 << 0,
        HardReset = 1 << 1
    }

    /// <summary>
    /// Contexto compartilhado entre participantes de reset por escopo.
    /// Transporta metadados para telemetria, logs e filtros.
    /// </summary>
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

        public bool HasScopes => Scopes != null && Scopes.Count > 0;

        public bool ContainsScope(ResetScope scope)
        {
            if (!HasScopes)
            {
                return false;
            }

            for (var i = 0; i < Scopes.Count; i++)
            {
                if (Scopes[i] == scope)
                {
                    return true;
                }
            }

            return false;
        }

        public override string ToString()
        {
            var scopesLabel = !HasScopes ? "<none>" : string.Join(",", Scopes);
            return $"ResetContext(Reason='{Reason}', Scopes={scopesLabel}, Flags={Flags})";
        }
    }

    /// <summary>
    /// Participante opt-in para reset por escopo.
    /// Respeita ordenação determinística por escopo + ordem.
    /// </summary>
    public interface IResetScopeParticipant
    {
        ResetScope Scope { get; }

        int Order { get; }

        System.Threading.Tasks.Task ResetAsync(ResetContext context);
    }
}
