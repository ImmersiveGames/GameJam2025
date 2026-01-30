// Assets/_ImmersiveGames/NewScripts/Infrastructure/Promotion/PromotionGateService.cs
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Promotion
{
    /// <summary>
    /// Implementação padrão de IPromotionGateService.
    ///
    /// Carrega um PromotionGateConfig via Resources (se existir), caso contrário
    /// usa defaults seguros ("default enabled").
    /// </summary>
    public sealed class PromotionGateService : IPromotionGateService
    {
        public const string DefaultResourcesPath = "NewScripts/Config/PromotionGateConfig";

        private readonly HashSet<string> _explicitEnabled;
        private readonly bool _defaultEnabled;

        public string Source { get; }

        private PromotionGateService(string source, bool defaultEnabled, IEnumerable<string> enabled)
        {
            Source = source;
            _defaultEnabled = defaultEnabled;
            _explicitEnabled = new HashSet<string>(enabled ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        }

        public bool IsEnabled(string gateId)
        {
            if (string.IsNullOrWhiteSpace(gateId))
            {
                return _defaultEnabled;
            }

            // Se estiver explicitamente listado, considera habilitado.
            if (_explicitEnabled.Contains(gateId))
            {
                return true;
            }

            return _defaultEnabled;
        }

        public static PromotionGateService CreateFromResourcesOrDefaults(string resourcesPath = DefaultResourcesPath)
        {
            var config = Resources.Load<PromotionGateConfig>(resourcesPath);
            if (config == null)
            {
                var service = new PromotionGateService(
                    source: "defaults(no-config)",
                    defaultEnabled: true,
                    enabled: Array.Empty<string>()
                );

                DebugUtility.Log(typeof(PromotionGateService),
                    $"PromotionGate: nenhum config encontrado em Resources/{resourcesPath}; usando defaults: defaultEnabled=true.",
                    DebugUtility.Colors.Warning);

                return service;
            }

            var gates = (config.EnabledGateIds ?? Array.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var result = new PromotionGateService(
                source: $"Resources/{resourcesPath}",
                defaultEnabled: config.DefaultEnabled,
                enabled: gates
            );

            DebugUtility.Log(typeof(PromotionGateService),
                $"PromotionGate carregado: source='{result.Source}' defaultEnabled={result._defaultEnabled} enabledCount={gates.Length}.",
                DebugUtility.Colors.Info);

            if (gates.Length > 0)
            {
                DebugUtility.Log(typeof(PromotionGateService),
                    $"PromotionGate enabled=[{string.Join(",", gates)}]",
                    DebugUtility.Colors.Info);
            }

            return result;
        }
    }
}
