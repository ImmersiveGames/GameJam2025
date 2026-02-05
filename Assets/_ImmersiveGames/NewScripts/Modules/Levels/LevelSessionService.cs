#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Levels.Catalogs;
using _ImmersiveGames.NewScripts.Modules.Levels.Resolvers;
namespace _ImmersiveGames.NewScripts.Modules.Levels
{

    /// <summary>
    /// Serviço de sessão de níveis: seleção atual + aplicação idempotente.
    /// </summary>
    public sealed class LevelSessionService : ILevelSessionService
    {
        private const string DefaultInitializeReason = "LevelSession/Initialize";
        private const string DefaultSelectReason = "LevelSession/Select";
        private const string DefaultApplyReason = "LevelSession/Apply";

        private readonly ILevelCatalogResolver _resolver;
        private readonly ILevelManager _levelManager;
        private readonly List<string> _orderedLevels = new();

        private bool _catalogSnapshotResolved;
        private LevelChangeOptions _selectedOptions = LevelChangeOptions.Default.Clone();

        public string SelectedLevelId { get; private set; } = string.Empty;
        public LevelPlan SelectedPlan { get; private set; } = LevelPlan.None;
        public string AppliedLevelId { get; private set; } = string.Empty;
        public LevelPlan AppliedPlan { get; private set; } = LevelPlan.None;

        public LevelSessionService(ILevelCatalogResolver resolver, ILevelManager levelManager)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _levelManager = levelManager ?? throw new ArgumentNullException(nameof(levelManager));
        }

        public bool Initialize()
        {
            string reason = DefaultInitializeReason;
            EnsureCatalogSnapshot(reason);
            return SelectInitialInternal(reason, out _);
        }

        public bool SelectInitial(string reason)
        {
            string normalizedReason = NormalizeReason(reason, DefaultSelectReason);
            return SelectInitialInternal(normalizedReason, out _);
        }

        public bool SelectLevelById(string levelId, string reason)
        {
            string normalizedReason = NormalizeReason(reason, DefaultSelectReason);
            string normalizedLevelId = NormalizeLevelId(levelId);
            if (normalizedLevelId.Length == 0)
            {
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] SelectionFailed levelId='' contentId='' reason='{normalizedReason}' detail='EmptyLevelId'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            if (!_resolver.TryResolvePlan(normalizedLevelId, out var plan, out var options))
            {
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] SelectionFailed levelId='{normalizedLevelId}' contentId='' reason='{normalizedReason}' detail='PlanNotResolved'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            ApplySelection(plan, options);

            DebugUtility.Log<LevelSessionService>(
                $"[OBS][Level] Selected levelId='{plan.LevelId}' contentId='{plan.ContentId}' reason='{normalizedReason}' source='Direct'.",
                DebugUtility.Colors.Info);

            return true;
        }

        public bool SelectNext(string reason)
        {
            string normalizedReason = NormalizeReason(reason, DefaultSelectReason);
            if (string.IsNullOrWhiteSpace(SelectedLevelId))
            {
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] SelectionFailed levelId='' contentId='' reason='{normalizedReason}' detail='NoSelection'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            if (!_resolver.TryResolveNextPlan(SelectedLevelId, out var plan, out var options))
            {
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] SelectionFailed levelId='{SelectedLevelId}' contentId='{SelectedPlan.ContentId}' reason='{normalizedReason}' detail='NextNotResolved'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            ApplySelection(plan, options);

            DebugUtility.Log<LevelSessionService>(
                $"[OBS][Level] Selected levelId='{plan.LevelId}' contentId='{plan.ContentId}' reason='{normalizedReason}' source='Next'.",
                DebugUtility.Colors.Info);

            return true;
        }

        public bool SelectPrevious(string reason)
        {
            string normalizedReason = NormalizeReason(reason, DefaultSelectReason);
            if (string.IsNullOrWhiteSpace(SelectedLevelId))
            {
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] SelectionFailed levelId='' contentId='' reason='{normalizedReason}' detail='NoSelection'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            if (!EnsureCatalogSnapshot(normalizedReason))
            {
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] SelectionFailed levelId='{SelectedLevelId}' contentId='{SelectedPlan.ContentId}' reason='{normalizedReason}' detail='CatalogMissing'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            string previousId = ResolvePreviousLevelId(SelectedLevelId);
            if (string.IsNullOrWhiteSpace(previousId))
            {
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] SelectionFailed levelId='{SelectedLevelId}' contentId='{SelectedPlan.ContentId}' reason='{normalizedReason}' detail='PreviousNotResolved'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            if (!_resolver.TryResolvePlan(previousId, out var plan, out var options))
            {
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] SelectionFailed levelId='{previousId}' contentId='' reason='{normalizedReason}' detail='PlanNotResolved'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            ApplySelection(plan, options);

            DebugUtility.Log<LevelSessionService>(
                $"[OBS][Level] Selected levelId='{plan.LevelId}' contentId='{plan.ContentId}' reason='{normalizedReason}' source='Previous'.",
                DebugUtility.Colors.Info);

            return true;
        }

