// Assets/_ImmersiveGames/NewScripts/Infrastructure/Promotion/PromotionGateService.cs
#nullable enable

using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;

namespace _ImmersiveGames.NewScripts.Infrastructure.Promotion
{
    /// <summary>
    /// Implementação padrão de IPromotionGateService.
    ///
    /// Carrega um PromotionGateConfig via Resources (se existir), caso contrário
    /// usa defaults seguros ("default enabled").
    /// </summary>
    public sealed class PromotionGateService
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
    }
}
