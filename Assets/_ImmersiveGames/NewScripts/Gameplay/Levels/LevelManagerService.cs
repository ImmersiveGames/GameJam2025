#nullable enable
using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.ContentSwap;
using _ImmersiveGames.NewScripts.Gameplay.Scene;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Catalogs;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Definitions;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Providers;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Resolvers;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LevelManagerService : ILevelManagerService, IDisposable
    {
        private const string LevelChangePrefix = "LevelChange/";
        private const string QaLevelPrefix = "QA/Level/";
        private const string QaLevelsPrefix = "QA/Levels/";
        private const string DefaultGameplaySceneName = "GameplayScene";

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
            var normalizedReason = NormalizeApplyReason(reason);

            if (!_selectedPlan.IsValid && !TryAutoSelectInitial(normalizedReason))
            {
                LogApplySelectionInvalid(normalizedReason);
                return;
            }

            if (!IsGameplaySceneActive())
            {
                DebugUtility.LogWarning<LevelManagerService>(
                    $"[LevelManager] ApplySelectedLevel ignorado (fora da gameplay). scene='{SceneManager.GetActiveScene().name}' reason='{normalizedReason}'.");
                return;
            }

            await _levelManager.RequestLevelInPlaceAsync(_selectedPlan, normalizedReason, _selectedOptions);
            UpdateCurrentFromPlan(_selectedPlan, normalizedReason, logApplied: true);
        }

        public async Task ApplyLevelAsync(string levelId, string reason)
        {
            if (!_resolver.TryResolvePlan(levelId, out var plan, out var options))
            {
                DebugUtility.LogWarning<LevelManagerService>(
                    $"[LevelManager] ApplyLevelAsync falhou ao resolver plano. levelId='{Sanitize(levelId)}'.");
                return;
            }

            ApplySelection(plan, options, reason);
            await ApplySelectedLevelAsync(reason);
        }

        public void ClearSelection(string reason)
        {
            _selectedPlan = LevelPlan.None;
            _selectedOptions = LevelChangeOptions.Default.Clone();
            _selectedReason = string.Empty;

            DebugUtility.Log(typeof(LevelManagerService),
                $"[OBS][LevelManager] LevelSelectionCleared reason='{NormalizeReason(reason)}'.",
                DebugUtility.Colors.Info);
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
            var current = BuildSnapshot(_currentPlan, _currentContentId, _currentContentSignature);
            var selected = BuildSnapshot(_selectedPlan, string.Empty, string.Empty);

            DebugUtility.Log(typeof(LevelManagerService),
                $"[OBS][LevelManager] StatusSnapshot current={{levelId='{current.LevelId}', contentId='{current.ContentId}', contentSig='{current.ContentSignature}'}} " +
                $"selected={{levelId='{selected.LevelId}', contentId='{selected.ContentId}', contentSig='{selected.ContentSignature}'}} reason='{NormalizeReason(reason)}'.",
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

        private string NormalizeApplyReason(string reason)
        {
            var normalized = NormalizeReason(reason);
            if (normalized == "n/a" && !string.IsNullOrWhiteSpace(_selectedReason))
            {
                return _selectedReason;
            }

            return normalized;
        }

        private bool TryAutoSelectInitial(string reason)
        {
            if (!_resolver.TryResolveInitialPlan(out var plan, out var options))
            {
                return false;
            }

            ApplySelection(plan, options, reason);

            DebugUtility.Log(typeof(LevelManagerService),
                $"[LevelManager] Seleção inválida; auto-select aplicou nível inicial levelId='{plan.LevelId}' reason='{NormalizeReason(reason)}'.",
                DebugUtility.Colors.Warning);
            return true;
        }

        private void LogApplySelectionInvalid(string reason)
        {
            var catalog = _catalogProvider.GetCatalog();
            if (catalog == null)
            {
                DebugUtility.LogWarning<LevelManagerService>(
                    $"[LevelManager] ApplySelectedLevel ignorado (catálogo ausente). reason='{NormalizeReason(reason)}'.");
                return;
            }

            var definitionsCount = catalog.Definitions?.Count ?? 0;
            var orderedCount = catalog.OrderedLevels?.Count ?? 0;
            DebugUtility.LogWarning<LevelManagerService>(
                $"[LevelManager] ApplySelectedLevel ignorado (selection inválida, catálogo sem nível inicial/defs). " +
                $"definitions='{definitionsCount}' ordered='{orderedCount}' reason='{NormalizeReason(reason)}'.");
        }

        private static bool IsGameplaySceneActive()
        {
            if (DependencyManager.Provider != null
                && DependencyManager.Provider.TryGetGlobal<IGameplaySceneClassifier>(out var classifier)
                && classifier != null)
            {
                return classifier.IsGameplayScene();
            }

            return SceneManager.GetActiveScene().name == DefaultGameplaySceneName;
        }

        private static Snapshot BuildSnapshot(LevelPlan plan, string contentIdOverride, string signatureOverride)
        {
            var levelId = plan.IsValid ? plan.LevelId : "<none>";
            var contentId = string.IsNullOrWhiteSpace(contentIdOverride)
                ? (plan.IsValid ? plan.ContentId : "<none>")
                : contentIdOverride;
            var signature = string.IsNullOrWhiteSpace(signatureOverride)
                ? (plan.IsValid ? plan.ContentSignature : "<none>")
                : signatureOverride;

            return new Snapshot(levelId, contentId, signature);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string Sanitize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "n/a" : value.Replace("\n", " ").Replace("\r", " ").Trim();
        }

        private readonly struct Snapshot
        {
            public string LevelId { get; }
            public string ContentId { get; }
            public string ContentSignature { get; }

            public Snapshot(string levelId, string contentId, string contentSignature)
            {
                LevelId = levelId;
                ContentId = contentId;
                ContentSignature = contentSignature;
            }
        }
    }
}
