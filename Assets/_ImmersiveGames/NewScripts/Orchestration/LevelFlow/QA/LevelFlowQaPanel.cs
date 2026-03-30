using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.QA
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/NewScripts/LevelFlow/QA/Level Flow QA Panel")]
    public sealed class LevelFlowQaPanel : MonoBehaviour
    {
        private const string ResetCurrentReason = "QA/LevelFlow/ResetCurrentLevel";
        private const string RestartFirstReason = "QA/LevelFlow/RestartFromFirstLevel";
        private const string NextReason = "QA/LevelFlow/NextLevel";
        private const string RootName = "__LevelFlowQaPanelPresenter";

        [Header("Layout")]
        [SerializeField] private float margin = 16f;
        [SerializeField] private float panelWidth = 520f;
        [SerializeField] private float panelHeight = 400f;
        [SerializeField] private string title = "Level QA";

        [Inject] private IRestartContextService _restartContextService;
        [Inject] private IPostLevelActionsService _postLevelActionsService;
        [Inject] private IGameLoopService _gameLoopService;
        [Inject] private ISimulationGateService _simulationGateService;

        private bool _dependenciesInjected;
        private volatile bool _actionInFlight;
        private bool _loggedMissingActionsService;
        private static bool _installed;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInstall()
        {
            EnsureInstalled();
        }
#endif

        public static void EnsureInstalled()
        {
            if (_installed || !Application.isPlaying)
            {
                return;
            }

            LevelFlowQaPanel existingPanel = UnityEngine.Object.FindFirstObjectByType<LevelFlowQaPanel>();
            if (existingPanel != null)
            {
                DontDestroyOnLoad(existingPanel.gameObject);
                _installed = true;
                return;
            }

            GameObject existingRoot = GameObject.Find(RootName);
            if (existingRoot != null)
            {
                if (existingRoot.GetComponent<LevelFlowQaPanel>() == null)
                {
                    existingRoot.AddComponent<LevelFlowQaPanel>();
                }

                DontDestroyOnLoad(existingRoot);
                _installed = true;
                return;
            }

            GameObject root = new GameObject(RootName);
            DontDestroyOnLoad(root);
            root.AddComponent<LevelFlowQaPanel>();
            _installed = true;
        }

        private void OnEnable()
        {
            EnsureDependenciesInjected();
        }

        private void OnDisable() { }

        private void OnDestroy() { }

        private void OnGUI()
        {
            if (!TryBuildViewModel(out LevelQaViewModel model))
            {
                return;
            }

            Rect panelRect = BuildPanelRect();

            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.Label(title);
            GUILayout.Label($"currentLevelRef: {model.CurrentLevelRefName}");
            GUILayout.Label($"currentIndex / total: {model.CurrentLevelIndexLabel}");
            GUILayout.Label($"contentId: {model.ContentId}");
            GUILayout.Label($"selectionVersion: {model.SelectionVersion}");
            GUILayout.Label($"levelSignature: {model.LevelSignature}");
            GUILayout.Label($"routeId: {model.RouteId}");
            GUILayout.Label($"nextLevelRef: {model.NextLevelRefName}");
            GUILayout.Label($"nextLevelAvailable: {model.HasNextLevelLabel}");
            GUILayout.Label($"availability: {model.AvailabilityLabel}");

            if (!string.IsNullOrWhiteSpace(model.StatusLine))
            {
                GUILayout.Label(model.StatusLine);
            }

            GUILayout.Space(8f);

            bool previousGuiEnabled = GUI.enabled;

            GUI.enabled = model.CanResetCurrentLevel;
            if (GUILayout.Button("Reset Current Level", GUILayout.ExpandWidth(true), GUILayout.Height(34f)))
            {
                _ = ExecuteActionAsync("ResetCurrentLevel", ResetCurrentReason, (reason, ct) =>
                    _postLevelActionsService.ResetCurrentLevelAsync(reason, ct));
            }

            GUILayout.Space(4f);

            GUI.enabled = model.CanRestartFromFirstLevel;
            if (GUILayout.Button("Restart From First Level", GUILayout.ExpandWidth(true), GUILayout.Height(34f)))
            {
                _ = ExecuteActionAsync("RestartFromFirstLevel", RestartFirstReason, (reason, ct) =>
                    _postLevelActionsService.RestartFromFirstLevelAsync(reason, ct));
            }

            GUILayout.Space(4f);

            GUI.enabled = model.CanNextLevel;
            if (GUILayout.Button("Next Level", GUILayout.ExpandWidth(true), GUILayout.Height(34f)))
            {
                _ = ExecuteActionAsync("NextLevel", NextReason, (reason, ct) =>
                    _postLevelActionsService.NextLevelAsync(reason, ct));
            }

            GUI.enabled = previousGuiEnabled;
            GUILayout.EndArea();
        }

        private Rect BuildPanelRect()
        {
            float maxWidth = Mathf.Max(320f, Screen.width - 308f);
            float width = Mathf.Clamp(panelWidth, 320f, maxWidth);
            float maxHeight = Mathf.Max(260f, Screen.height - 32f);
            float height = Mathf.Min(panelHeight, maxHeight);
            float x = Mathf.Max(margin, Screen.width - width - margin);
            float y = Mathf.Max(margin, margin);
            return new Rect(x, y, width, height);
        }

        private bool TryBuildViewModel(out LevelQaViewModel model)
        {
            model = default;
            EnsureDependenciesInjected();

            if (_restartContextService == null)
            {
                return false;
            }

            if (!_restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot) ||
                !snapshot.IsValid ||
                !snapshot.HasLevelRef ||
                snapshot.MacroRouteRef == null)
            {
                return false;
            }

            LevelCollectionAsset levelCollection = snapshot.MacroRouteRef.LevelCollection;
            int currentLevelIndex = ResolveCurrentLevelIndex(levelCollection, snapshot.LevelRef);
            int totalLevels = levelCollection != null && levelCollection.Levels != null ? levelCollection.Levels.Count : 0;
            bool hasValidCollection = levelCollection != null && levelCollection.TryValidateRuntime(out _);

            string gameplayStateName = _gameLoopService != null
                ? _gameLoopService.CurrentStateIdName ?? string.Empty
                : string.Empty;
            bool isPlaying = string.Equals(gameplayStateName, nameof(GameLoopStateId.Playing), StringComparison.Ordinal);
            bool gateAvailable = _simulationGateService != null;
            bool gateOpen = gateAvailable && _simulationGateService.IsOpen;
            bool hasActionsService = _postLevelActionsService != null;
            bool canInteract = hasActionsService && isPlaying && gateAvailable && gateOpen && !_actionInFlight;

            bool hasNextLevel = hasValidCollection && totalLevels > 0 && currentLevelIndex >= 0;
            string currentLevelIndexLabel = currentLevelIndex >= 0
                ? $"{currentLevelIndex + 1}/{Math.Max(totalLevels, 1)}"
                : totalLevels > 0
                    ? $"n/a/{totalLevels}"
                    : "n/a";

            string nextLevelRefName = ResolveNextLevelName(levelCollection, currentLevelIndex, hasValidCollection);
            string availabilityLabel = canInteract
                ? "ready"
                : BuildUnavailableReason(gameplayStateName, gateAvailable, gateOpen, _actionInFlight);

            string statusLine = hasActionsService
                ? (_actionInFlight ? "status: applying..." : $"status: {availabilityLabel}")
                : "actions: missing";

            model = new LevelQaViewModel(
                snapshot.LevelRef != null ? snapshot.LevelRef.name : "<none>",
                currentLevelIndexLabel,
                totalLevels,
                snapshot.LocalContentId,
                snapshot.SelectionVersion,
                snapshot.LevelSignature,
                snapshot.MacroRouteId.ToString(),
                nextLevelRefName,
                hasNextLevel,
                canInteract,
                canInteract && hasNextLevel,
                availabilityLabel,
                statusLine);

            return true;
        }

        private static int ResolveCurrentLevelIndex(LevelCollectionAsset levelCollection, LevelDefinitionAsset levelRef)
        {
            if (levelCollection == null || levelRef == null || levelCollection.Levels == null)
            {
                return -1;
            }

            for (int i = 0; i < levelCollection.Levels.Count; i++)
            {
                if (ReferenceEquals(levelCollection.Levels[i], levelRef))
                {
                    return i;
                }
            }

            return -1;
        }

        private static string ResolveNextLevelName(LevelCollectionAsset levelCollection, int currentLevelIndex, bool hasValidCollection)
        {
            if (!hasValidCollection ||
                levelCollection == null ||
                levelCollection.Levels == null ||
                levelCollection.Levels.Count == 0 ||
                currentLevelIndex < 0)
            {
                return "<none>";
            }

            int nextIndex = (currentLevelIndex + 1) % levelCollection.Levels.Count;
            LevelDefinitionAsset nextLevel = levelCollection.Levels[nextIndex];
            if (nextLevel == null)
            {
                return "<none>";
            }

            return nextIndex == currentLevelIndex ? $"{nextLevel.name} (wrap)" : nextLevel.name;
        }

        private async Task ExecuteActionAsync(
            string actionName,
            string reason,
            Func<string, CancellationToken, Task> action)
        {
            EnsureDependenciesInjected();

            if (!TryGetActionAvailability(out string availabilityReason))
            {
                DebugUtility.LogWarning<LevelFlowQaPanel>(
                    $"[QA][LevelFlow] {actionName} ignored: unavailable. reason='{reason}'. availability='{availabilityReason}'.");
                return;
            }

            if (_postLevelActionsService == null)
            {
                if (!_loggedMissingActionsService)
                {
                    DebugUtility.LogWarning<LevelFlowQaPanel>(
                        $"[QA][LevelFlow] {actionName} ignored: IPostLevelActionsService unavailable. reason='{reason}'.");
                    _loggedMissingActionsService = true;
                }

                return;
            }

            _actionInFlight = true;

            try
            {
                DebugUtility.Log<LevelFlowQaPanel>(
                    $"[QA][LevelFlow] action='{actionName}' requested reason='{reason}'.",
                    DebugUtility.Colors.Info);

                await action(reason, CancellationToken.None);

                DebugUtility.Log<LevelFlowQaPanel>(
                    $"[QA][LevelFlow] action='{actionName}' applied reason='{reason}'.",
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<LevelFlowQaPanel>(
                    $"[QA][LevelFlow] action='{actionName}' failed reason='{reason}' notes='{ex.GetType().Name}: {ex.Message}'.");
            }
            finally
            {
                _actionInFlight = false;
            }
        }

        private void EnsureDependenciesInjected()
        {
            bool needsRefresh = !_dependenciesInjected ||
                                _restartContextService == null ||
                                _postLevelActionsService == null ||
                                _gameLoopService == null ||
                                _simulationGateService == null;
            if (!needsRefresh)
            {
                return;
            }

            if (!DependencyManager.HasInstance || DependencyManager.Provider == null)
            {
                return;
            }

            DependencyManager.Provider.TryGetGlobal(out _restartContextService);
            DependencyManager.Provider.TryGetGlobal(out _postLevelActionsService);
            DependencyManager.Provider.TryGetGlobal(out _gameLoopService);
            DependencyManager.Provider.TryGetGlobal(out _simulationGateService);
            _dependenciesInjected = _restartContextService != null &&
                                    _postLevelActionsService != null &&
                                    _gameLoopService != null &&
                                    _simulationGateService != null;
        }

        private bool TryGetActionAvailability(out string reason)
        {
            EnsureDependenciesInjected();

            if (_postLevelActionsService == null)
            {
                reason = "post_level_actions_missing";
                return false;
            }

            if (_gameLoopService == null)
            {
                reason = "game_loop_missing";
                return false;
            }

            if (_simulationGateService == null)
            {
                reason = "simulation_gate_missing";
                return false;
            }

            string stateName = _gameLoopService.CurrentStateIdName ?? string.Empty;
            bool isPlaying = string.Equals(stateName, nameof(GameLoopStateId.Playing), StringComparison.Ordinal);
            bool gateOpen = _simulationGateService.IsOpen;

            if (isPlaying && gateOpen && !_actionInFlight)
            {
                reason = "ready";
                return true;
            }

            reason = BuildUnavailableReason(stateName, gateAvailable: true, gateOpen, _actionInFlight);
            return false;
        }

        private static string BuildUnavailableReason(
            string stateName,
            bool gateAvailable,
            bool gateOpen,
            bool actionInFlight)
        {
            if (actionInFlight)
            {
                return "action_in_flight";
            }

            if (!gateAvailable)
            {
                return "simulation_gate_missing";
            }

            if (!gateOpen)
            {
                return "scene_transition_active";
            }

            if (string.IsNullOrWhiteSpace(stateName))
            {
                return "game_loop_unavailable";
            }

            return string.Equals(stateName, nameof(GameLoopStateId.Playing), StringComparison.Ordinal)
                ? "ready"
                : $"game_loop_state:{stateName}";
        }

        private readonly struct LevelQaViewModel
        {
            public LevelQaViewModel(
                string levelRefName,
                string currentLevelIndexLabel,
                int totalLevels,
                string contentId,
                int selectionVersion,
                string levelSignature,
                string routeId,
                string nextLevelRefName,
                bool hasNextLevel,
                bool canResetCurrentLevel,
                bool canRestartFromFirstLevel,
                string availabilityLabel,
                string statusLine)
            {
                CurrentLevelRefName = string.IsNullOrWhiteSpace(levelRefName) ? "<none>" : levelRefName.Trim();
                CurrentLevelIndexLabel = string.IsNullOrWhiteSpace(currentLevelIndexLabel) ? "n/a" : currentLevelIndexLabel.Trim();
                TotalLevels = totalLevels < 0 ? 0 : totalLevels;
                ContentId = string.IsNullOrWhiteSpace(contentId) ? "<none>" : contentId.Trim();
                SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
                LevelSignature = string.IsNullOrWhiteSpace(levelSignature) ? "<none>" : levelSignature.Trim();
                RouteId = string.IsNullOrWhiteSpace(routeId) ? "<none>" : routeId.Trim();
                NextLevelRefName = string.IsNullOrWhiteSpace(nextLevelRefName) ? "<none>" : nextLevelRefName.Trim();
                HasNextLevelLabel = hasNextLevel ? "yes" : "no";
                CanNextLevel = canResetCurrentLevel && hasNextLevel;
                CanResetCurrentLevel = canResetCurrentLevel;
                CanRestartFromFirstLevel = canRestartFromFirstLevel;
                AvailabilityLabel = string.IsNullOrWhiteSpace(availabilityLabel) ? "ready" : availabilityLabel.Trim();
                StatusLine = string.IsNullOrWhiteSpace(statusLine) ? string.Empty : statusLine.Trim();
            }

            public string CurrentLevelRefName { get; }
            public string CurrentLevelIndexLabel { get; }
            public int TotalLevels { get; }
            public string ContentId { get; }
            public int SelectionVersion { get; }
            public string LevelSignature { get; }
            public string RouteId { get; }
            public string NextLevelRefName { get; }
            public string HasNextLevelLabel { get; }
            public string AvailabilityLabel { get; }
            public bool CanNextLevel { get; }
            public bool CanResetCurrentLevel { get; }
            public bool CanRestartFromFirstLevel { get; }
            public string StatusLine { get; }
        }
    }
}
