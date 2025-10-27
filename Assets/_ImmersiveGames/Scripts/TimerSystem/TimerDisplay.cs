using _ImmersiveGames.Scripts.TimerSystem.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.Scripts.TimerSystem
{
    /// <summary>
    /// Responsável por exibir o tempo restante na UI com formatação 00:00
    /// e preenchimento opcional de barra.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TimerDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Image timerFillImage;

        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color dangerColor = Color.red;

        [SerializeField] private float warningThreshold = 60f;
        [SerializeField] private float dangerThreshold = 15f;

        private EventBinding<EventTimerStarted> _timerStartedBinding;
        private EventBinding<EventTimeEnded> _timerEndedBinding;

        private GameTimer _gameTimer;
        private float _configuredDuration;
        private string _lastFormattedValue;

        private void Awake()
        {
            ResolveComponents();
            RefreshTimerReference();
            UpdateConfiguredDuration();
            ApplyDisplay(_configuredDuration);

            DebugUtility.Log<TimerDisplay>(
                $"TimerDisplay pronto com duração inicial {_configuredDuration:F2}s.",
                context: this);
        }

        private void OnEnable()
        {
            RefreshTimerReference();
            UpdateConfiguredDuration();
            RegisterEvents();
            ForceRefresh();
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }

        private void Update()
        {
            RefreshTimerReference();
            UpdateConfiguredDuration();
            ApplyDisplay(GetRemainingTime());
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

        private void UnregisterEvents()
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

        private void HandleTimerStarted(EventTimerStarted evt)
        {
            _configuredDuration = Mathf.Max(evt.Duration, 0f);
            DebugUtility.Log<TimerDisplay>(
                $"Evento de início recebido: {_configuredDuration:F2}s.",
                context: this);
            ForceRefresh();
        }

        private void ForceRefresh()
        {
            ApplyDisplay(GetRemainingTime());
        }

        private float GetRemainingTime()
        {
            if (_gameTimer == null)
            {
                return Mathf.Max(_configuredDuration, 0f);
            }

            if (_gameTimer.HasActiveSession)
            {
                return _gameTimer.RemainingTime;
            }

            return Mathf.Max(_gameTimer.ConfiguredDuration, _configuredDuration);
        }

        private void ApplyDisplay(float seconds)
        {
            string formatted = FormatTime(seconds);
            if (timerText != null)
            {
                if (_lastFormattedValue != formatted)
                {
                    _lastFormattedValue = formatted;
                    DebugUtility.Log<TimerDisplay>(
                        $"Display atualizado para {formatted}.",
                        context: this);
                }

                timerText.text = formatted;
            }

            if (timerFillImage == null)
            {
                return;
            }

            float normalized = _configuredDuration > 0f
                ? Mathf.Clamp01(seconds / _configuredDuration)
                : 0f;

            timerFillImage.fillAmount = normalized;

            Color targetColor = normalColor;
            if (seconds <= dangerThreshold)
            {
                targetColor = dangerColor;
            }
            else if (seconds <= warningThreshold)
            {
                targetColor = warningColor;
            }

            timerFillImage.color = targetColor;
        }

        private void RefreshTimerReference()
        {
            if (_gameTimer == null)
            {
                _gameTimer = GameTimer.Instance;
            }
        }

        private void UpdateConfiguredDuration()
        {
            if (_gameTimer == null)
            {
                return;
            }

            _configuredDuration = Mathf.Max(_gameTimer.ConfiguredDuration, 0f);
        }

        private void ResolveComponents()
        {
            if (timerText == null)
            {
                timerText = GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (timerFillImage == null)
            {
                timerFillImage = GetComponentInChildren<Image>(true);
            }
        }

        private static string FormatTime(float seconds)
        {
            float clamped = Mathf.Max(seconds, 0f);
            int minutes = Mathf.FloorToInt(clamped / 60f);
            int secs = Mathf.FloorToInt(clamped % 60f);
            return $"{minutes:00}:{secs:00}";
        }
    }
}
