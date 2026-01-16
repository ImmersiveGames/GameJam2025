using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace _ImmersiveGames.NewScripts.QA.Baseline2.Contract
{
    internal static class ObservabilityContractParser
    {
        private static readonly string[] DomainHeaders =
        {
            "### SceneFlow",
            "### WorldLifecycle",
            "### GameLoop",
            "### InputMode",
            "### PhaseChange"
        };

        internal static ObservabilityContractModel Parse(string contractPath)
        {
            var errors = new List<string>();
            var domains = new List<ObservabilityDomain>();

            if (!File.Exists(contractPath))
            {
                errors.Add($"Contract not found: {contractPath}");
                return new ObservabilityContractModel(domains, errors);
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(contractPath);
            }
            catch (Exception ex)
            {
                errors.Add($"Contract read failed: {ex.GetType().Name}: {ex.Message}");
                return new ObservabilityContractModel(domains, errors);
            }

            var evidenceRegex = new Regex("`([^`]+)`", RegexOptions.Compiled);
            var currentDomain = string.Empty;
            var currentEvidence = new List<string>();
            var foundDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void FlushDomain()
            {
                if (string.IsNullOrEmpty(currentDomain))
                    return;

                domains.Add(new ObservabilityDomain(currentDomain, currentEvidence.ToArray()));
                foundDomains.Add(currentDomain);
                currentEvidence = new List<string>();
            }

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                var headerMatch = TryMatchDomainHeader(line, out var domainName);
                if (headerMatch)
                {
                    FlushDomain();
                    currentDomain = domainName;
                    continue;
                }

                if (string.IsNullOrEmpty(currentDomain))
                    continue;

                foreach (Match match in evidenceRegex.Matches(line))
                {
                    var token = match.Groups[1].Value.Trim();
                    if (string.IsNullOrEmpty(token))
                        continue;

                    if (!currentEvidence.Contains(token))
                        currentEvidence.Add(token);
                }
            }

            FlushDomain();

            foreach (var header in DomainHeaders)
            {
                var domainName = header.Replace("### ", string.Empty);
                if (!foundDomains.Contains(domainName))
                    errors.Add($"Domain section missing: {domainName}");
            }

            return new ObservabilityContractModel(domains, errors);
        }

        private static bool TryMatchDomainHeader(string line, out string domainName)
        {
            foreach (var header in DomainHeaders)
            {
                if (!line.Equals(header, StringComparison.OrdinalIgnoreCase))
                    continue;

                domainName = header.Replace("### ", string.Empty);
                return true;
            }

            domainName = string.Empty;
            return false;
        }
    }
}
