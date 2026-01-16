using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.QA.Baseline2.Contract
{
    internal sealed class ObservabilityContractModel
    {
        internal IReadOnlyList<ObservabilityDomain> Domains { get; }
        internal IReadOnlyList<string> Errors { get; }

        internal ObservabilityContractModel(
            IReadOnlyList<ObservabilityDomain> domains,
            IReadOnlyList<string> errors)
        {
            Domains = domains;
            Errors = errors;
        }
    }

    internal sealed class ObservabilityDomain
    {
        internal string Name { get; }
        internal IReadOnlyList<string> EvidenceTokens { get; }

        internal ObservabilityDomain(string name, IReadOnlyList<string> evidenceTokens)
        {
            Name = name;
            EvidenceTokens = evidenceTokens;
        }
    }
}
