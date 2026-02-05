#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Modules.Levels.Catalogs;
using _ImmersiveGames.NewScripts.Modules.Levels.Definitions;
using _ImmersiveGames.NewScripts.Modules.Levels.Providers;
namespace _ImmersiveGames.NewScripts.Modules.Levels.Resolvers
{
    /// <summary>
    /// Resolver que combina Catalog + Definitions para gerar LevelPlan.
    /// </summary>
    public sealed class LevelCatalogResolver : ILevelCatalogResolver
    {
        private const string DegradedFeature = "level_catalog";

        private readonly ILevelCatalogProvider _catalogProvider;
        private readonly ILevelDefinitionProvider _definitionProvider;
        private readonly IRuntimeModeProvider _runtimeModeProvider;
        private readonly IDegradedModeReporter _degradedModeReporter;

        public LevelCatalogResolver(
            ILevelCatalogProvider catalogProvider,
            ILevelDefinitionProvider definitionProvider,
            IRuntimeModeProvider? runtimeModeProvider = null,
            IDegradedModeReporter? degradedModeReporter = null)
        {
            _catalogProvider = catalogProvider;
            _definitionProvider = definitionProvider;
            _runtimeModeProvider = runtimeModeProvider ?? new UnityRuntimeModeProvider();
            _degradedModeReporter = degradedModeReporter ?? new DegradedModeReporter();
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
                return FailOrDegrade(
                    reason: "catalog_missing",
                    detail: "Catalog ausente ao resolver nivel inicial.",
                    action: "ResolveInitialLevelId");
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
                return FailOrDegrade(
                    reason: "catalog_missing",
                    detail: "Catalog ausente ao resolver catalogo.",
                    action: "ResolveCatalog");
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
                return FailOrDegrade(
                    reason: "definition_missing",
                    detail: $"LevelDefinition ausente levelId='{levelId}'.",
                    action: "ResolveDefinition");
            }

            DebugUtility.Log<LevelCatalogResolver>(
                $"[OBS][LevelCatalog] DefinitionResolved levelId='{definition.LevelId}' contentId='{definition.ContentId}'.");
            return true;
        }

        public bool TryResolveInitialDefinition(out LevelDefinition definition)
        {
            definition = null!;
            if (!TryResolveInitialLevelId(out string levelId))
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
                return FailOrDegrade(
                    reason: "catalog_missing",
                    detail: "Catalog ausente ao resolver proximo nivel.",
                    action: "ResolveNextLevelId");
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
                return FailOrDegrade(
                    reason: "plan_invalid",
                    detail: $"LevelPlan invalido levelId='{levelId}' contentId='{plan.ContentId}'.",
                    action: "ResolvePlan");
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

            foreach (string? candidateId in EnumerateCandidateLevelIds(catalog))
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
            string current = string.IsNullOrWhiteSpace(levelId) ? string.Empty : levelId.Trim();
            for (int i = 0; i < 32; i++)
            {
                if (!catalog.TryResolveNextLevelId(current, out string? nextId) || string.IsNullOrWhiteSpace(nextId))
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

        private bool FailOrDegrade(string reason, string detail, string action)
        {
            if (_runtimeModeProvider != null && _runtimeModeProvider.IsStrict)
            {
                DebugUtility.LogError<LevelCatalogResolver>(
                    $"[LevelCatalog][STRICT] action='{action}' reason='{reason}'. {detail}");
                throw new InvalidOperationException(
                    $"[LevelCatalog] Strict failure. action='{action}' reason='{reason}'. {detail}");
            }

            _degradedModeReporter?.Report(
                feature: DegradedFeature,
                reason: reason,
                detail: $"action='{action}' {detail}");

            return false;
        }

        private static IEnumerable<string> EnumerateCandidateLevelIds(LevelCatalog catalog)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);

            // 1) Priorizar o initial
            if (!string.IsNullOrWhiteSpace(catalog.InitialLevelId))
            {
                string initial = catalog.InitialLevelId.Trim();
                if (seen.Add(initial))
                {
                    yield return initial;
                }
            }

            // 2) Depois OrderedLevels (se existir)
            if (catalog.OrderedLevels != null)
            {
                foreach (string? id in catalog.OrderedLevels)
                {
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        continue;
                    }

                    string trimmed = id.Trim();
                    if (seen.Add(trimmed))
                    {
                        yield return trimmed;
                    }
                }
            }

            // 3) Por fim Definitions (tipo pode variar: Dictionary, List, etc.)
            if (catalog.Definitions != null)
            {
                foreach (object? entry in (IEnumerable)catalog.Definitions)
                {
                    switch (entry)
                    {
                        // Caso A: Dictionary<string, LevelDefinition>
                        case KeyValuePair<string, LevelDefinition> kv:
                        {
                            string? id = kv.Key;
                            if (!string.IsNullOrWhiteSpace(id))
                            {
                                string trimmed = id.Trim();
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
                            string? id = def.LevelId;
                            if (!string.IsNullOrWhiteSpace(id))
                            {
                                string trimmed = id.Trim();
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
                                string trimmed = idStr.Trim();
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
