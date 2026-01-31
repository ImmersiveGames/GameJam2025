#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Catalogs;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Definitions;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Providers;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Promotion;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels.Resolvers
{
    /// <summary>
    /// Resolver que combina Catalog + Definitions para gerar LevelPlan.
    /// </summary>
    public sealed class LevelCatalogResolver : ILevelCatalogResolver
    {
        private readonly ILevelCatalogProvider _catalogProvider;
        private readonly ILevelDefinitionProvider _definitionProvider;
        private readonly PromotionGateService? _promotionGate;

        public LevelCatalogResolver(
            ILevelCatalogProvider catalogProvider,
            ILevelDefinitionProvider definitionProvider,
            PromotionGateService? promotionGate = null)
        {
            _catalogProvider = catalogProvider;
            _definitionProvider = definitionProvider;
            _promotionGate = promotionGate;
        }

        public bool TryResolveInitialLevelId(out string levelId)
        {
            levelId = string.Empty;
            var catalog = _catalogProvider.GetCatalog();
            if (catalog == null)
            {
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    "[LevelCatalog] Catalog ausente ao resolver nível inicial.");
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    "[OBS][LevelCatalog] CatalogMissing action='ResolveInitialLevelId'.");
                return false;
            }

            if (!catalog.TryResolveInitialLevelId(out levelId))
            {
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    "[LevelCatalog] Nível inicial não configurado no catalog.");
                levelId = string.Empty;
                return false;
            }

            DebugUtility.Log<LevelCatalogResolver>($"[OBS][LevelCatalog] InitialResolved levelId='{levelId}'.");
            return true;
        }

        public bool TryResolveCatalog(out LevelCatalog catalog)
        {
            catalog = _catalogProvider.GetCatalog();
            if (catalog == null)
            {
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    "[LevelCatalog] Catalog ausente ao resolver catálogo.");
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    "[OBS][LevelCatalog] CatalogMissing action='ResolveCatalog'.");
                return false;
            }

            return true;
        }

        public bool TryResolveDefinition(string levelId, out LevelDefinition definition)
        {
            definition = null!;

            if (!_definitionProvider.TryGetDefinition(levelId, out definition) || definition == null)
            {
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    $"[LevelCatalog] LevelDefinition ausente. levelId='{levelId}'.");
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    $"[OBS][LevelCatalog] DefinitionMissing levelId='{levelId}'.");

                if (_catalogProvider.GetCatalog() == null)
                {
                    DebugUtility.LogWarning<LevelCatalogResolver>(
                        "[OBS][LevelCatalog] CatalogMissing action='ResolveDefinition'.");
                }

                definition = null!;
                return false;
            }

            DebugUtility.Log<LevelCatalogResolver>(
                $"[OBS][LevelCatalog] DefinitionResolved levelId='{definition.LevelId}' contentId='{definition.ContentId}'.");
            return true;
        }

        public bool TryResolveInitialDefinition(out LevelDefinition definition)
        {
            definition = null!;
            if (!TryResolveInitialLevelId(out var levelId))
            {
                return false;
            }

            return TryResolveDefinition(levelId, out definition);
        }

        public bool TryResolveNextLevelId(string levelId, out string nextLevelId)
        {
            nextLevelId = string.Empty;
            var catalog = _catalogProvider.GetCatalog();
            if (catalog == null)
            {
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    "[LevelCatalog] Catalog ausente ao resolver próximo nível.");
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    "[OBS][LevelCatalog] CatalogMissing action='ResolveNextLevelId'.");
                return false;
            }

            if (!catalog.TryResolveNextLevelId(levelId, out nextLevelId))
            {
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    $"[LevelCatalog] Próximo nível não resolvido. levelId='{levelId}'.");
                nextLevelId = string.Empty;
                return false;
            }

            return true;
        }

        public bool TryResolvePlan(string levelId, out LevelPlan plan, out LevelChangeOptions options)
        {
            plan = LevelPlan.None;
            options = LevelChangeOptions.Default.Clone();

            if (!TryResolveDefinition(levelId, out var definition))
            {
                return false;
            }

            plan = definition.ToPlan();
            if (!plan.IsValid)
            {
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    $"[LevelCatalog] LevelPlan inválido para levelId='{levelId}'. contentId='{plan.ContentId}'.");
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    $"[OBS][LevelCatalog] PlanInvalid levelId='{levelId}' contentId='{plan.ContentId}'.");
                plan = LevelPlan.None;
                return false;
            }

            if (!IsEnabled(definition, out var gateId))
            {
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    $"[LevelCatalog] Nível bloqueado por gate. levelId='{definition.LevelId}' gateId='{gateId}'.");
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    $"[OBS][LevelCatalog] PlanRejected levelId='{definition.LevelId}' contentId='{definition.ContentId}' gateId='{gateId}' detail='GateDisabled'.");
                plan = LevelPlan.None;
                return false;
            }

            options = definition.DefaultOptions.Clone();
            return true;
        }

        public bool TryResolveInitialPlan(out LevelPlan plan, out LevelChangeOptions options)
        {
            plan = LevelPlan.None;
            options = LevelChangeOptions.Default.Clone();

            if (!TryResolveCatalog(out var catalog))
            {
                return false;
            }

            foreach (var candidateId in EnumerateCandidateLevelIds(catalog))
            {
                if (TryResolvePlan(candidateId, out plan, out options))
                {
                    DebugUtility.Log<LevelCatalogResolver>(
                        $"[OBS][LevelCatalog] InitialPlanResolved levelId='{plan.LevelId}' contentId='{plan.ContentId}' source='FirstEligible'.");
                    return true;
                }
            }

            DebugUtility.LogWarning<LevelCatalogResolver>(
                "[LevelCatalog] Nenhum nível elegível encontrado ao resolver plano inicial.");
            DebugUtility.LogWarning<LevelCatalogResolver>(
                "[OBS][LevelCatalog] InitialPlanMissing detail='NoEligibleLevels'.");
            return false;
        }

        public bool TryResolveNextPlan(string levelId, out LevelPlan plan, out LevelChangeOptions options)
        {
            plan = LevelPlan.None;
            options = LevelChangeOptions.Default.Clone();

            if (!TryResolveCatalog(out var catalog))
            {
                return false;
            }

            var visited = new HashSet<string>(StringComparer.Ordinal);
            var current = string.IsNullOrWhiteSpace(levelId) ? string.Empty : levelId.Trim();
            for (var i = 0; i < 32; i++)
            {
                if (!catalog.TryResolveNextLevelId(current, out var nextId) || string.IsNullOrWhiteSpace(nextId))
                {
                    DebugUtility.LogWarning<LevelCatalogResolver>(
                        $"[LevelCatalog] Próximo nível não resolvido. levelId='{current}'.");
                    return false;
                }

                nextId = nextId.Trim();
                if (!visited.Add(nextId))
                {
                    DebugUtility.LogWarning<LevelCatalogResolver>(
                        $"[LevelCatalog] Loop detectado ao resolver próximo nível. start='{levelId}' candidate='{nextId}'.");
                    DebugUtility.LogWarning<LevelCatalogResolver>(
                        $"[OBS][LevelCatalog] NextPlanMissing startLevelId='{levelId}' detail='LoopDetected'.");
                    return false;
                }

                if (TryResolvePlan(nextId, out plan, out options))
                {
                    DebugUtility.Log<LevelCatalogResolver>(
                        $"[OBS][LevelCatalog] NextPlanResolved startLevelId='{levelId}' levelId='{plan.LevelId}' contentId='{plan.ContentId}'.");
                    return true;
                }

                // Se o plano não é elegível, tentar avançar de novo.
                current = nextId;
            }

            DebugUtility.LogWarning<LevelCatalogResolver>(
                $"[LevelCatalog] Nenhum próximo nível elegível encontrado. start='{levelId}'.");
            DebugUtility.LogWarning<LevelCatalogResolver>(
                $"[OBS][LevelCatalog] NextPlanMissing startLevelId='{levelId}' detail='NoEligibleNext'.");
            return false;
        }

        private bool IsEnabled(LevelDefinition definition, out string gateId)
        {
            gateId = definition.PromotionGateId;
            if (string.IsNullOrEmpty(gateId))
            {
                return true;
            }

            if (_promotionGate == null)
            {
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    $"[LevelCatalog] PromotionGateService ausente; permitindo nível com gateId='{gateId}'.");
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    $"[OBS][LevelCatalog] GateServiceMissing gateId='{gateId}' default='allow'.");
                return true;
            }

            return _promotionGate.IsEnabled(gateId);
        }

        private static IEnumerable<string> EnumerateCandidateLevelIds(LevelCatalog catalog)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);

            // 1) Priorizar o initial
            if (!string.IsNullOrWhiteSpace(catalog.InitialLevelId))
            {
                var initial = catalog.InitialLevelId.Trim();
                if (seen.Add(initial))
                {
                    yield return initial;
                }
            }

            // 2) Depois OrderedLevels (se existir)
            if (catalog.OrderedLevels != null)
            {
                foreach (var id in catalog.OrderedLevels)
                {
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        continue;
                    }

                    var trimmed = id.Trim();
                    if (seen.Add(trimmed))
                    {
                        yield return trimmed;
                    }
                }
            }

            // 3) Por fim Definitions (tipo pode variar: Dictionary, List, etc.)
            if (catalog.Definitions != null)
            {
                foreach (var entry in (IEnumerable)catalog.Definitions)
                {
                    switch (entry)
                    {
                        // Caso A: Dictionary<string, LevelDefinition>
                        case KeyValuePair<string, LevelDefinition> kv:
                        {
                            var id = kv.Key;
                            if (!string.IsNullOrWhiteSpace(id))
                            {
                                var trimmed = id.Trim();
                                if (seen.Add(trimmed))
                                {
                                    yield return trimmed;
                                }
                            }

                            break;
                        }

                        // Caso B: List/Array de LevelDefinition
                        case LevelDefinition def:
                        {
                            var id = def.LevelId;
                            if (!string.IsNullOrWhiteSpace(id))
                            {
                                var trimmed = id.Trim();
                                if (seen.Add(trimmed))
                                {
                                    yield return trimmed;
                                }
                            }

                            break;
                        }

                        // Caso C: coleção de string (ids)
                        case string idStr:
                        {
                            if (!string.IsNullOrWhiteSpace(idStr))
                            {
                                var trimmed = idStr.Trim();
                                if (seen.Add(trimmed))
                                {
                                    yield return trimmed;
                                }
                            }

                            break;
                        }

                        // Outros formatos: ignorar (não quebrar)
                        default:
                            break;
                    }
                }
            }
        }
    }
}