        public bool ApplySelected(string reason)
        {
            string normalizedReason = NormalizeReason(reason, DefaultApplyReason);

            DebugUtility.Log<LevelSessionService>(
                $"[OBS][Level] ApplyRequested levelId='{SelectedPlan.LevelId}' contentId='{SelectedPlan.ContentId}' selectedLevelId='{SelectedPlan.LevelId}' selectedContentId='{SelectedPlan.ContentId}' appliedLevelId='{AppliedPlan.LevelId}' appliedContentId='{AppliedPlan.ContentId}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            if (!SelectedPlan.IsValid)
            {
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] ApplySkipped levelId='' contentId='' reason='{normalizedReason}' detail='NoSelection'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            if (AppliedPlan == SelectedPlan)
            {
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] ApplySkipped levelId='{SelectedPlan.LevelId}' contentId='{SelectedPlan.ContentId}' reason='{normalizedReason}' detail='AlreadyApplied'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            _ = _levelManager.RequestLevelInPlaceAsync(SelectedPlan, normalizedReason, _selectedOptions);

            AppliedPlan = SelectedPlan;
            AppliedLevelId = SelectedPlan.LevelId;

            DebugUtility.Log<LevelSessionService>(
                $"[OBS][Level] Applied levelId='{SelectedPlan.LevelId}' contentId='{SelectedPlan.ContentId}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);

            return true;
        }

        private bool SelectInitialInternal(string reason, out string detail)
        {
            detail = "CatalogInitial";
            if (_resolver.TryResolveInitialPlan(out var plan, out var options))
            {
                ApplySelection(plan, options);
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] InitialSelected levelId='{plan.LevelId}' contentId='{plan.ContentId}' reason='{reason}' source='CatalogInitial'.",
                    DebugUtility.Colors.Info);
                return true;
            }

            if (!EnsureCatalogSnapshot(reason))
            {
                detail = "CatalogMissing";
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] InitialSelectionFailed levelId='' contentId='' reason='{reason}' detail='{detail}'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            if (TryResolveFallbackPlan(out plan, out options, out detail))
            {
                ApplySelection(plan, options);
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] InitialSelected levelId='{plan.LevelId}' contentId='{plan.ContentId}' reason='{reason}' source='{detail}'.",
                    DebugUtility.Colors.Info);
                return true;
            }

            DebugUtility.Log<LevelSessionService>(
                $"[OBS][Level] InitialSelectionFailed levelId='' contentId='' reason='{reason}' detail='{detail}'.",
                DebugUtility.Colors.Warning);
            return false;
        }

        private void ApplySelection(LevelPlan plan, LevelChangeOptions? options)
        {
            // Atualiza o estado de seleção atual de forma centralizada.
            SelectedPlan = plan;
            SelectedLevelId = plan.LevelId;
            _selectedOptions = options?.Clone() ?? LevelChangeOptions.Default.Clone();
        }

        private bool EnsureCatalogSnapshot(string reason)
        {
            if (_catalogSnapshotResolved)
            {
                return true;
            }

            if (!_resolver.TryResolveCatalog(out var catalog))
            {
                _orderedLevels.Clear();
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] CatalogSnapshotFailed levels='<missing>' count='0' reason='{reason}' detail='CatalogMissing'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            BuildOrderedLevels(catalog);
            _catalogSnapshotResolved = true;

            DebugUtility.Log<LevelSessionService>(
                $"[OBS][Level] CatalogSnapshotResolved levels='{FormatLevels(_orderedLevels)}' count='{_orderedLevels.Count}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            return true;
        }

        private void BuildOrderedLevels(LevelCatalog catalog)
        {
            _orderedLevels.Clear();

            if (catalog == null)
            {
                return;
            }

            if (catalog.OrderedLevels != null && catalog.OrderedLevels.Count > 0)
            {
                AddLevels(catalog.OrderedLevels);
                return;
            }

            if (catalog.Definitions != null && catalog.Definitions.Count > 0)
            {
                foreach (var definition in catalog.Definitions)
                {
                    if (definition == null)
                    {
                        continue;
                    }

                    string id = NormalizeLevelId(definition.LevelId);
                    if (id.Length == 0)
                    {
                        continue;
                    }

                    if (!_orderedLevels.Contains(id))
                    {
                        _orderedLevels.Add(id);
                    }
                }
            }
        }

        private void AddLevels(IReadOnlyList<string>? levels)
        {
            if (levels == null)
            {
                return;
            }

            foreach (string? entry in levels)
            {
                string id = NormalizeLevelId(entry);
                if (id.Length == 0)
                {
                    continue;
                }

                if (!_orderedLevels.Contains(id))
                {
                    _orderedLevels.Add(id);
                }
            }
        }

        private bool TryResolveFallbackPlan(out LevelPlan plan, out LevelChangeOptions options, out string detail)
        {
            plan = LevelPlan.None;
            options = LevelChangeOptions.Default.Clone();
            detail = "FallbackEmpty";

            if (_orderedLevels.Count == 0)
            {
                return false;
            }

            foreach (string? levelId in _orderedLevels)
            {
                if (_resolver.TryResolvePlan(levelId, out plan, out options))
                {
                    detail = "FallbackOrdered";
                    return true;
                }
            }

            detail = "FallbackNoValidPlan";
            return false;
        }

        private string ResolvePreviousLevelId(string currentLevelId)
        {
            if (_orderedLevels.Count == 0)
            {
                return string.Empty;
            }

            string normalized = NormalizeLevelId(currentLevelId);
            if (normalized.Length == 0)
            {
                return string.Empty;
            }

            for (int index = 0; index < _orderedLevels.Count; index++)
            {
                if (!string.Equals(_orderedLevels[index], normalized, StringComparison.Ordinal))
                {
                    continue;
                }

                if (index == 0)
                {
                    return string.Empty;
                }

                return _orderedLevels[index - 1];
            }

            return string.Empty;
        }

        private static string NormalizeReason(string reason, string fallback)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return fallback;
            }

            return reason.Trim();
        }

        private static string NormalizeLevelId(string levelId)
        {
            return string.IsNullOrWhiteSpace(levelId) ? string.Empty : levelId.Trim();
        }

        private static string FormatLevels(IReadOnlyList<string>? levels)
        {
            if (levels == null || levels.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (int index = 0; index < levels.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                builder.Append(levels[index]);
            }

            return builder.ToString();
        }
    }
}
