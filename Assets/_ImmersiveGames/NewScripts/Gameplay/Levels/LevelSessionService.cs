// Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/LevelSessionService.cs

#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Catalogs;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Resolvers;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels
{
    public interface ILevelSessionService
    {
        bool Initialize();
        bool SelectInitial(string reason);
        bool SelectLevelById(string levelId, string reason);
        bool SelectNext(string reason);
        bool SelectPrevious(string reason);
        void ResetSelection(string reason);
        Task<bool> ApplySelectedAsync(string reason, LevelChangeOptions? options = null);
        LevelPlan SelectedPlan { get; }
        LevelPlan AppliedPlan { get; }
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LevelSessionService : ILevelSessionService
    {
        private const string DefaultInitializeReason = "LevelSession/Initialize";
        private const string DefaultSelectReason = "LevelSession/Select";
        private const string DefaultApplyReason = "LevelSession/Apply";

        private readonly ILevelCatalogResolver _resolver;
        private readonly ILevelManager _levelManager;

        private readonly List<string> _orderedLevels = new();

        private bool _disabled;
        private string _disabledReason = string.Empty;

        private bool _catalogSnapshotResolved;
        private LevelCatalog? _catalogSnapshot;

        public LevelPlan SelectedPlan { get; private set; } = LevelPlan.Invalid;
        public LevelPlan AppliedPlan { get; private set; } = LevelPlan.Invalid;

        public LevelSessionService(ILevelCatalogResolver resolver, ILevelManager levelManager)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _levelManager = levelManager ?? throw new ArgumentNullException(nameof(levelManager));
        }

        public bool Initialize()
        {
            var reason = DefaultInitializeReason;

            if (_disabled)
            {
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] InitializeSkipped reason='{reason}' detail='Disabled' disabledReason='{_disabledReason}'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            if (!EnsureCatalogSnapshot(reason))
            {
                Disable("CatalogMissing", reason);
                return false;
            }

            return SelectInitialInternal(reason, out _);
        }

        public bool SelectInitial(string reason)
        {
            var normalizedReason = NormalizeReason(reason, DefaultSelectReason);

            if (_disabled)
            {
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] SelectionSkipped levelId='{SelectedPlan.LevelId}' contentId='{SelectedPlan.ContentId}' reason='{normalizedReason}' detail='Disabled' disabledReason='{_disabledReason}'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            return SelectInitialInternal(normalizedReason, out _);
        }

        public bool SelectLevelById(string levelId, string reason)
        {
            var normalizedReason = NormalizeReason(reason, DefaultSelectReason);

            if (_disabled)
            {
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] SelectionSkipped levelId='{levelId}' contentId='<unknown>' reason='{normalizedReason}' detail='Disabled' disabledReason='{_disabledReason}'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(levelId))
            {
                DebugUtility.LogWarning<LevelSessionService>(
                    $"[Level] Ignorando SelectLevelById com id vazio. reason='{normalizedReason}'.");
                return false;
            }

            var trimmed = levelId.Trim();

            if (!TryResolvePlanCompat(trimmed, normalizedReason, out var plan) || !plan.IsValid)
            {
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] SelectionFailed levelId='{trimmed}' contentId='<missing>' reason='{normalizedReason}' detail='PlanMissing'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            SelectedPlan = plan;

            DebugUtility.Log<LevelSessionService>(
                $"[OBS][Level] Selected levelId='{plan.LevelId}' contentId='{plan.ContentId}' reason='{normalizedReason}' source='Direct'.",
                DebugUtility.Colors.Info);

            return true;
        }

        public bool SelectNext(string reason)
        {
            var normalizedReason = NormalizeReason(reason, DefaultSelectReason);

            if (_disabled)
            {
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] SelectionSkipped levelId='{SelectedPlan.LevelId}' contentId='{SelectedPlan.ContentId}' reason='{normalizedReason}' detail='Disabled' disabledReason='{_disabledReason}'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            if (!EnsureCatalogSnapshot(normalizedReason))
            {
                Disable("CatalogMissing", normalizedReason);
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] SelectionFailed levelId='{SelectedPlan.LevelId}' contentId='{SelectedPlan.ContentId}' reason='{normalizedReason}' detail='CatalogMissing'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            if (_orderedLevels.Count == 0)
            {
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] SelectionFailed levelId='{SelectedPlan.LevelId}' contentId='{SelectedPlan.ContentId}' reason='{normalizedReason}' detail='NoOrderedLevels'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            var initialId = GetInitialLevelId(_catalogSnapshot);
            var current = SelectedPlan.IsValid ? SelectedPlan.LevelId : initialId;

            var idx = Math.Max(0, _orderedLevels.IndexOf(current));
            var nextIdx = (idx + 1) % _orderedLevels.Count;

            return SelectLevelById(_orderedLevels[nextIdx], normalizedReason);
        }

        public bool SelectPrevious(string reason)
        {
            var normalizedReason = NormalizeReason(reason, DefaultSelectReason);

            if (_disabled)
            {
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] SelectionSkipped levelId='{SelectedPlan.LevelId}' contentId='{SelectedPlan.ContentId}' reason='{normalizedReason}' detail='Disabled' disabledReason='{_disabledReason}'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            if (!EnsureCatalogSnapshot(normalizedReason))
            {
                Disable("CatalogMissing", normalizedReason);
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] SelectionFailed levelId='{SelectedPlan.LevelId}' contentId='{SelectedPlan.ContentId}' reason='{normalizedReason}' detail='CatalogMissing'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            if (_orderedLevels.Count == 0)
            {
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] SelectionFailed levelId='{SelectedPlan.LevelId}' contentId='{SelectedPlan.ContentId}' reason='{normalizedReason}' detail='NoOrderedLevels'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            var initialId = GetInitialLevelId(_catalogSnapshot);
            var current = SelectedPlan.IsValid ? SelectedPlan.LevelId : initialId;

            var idx = Math.Max(0, _orderedLevels.IndexOf(current));
            var prevIdx = (idx - 1 + _orderedLevels.Count) % _orderedLevels.Count;

            return SelectLevelById(_orderedLevels[prevIdx], normalizedReason);
        }

        public void ResetSelection(string reason)
        {
            var normalizedReason = NormalizeReason(reason, DefaultSelectReason);

            SelectedPlan = LevelPlan.Invalid;

            DebugUtility.Log<LevelSessionService>(
                $"[OBS][Level] SelectionReset levelId='<none>' contentId='<none>' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);
        }

        public async Task<bool> ApplySelectedAsync(string reason, LevelChangeOptions? options = null)
        {
            var normalizedReason = NormalizeReason(reason, DefaultApplyReason);

            if (_disabled)
            {
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] ApplySkipped levelId='{SelectedPlan.LevelId}' contentId='{SelectedPlan.ContentId}' reason='{normalizedReason}' detail='Disabled' disabledReason='{_disabledReason}'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            if (!SelectedPlan.IsValid)
            {
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] ApplyFailed levelId='<none>' contentId='<none>' reason='{normalizedReason}' detail='NoSelection' catalogResolved='{_catalogSnapshotResolved}'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            if (AppliedPlan.IsValid && AppliedPlan.Equals(SelectedPlan))
            {
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] ApplySkipped levelId='{SelectedPlan.LevelId}' contentId='{SelectedPlan.ContentId}' reason='{normalizedReason}' detail='AlreadyApplied'.",
                    DebugUtility.Colors.Info);
                return true;
            }

            var normalizedOptions = options?.Clone() ?? LevelChangeOptions.Default.Clone();

            DebugUtility.Log<LevelSessionService>(
                $"[OBS][Level] ApplyRequested levelId='{SelectedPlan.LevelId}' contentId='{SelectedPlan.ContentId}' reason='{normalizedReason}' mode='InPlace'.",
                DebugUtility.Colors.Info);

            await _levelManager.RequestLevelInPlaceAsync(SelectedPlan, normalizedReason, normalizedOptions);

            AppliedPlan = SelectedPlan;

            DebugUtility.Log<LevelSessionService>(
                $"[OBS][Level] Applied levelId='{AppliedPlan.LevelId}' contentId='{AppliedPlan.ContentId}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);

            return true;
        }

        private bool SelectInitialInternal(string reason, out LevelPlan plan)
        {
            plan = LevelPlan.Invalid;

            if (!EnsureCatalogSnapshot(reason))
            {
                Disable("CatalogMissing", reason);
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] InitialResolveFailed levelId='<missing>' contentId='<missing>' reason='{reason}' detail='CatalogMissing'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            var initialId = GetInitialLevelId(_catalogSnapshot);
            if (string.IsNullOrWhiteSpace(initialId))
            {
                Disable("InitialMissing", reason);
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] InitialResolveFailed levelId='<missing>' contentId='<missing>' reason='{reason}' detail='InitialMissing'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            if (!TryResolvePlanCompat(initialId, reason, out plan) || !plan.IsValid)
            {
                Disable("InitialPlanMissing", reason);
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] InitialResolveFailed levelId='{initialId}' contentId='<missing>' reason='{reason}' detail='InitialPlanMissing'.",
                    DebugUtility.Colors.Warning);
                plan = LevelPlan.Invalid;
                return false;
            }

            SelectedPlan = plan;

            DebugUtility.Log<LevelSessionService>(
                $"[OBS][Level] InitialResolved levelId='{plan.LevelId}' contentId='{plan.ContentId}' reason='{reason}' source='CatalogInitial'.",
                DebugUtility.Colors.Info);

            return true;
        }

        private bool EnsureCatalogSnapshot(string reason)
        {
            if (_disabled)
            {
                return false;
            }

            if (_catalogSnapshotResolved)
            {
                return _catalogSnapshot != null;
            }

            if (!TryResolveCatalogCompat(reason, out var catalog) || catalog == null)
            {
                DebugUtility.Log<LevelSessionService>(
                    $"[OBS][Level] CatalogResolveFailed reason='{reason}' detail='CatalogMissing'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            _catalogSnapshot = catalog;
            _catalogSnapshotResolved = true;

            BuildOrderedLevelsCompat(catalog);

            DebugUtility.Log<LevelSessionService>(
                $"[OBS][Level] CatalogResolved levels='{FormatLevels(_orderedLevels)}' count='{_orderedLevels.Count}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            return true;
        }

        private void BuildOrderedLevelsCompat(LevelCatalog catalog)
        {
            _orderedLevels.Clear();

            // 1) Tenta OrderedLevels / OrderedLevelIds etc.
            var ordered = GetStringListCompat(catalog, "OrderedLevels", "OrderedLevelIds", "Ordered");
            if (ordered.Count > 0)
            {
                _orderedLevels.AddRange(ordered);
                return;
            }

            // 2) Fallback: tenta Definitions.Keys (dicionário)
            var keys = GetDictionaryKeysCompat(catalog, "Definitions", "LevelDefinitions");
            if (keys.Count > 0)
            {
                _orderedLevels.AddRange(keys.OrderBy(s => s, StringComparer.Ordinal));
            }
        }

        private void Disable(string detail, string reason)
        {
            if (_disabled)
            {
                return;
            }

            _disabled = true;
            _disabledReason = string.IsNullOrWhiteSpace(detail) ? "Unknown" : detail.Trim();

            DebugUtility.Log<LevelSessionService>(
                $"[OBS][Level] ServiceDisabled reason='{reason}' detail='{_disabledReason}'.",
                DebugUtility.Colors.Warning);
        }

        private static string NormalizeReason(string reason, string fallback)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return fallback;
            }

            return reason.Trim();
        }

        private static string FormatLevels(List<string> levels)
        {
            if (levels.Count == 0)
            {
                return "<none>";
            }

            if (levels.Count <= 8)
            {
                return string.Join(",", levels);
            }

            return string.Join(",", levels.Take(8)) + $",...(+{levels.Count - 8})";
        }

        // ---------------------------
        // Compat / Reflection helpers
        // ---------------------------

        private bool TryResolveCatalogCompat(string reason, out LevelCatalog? catalog)
        {
            catalog = null;

            var type = _resolver.GetType();
            var byRefCatalog = typeof(LevelCatalog).MakeByRefType();

            // bool TryResolveCatalog(string reason, out LevelCatalog catalog)
            var m1 = type.GetMethod("TryResolveCatalog", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null, types: new[] { typeof(string), byRefCatalog }, modifiers: null);
            if (m1 != null)
            {
                var args = new object?[] { reason, null };
                var result = m1.Invoke(_resolver, args);
                if (result is bool ok && ok)
                {
                    catalog = args[1] as LevelCatalog;
                    return catalog != null;
                }

                return false;
            }

            // bool TryResolveCatalog(out LevelCatalog catalog)
            var m2 = type.GetMethod("TryResolveCatalog", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null, types: new[] { byRefCatalog }, modifiers: null);
            if (m2 != null)
            {
                var args = new object?[] { null };
                var result = m2.Invoke(_resolver, args);
                if (result is bool ok && ok)
                {
                    catalog = args[0] as LevelCatalog;
                    return catalog != null;
                }

                return false;
            }

            // Nenhuma assinatura conhecida
            return false;
        }

        private bool TryResolvePlanCompat(string levelId, string reason, out LevelPlan plan)
        {
            plan = LevelPlan.Invalid;

            var type = _resolver.GetType();
            var byRefPlan = typeof(LevelPlan).MakeByRefType();
            var byRefOptions = typeof(LevelChangeOptions).MakeByRefType();

            // bool TryResolvePlan(string levelId, string reason, out LevelPlan plan)
            var m1 = type.GetMethod("TryResolvePlan", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null, types: new[] { typeof(string), typeof(string), byRefPlan }, modifiers: null);
            if (m1 != null)
            {
                var args = new object?[] { levelId, reason, null };
                var result = m1.Invoke(_resolver, args);
                if (result is bool ok && ok && args[2] is LevelPlan p1)
                {
                    plan = p1;
                    return true;
                }

                return false;
            }

            // bool TryResolvePlan(string levelId, out LevelPlan plan, out LevelChangeOptions options)
            var m2 = type.GetMethod("TryResolvePlan", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null, types: new[] { typeof(string), byRefPlan, byRefOptions }, modifiers: null);
            if (m2 != null)
            {
                var args = new object?[] { levelId, null, null };
                var result = m2.Invoke(_resolver, args);
                if (result is bool ok && ok && args[1] is LevelPlan p2)
                {
                    plan = p2;
                    return true;
                }

                return false;
            }

            // bool TryResolvePlan(string levelId, out LevelPlan plan)
            var m3 = type.GetMethod("TryResolvePlan", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null, types: new[] { typeof(string), byRefPlan }, modifiers: null);
            if (m3 != null)
            {
                var args = new object?[] { levelId, null };
                var result = m3.Invoke(_resolver, args);
                if (result is bool ok && ok && args[1] is LevelPlan p3)
                {
                    plan = p3;
                    return true;
                }

                return false;
            }

            return false;
        }

        private static string GetInitialLevelId(LevelCatalog? catalog)
        {
            if (catalog == null)
            {
                return string.Empty;
            }

            // tenta nomes comuns sem assumir exatamente
            var s = GetStringPropCompat(catalog, "InitialLevelId", "InitialLevelID", "InitialLevel", "Initial");
            return s ?? string.Empty;
        }

        private static string? GetStringPropCompat(object obj, params string[] names)
        {
            var t = obj.GetType();
            foreach (var n in names)
            {
                var p = t.GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p != null && p.PropertyType == typeof(string))
                {
                    return p.GetValue(obj) as string;
                }
            }

            return null;
        }

        private static List<string> GetStringListCompat(object obj, params string[] names)
        {
            var t = obj.GetType();
            foreach (var n in names)
            {
                var p = t.GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p == null)
                {
                    continue;
                }

                var value = p.GetValue(obj);
                if (value is IEnumerable enumerable)
                {
                    var list = new List<string>();
                    foreach (var item in enumerable)
                    {
                        if (item is string s && !string.IsNullOrWhiteSpace(s))
                        {
                            list.Add(s.Trim());
                        }
                    }

                    if (list.Count > 0)
                    {
                        return list;
                    }
                }
            }

            return new List<string>();
        }

        private static List<string> GetDictionaryKeysCompat(object obj, params string[] names)
        {
            var t = obj.GetType();
            foreach (var n in names)
            {
                var p = t.GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p == null)
                {
                    continue;
                }

                var value = p.GetValue(obj);
                if (value is IDictionary dict)
                {
                    var list = new List<string>();
                    foreach (var k in dict.Keys)
                    {
                        if (k is string s && !string.IsNullOrWhiteSpace(s))
                        {
                            list.Add(s.Trim());
                        }
                    }

                    if (list.Count > 0)
                    {
                        return list;
                    }
                }
            }

            return new List<string>();
        }
    }
}
