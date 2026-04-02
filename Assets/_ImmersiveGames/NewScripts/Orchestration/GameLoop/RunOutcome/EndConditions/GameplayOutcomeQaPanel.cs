using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunOutcome.EndConditions
{
    [DisallowMultipleComponent]
    public sealed class GameplayOutcomeQaPanel : MonoBehaviour
    {
        private const string VictoryReason = "QA/BaselineV3/VictoryButton";
        private const string DefeatReason = "QA/BaselineV3/DefeatButton";

        [Header("Layout")]
        [SerializeField] private Rect panelRect = new(16f, 16f, 260f, 120f);
        [SerializeField] private string title = "Baseline V3 Outcome Mock";

        [Inject] private IGameRunEndRequestService _endRequest;
        [Inject] private IGameLoopService _gameLoopService;

        private EventBinding<GameRunStartedEvent> _runStartedBinding;
        private EventBinding<GameRunEndedEvent> _runEndedBinding;
        private bool _registered;
        private bool _runEnded;

        private void Awake()
        {
            _runStartedBinding = new EventBinding<GameRunStartedEvent>(_ => _runEnded = false);
            _runEndedBinding = new EventBinding<GameRunEndedEvent>(_ => _runEnded = true);
        }

        private void OnEnable()
        {
            EnsureDependenciesInjected();
            RegisterBindings();
        }

        private void OnDisable()
        {
            UnregisterBindings();
        }

        private void OnDestroy()
        {
            UnregisterBindings();
        }

        private void OnGUI()
        {
            if (!ShouldShow())
            {
                return;
            }

            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.Label(title);

            if (GUILayout.Button("Victory"))
            {
                RequestOutcome(GameRunOutcome.Victory, VictoryReason);
            }

            if (GUILayout.Button("Defeat"))
            {
                RequestOutcome(GameRunOutcome.Defeat, DefeatReason);
            }

            GUILayout.EndArea();
        }

        private bool ShouldShow()
        {
            EnsureDependenciesInjected();

            if (_endRequest == null || _gameLoopService == null)
            {
                return false;
            }

            if (_runEnded)
            {
                return false;
            }

            return string.Equals(_gameLoopService.CurrentStateIdName, nameof(GameLoopStateId.Playing), System.StringComparison.Ordinal);
        }

        private void RequestOutcome(GameRunOutcome outcome, string reason)
        {
            EnsureDependenciesInjected();
            if (_endRequest == null)
            {
                DebugUtility.LogWarning<GameplayOutcomeQaPanel>(
                    $"[QA][BaselineV3] Outcome mock ignored: IGameRunEndRequestService unavailable. outcome='{outcome}'.",
                    this);
                return;
            }

            DebugUtility.Log<GameplayOutcomeQaPanel>(
                $"[QA][BaselineV3] Outcome mock requested. outcome='{outcome}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            _endRequest.RequestRunEnd(outcome, reason);
        }

        private void EnsureDependenciesInjected()
        {
            if (!DependencyManager.HasInstance)
            {
                return;
            }

            if (_endRequest == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _endRequest);
            }

            if (_gameLoopService == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _gameLoopService);
            }
        }

        private void RegisterBindings()
        {
            if (_registered)
            {
                return;
            }

            EventBus<GameRunStartedEvent>.Register(_runStartedBinding);
            EventBus<GameRunEndedEvent>.Register(_runEndedBinding);
            _registered = true;
        }

        private void UnregisterBindings()
        {
            if (!_registered)
            {
                return;
            }

            EventBus<GameRunStartedEvent>.Unregister(_runStartedBinding);
            EventBus<GameRunEndedEvent>.Unregister(_runEndedBinding);
            _registered = false;
        }
    }
}
