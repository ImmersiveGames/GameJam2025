#nullable enable
using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.ContentSwap;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Catalogs;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Definitions;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Providers;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Resolvers;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LevelManagerService : ILevelManagerService, IDisposable
    {
        private const string LevelChangePrefix = "LevelChange/";
        private const string QaLevelPrefix = "QA/Level/";
        private const string QaLevelsPrefix = "QA/Levels/";

        private readonly ILevelManager _levelManager;
        private readonly ILevelCatalogResolver _resolver;
        private readonly ILevelCatalogProvider _catalogProvider;

        private LevelPlan _selectedPlan = LevelPlan.None;
        private LevelChangeOptions _selectedOptions = LevelChangeOptions.Default.Clone();
        private string _selectedReason = string.Empty;

        private LevelPlan _currentPlan = LevelPlan.None;
        private string _currentContentId = string.Empty;
        private string _currentContentSignature = string.Empty;
        private string _currentReason = string.Empty;

        public LevelManagerService(ILevelManager levelManager, ILevelCatalogResolver resolver, ILevelCatalogProvider catalogProvider)
        {
            _levelManager = levelManager ?? throw new ArgumentNullException(nameof(levelManager));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _catalogProvider = catalogProvider ?? throw new ArgumentNullException(nameof(catalogProvider));

            DebugUtility.Log(typeof(LevelManagerService), "[LevelManager] Registered (no bootstrap)", DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
        }

        public bool SelectLevel(string levelId, string reason)
        {
            if (!_resolver.TryResolvePlan(levelId, out var plan, out var options))
            {
                DebugUtility.LogWarning<LevelManagerService>($"[LevelManager] SelectLevel falhou. levelId='{Sanitize(levelId)}'.");
                return false;
            }

            ApplySelection(plan, options, reason);
            return true;
        }

        public bool SelectInitialLevel(string reason)
        {
            if (!_resolver.TryResolveInitialPlan(out var plan, out var options))
            {
                DebugUtility.LogWarning<LevelManagerService>("[LevelManager] SelectInitialLevel falhou ao resolver plano inicial.");
                return false;
            }

            ApplySelection(plan, options, reason);
            return true;
        }

        public bool SelectNextLevel(string reason)
        {
            var baseLevelId = GetReferenceLevelId();
            if (string.IsNullOrWhiteSpace(baseLevelId))
            {
                DebugUtility.LogWarning<LevelManagerService>("[LevelManager] SelectNextLevel sem nível de referência (current/selected)." );
                return false;
            }

            if (!_resolver.TryResolveNextPlan(baseLevelId, out var plan, out var options))
            {
                DebugUtility.LogWarning<LevelManagerService>($"[LevelManager] SelectNextLevel falhou. levelId='{baseLevelId}'.");
                return false;
            }

            ApplySelection(plan, options, reason);
            return true;
        }

        public bool SelectPreviousLevel(string reason)
        {
            var baseLevelId = GetReferenceLevelId();
            if (string.IsNullOrWhiteSpace(baseLevelId))
            {
                DebugUtility.LogWarning<LevelManagerService>("[LevelManager] SelectPreviousLevel sem nível de referência (current/selected)." );
                return false;
            }

            if (!TryResolvePreviousLevelId(baseLevelId, out var previousLevelId))
            {
                DebugUtility.LogWarning<LevelManagerService>($"[LevelManager] SelectPreviousLevel falhou. levelId='{baseLevelId}'.");
                return false;
            }

            if (!_resolver.TryResolvePlan(previousLevelId, out var plan, out var options))
            {
                DebugUtility.LogWarning<LevelManagerService>($"[LevelManager] SelectPreviousLevel sem plano válido. levelId='{previousLevelId}'.");
                return false;
            }

            ApplySelection(plan, options, reason);
            return true;
        }

        public async Task ApplySelectedLevelAsync(string reason)
        {
            if (!_selectedPlan.IsValid)
            {
                DebugUtility.LogWarning<LevelManagerService>("[LevelManager] ApplySelectedLevel ignorado (selection inválida)." );
                return;
            }

            var normalizedReason = NormalizeReason(reason);
            await _levelManager.RequestLevelInPlaceAsync(_selectedPlan, normalizedReason, _selectedOptions);
            UpdateCurrentFromPlan(_selectedPlan, normalizedReason, logApplied: true);
        }

        public void NotifyContentSwapCommitted(ContentSwapPlan plan, string reason)
        {
            if (!plan.IsValid)
            {
                return;
            }

            _currentContentId = plan.ContentId;
            _currentContentSignature = plan.ContentSignature;
            _currentReason = Sanitize(reason);

            var levelId = "<unmapped>";
            if (TryResolveDefinitionByContentId(plan.ContentId, out var definition))
            {
                _currentPlan = definition.ToPlan();
                levelId = _currentPlan.LevelId;
            }
            else
            {
                _currentPlan = LevelPlan.None;
            }

            if (IsLevelReason(reason))
            {
                return;
            }

            DebugUtility.Log(typeof(LevelManagerService),
                $"[OBS][LevelManager] LevelChangedFromContentSwap contentId='{plan.ContentId}' levelId='{levelId}' reason='{NormalizeReason(reason)}' contentSig='{Sanitize(plan.ContentSignature)}'.",
                DebugUtility.Colors.Info);
        }

        public void DumpCurrent(string reason)
        {
            var levelId = _currentPlan.IsValid ? _currentPlan.LevelId : "<none>";
            var contentId = string.IsNullOrWhiteSpace(_currentContentId)
                ? (_currentPlan.IsValid ? _currentPlan.ContentId : "<none>")
                : _currentContentId;
            var signature = string.IsNullOrWhiteSpace(_currentContentSignature)
                ? (_currentPlan.IsValid ? _currentPlan.ContentSignature : "<none>")
                : _currentContentSignature;

            DebugUtility.Log(typeof(LevelManagerService),
                $"[OBS][LevelManager] CurrentSnapshot levelId='{levelId}' contentId='{contentId}' contentSig='{signature}' reason='{NormalizeReason(reason)}'.",
                DebugUtility.Colors.Info);
        }

        private void ApplySelection(LevelPlan plan, LevelChangeOptions options, string reason)
        {
            _selectedPlan = plan;
            _selectedOptions = options?.Clone() ?? LevelChangeOptions.Default.Clone();
            _selectedReason = Sanitize(reason);

            DebugUtility.Log(typeof(LevelManagerService),
                $"[OBS][LevelManager] LevelSelected levelId='{plan.LevelId}' contentId='{plan.ContentId}' reason='{NormalizeReason(reason)}' contentSig='{plan.ContentSignature}'.",
                DebugUtility.Colors.Info);
        }

        private void UpdateCurrentFromPlan(LevelPlan plan, string reason, bool logApplied)
        {
            _currentPlan = plan;
            _currentContentId = plan.ContentId;
            _currentContentSignature = plan.ContentSignature;
            _currentReason = Sanitize(reason);

            if (!logApplied)
            {
                return;
            }

            DebugUtility.Log(typeof(LevelManagerService),
                $"[OBS][LevelManager] LevelApplied levelId='{plan.LevelId}' reason='{NormalizeReason(reason)}' contentId='{plan.ContentId}' contentSig='{plan.ContentSignature}'.",
                DebugUtility.Colors.Info);
        }

        private bool TryResolveDefinitionByContentId(string contentId, out LevelDefinition definition)
        {
            definition = null;
            var catalog = _catalogProvider.GetCatalog();
            if (catalog == null)
            {
                return false;
            }

            foreach (var def in catalog.Definitions)
            {
                if (def == null)
                {
                    continue;
                }

                if (string.Equals(def.ContentId, Normalize(contentId), StringComparison.Ordinal))
                {
                    definition = def;
                    return true;
                }
            }

            return false;
        }

        private bool TryResolvePreviousLevelId(string levelId, out string previousLevelId)
        {
            previousLevelId = string.Empty;
            var catalog = _catalogProvider.GetCatalog();
            if (catalog == null)
            {
                return false;
            }

            if (TryResolvePreviousFromList(catalog.OrderedLevels, levelId, out previousLevelId))
            {
                return true;
            }

            var fallback = ExtractDefinitionOrder(catalog);
            return TryResolvePreviousFromList(fallback, levelId, out previousLevelId);
        }

        private static bool TryResolvePreviousFromList(System.Collections.Generic.IReadOnlyList<string> list, string levelId, out string previousLevelId)
        {
            previousLevelId = string.Empty;
            if (list == null || list.Count == 0)
            {
                return false;
            }

            var normalized = Normalize(levelId);
            for (int index = 0; index < list.Count; index++)
            {
                var entry = Normalize(list[index]);
                if (!string.Equals(entry, normalized, StringComparison.Ordinal))
                {
                    continue;
                }

                if (index - 1 < 0)
                {
                    return false;
                }

                previousLevelId = Normalize(list[index - 1]);
                return previousLevelId.Length > 0;
            }

            return false;
        }

        private static System.Collections.Generic.List<string> ExtractDefinitionOrder(LevelCatalog catalog)
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (var def in catalog.Definitions)
            {
                if (def == null)
                {
                    continue;
                }

                var id = Normalize(def.LevelId);
                if (id.Length > 0)
                {
                    list.Add(id);
                }
            }

            return list;
        }

        private string GetReferenceLevelId()
        {
            if (_currentPlan.IsValid)
            {
                return _currentPlan.LevelId;
            }

            if (_selectedPlan.IsValid)
            {
                return _selectedPlan.LevelId;
            }

            return string.Empty;
        }

        private static bool IsLevelReason(string reason)
        {
            var normalized = NormalizeReason(reason);
            return normalized.StartsWith(LevelChangePrefix, StringComparison.Ordinal)
                   || normalized.StartsWith(QaLevelPrefix, StringComparison.Ordinal)
                   || normalized.StartsWith(QaLevelsPrefix, StringComparison.Ordinal);
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "n/a" : reason.Trim();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string Sanitize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "n/a" : value.Replace("\n", " ").Replace("\r", " ").Trim();
        }
    }
}
