using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.StateMachineSystems.GameStates;
using _ImmersiveGames.Scripts.TimerSystem.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.Scripts.TimerSystem
{
    /// <summary>
    /// Atualiza o texto e a barra do cronômetro conforme os eventos do GameTimer.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TimerDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Image timerFillImage;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color warningColor = new Color(1f, 0.8f, 0f);
        [SerializeField] private Color dangerColor = Color.red;
        [SerializeField] private float warningThreshold = 60f;
        [SerializeField] private float dangerThreshold = 20f;

        private GameTimer _gameTimer;
        private float _initialDuration;
        private string _lastFormatted;

        private EventBinding<EventTimerStarted> _timerStartedBinding;
        private EventBinding<EventTimeEnded> _timerEndedBinding;

        private void Awake()
        {
            ResolveReferences();
            CaptureInitialDuration();
            ApplyDisplay(GetDisplayedTime());
            DebugUtility.Log<TimerDisplay>($"TimerDisplay configurado com {_initialDuration:F2}s iniciais.", context: this);
        }

        private void OnEnable()
        {
            ResolveReferences();
            RegisterEvents();
            CaptureInitialDuration();
            ForceRefresh();
        }

        private void OnDisable()
        {
            if (_timerStartedBinding != null)
            {
                EventBus<EventTimerStarted>.Unregister(_timerStartedBinding);
                _timerStartedBinding = null;
            }

            if (_timerEndedBinding != null)
            {
                EventBus<EventTimeEnded>.Unregister(_timerEndedBinding);
                _timerEndedBinding = null;
            }
        }

        private void Update()
        {
            ResolveReferences();
            ApplyDisplay(GetDisplayedTime());
        }

        private void RegisterEvents()
        {
            if (_timerStartedBinding == null)
            {
                _timerStartedBinding = new EventBinding<EventTimerStarted>(HandleTimerStarted);
                EventBus<EventTimerStarted>.Register(_timerStartedBinding);
            }

            if (_timerEndedBinding == null)
            {
                _timerEndedBinding = new EventBinding<EventTimeEnded>(_ => ForceRefresh());
                EventBus<EventTimeEnded>.Register(_timerEndedBinding);
            }
        }

        private void HandleTimerStarted(EventTimerStarted evt)
        {
            _initialDuration = Mathf.Max(evt.Duration, 0f);
            DebugUtility.Log<TimerDisplay>($"Evento de início recebido: {_initialDuration:F2}s.", context: this);
            ForceRefresh();
        }

        private void ForceRefresh()
        {
            _lastFormatted = null;
            ApplyDisplay(GetDisplayedTime());
        }

        private void ResolveReferences()
        {
            if (_gameTimer == null)
            {
                _gameTimer = GameTimer.Instance;
            }

            if (timerText == null)
            {
                timerText = GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (timerFillImage == null)
            {
                timerFillImage = GetComponentInChildren<Image>(true);
            }
        }

        private void CaptureInitialDuration()
        {
            if (_gameTimer != null)
            {
                _initialDuration = Mathf.Max(_gameTimer.ConfiguredDuration, 0f);
                return;
            }

            GameManager manager = GameManager.Instance;
            if (manager != null && manager.GameConfig != null)
            {
                _initialDuration = Mathf.Max(manager.GameConfig.timerGame, 0f);
            }
        }

        private float GetDisplayedTime()
        {
            if (_gameTimer == null)
            {
                return Mathf.Max(_initialDuration, 0f);
            }

            if (_gameTimer.HasActiveSession)
            {
                return _gameTimer.RemainingTime;
            }

            var currentState = GameManagerStateMachine.Instance?.CurrentState;
            if (currentState is MenuState)
            {
                return Mathf.Max(_initialDuration, 0f);
            }

            return Mathf.Max(_gameTimer.RemainingTime, 0f);
        }

        private void ApplyDisplay(float seconds)
        {
            float clamped = Mathf.Max(seconds, 0f);
            string formatted = FormatTime(clamped);

            if (timerText != null && _lastFormatted != formatted)
            {
                timerText.text = formatted;
                _lastFormatted = formatted;
                DebugUtility.LogVerbose<TimerDisplay>($"UI atualizada para {formatted}.", context: this);
            }

            if (timerFillImage == null)
            {
                return;
            }

            float duration = Mathf.Max(_initialDuration, 0.01f);
            float normalized = Mathf.Clamp01(clamped / duration);
            timerFillImage.fillAmount = normalized;

            if (clamped <= dangerThreshold)
            {
                timerFillImage.color = dangerColor;
            }
            else if (clamped <= warningThreshold)
            {
                timerFillImage.color = warningColor;
            }
            else
            {
                timerFillImage.color = normalColor;
            }
        }

        private static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes:00}:{secs:00}";
        }
    }
}
