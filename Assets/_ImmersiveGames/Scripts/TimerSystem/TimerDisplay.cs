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

        [Header("Fill Settings")]
        [SerializeField] private bool useFillColorStates = true;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color dangerColor = Color.red;

        [Header("Text Settings")]
        [SerializeField] private bool useTextColorStates;
        [SerializeField] private Color textNormalColor = Color.white;
        [SerializeField] private Color textWarningColor = Color.yellow;
        [SerializeField] private Color textDangerColor = Color.red;

        [SerializeField] private float warningThreshold = 60f;
        [SerializeField] private float dangerThreshold = 15f;

        private EventBinding<EventTimerStarted> _timerStartedBinding;
        private EventBinding<EventTimeEnded> _timerEndedBinding;

        private GameTimer _gameTimer;
        private float _configuredDuration;
        private string _lastFormattedValue;
        private Color _initialFillColor;
        private Color _initialTextColor;
        private bool _capturedDefaults;

        private void Awake()
        {
            ResolveComponents();
            RefreshTimerReference();
            UpdateConfiguredDuration();
            CaptureDefaultColors();
            ApplyDisplay(_configuredDuration);

            DebugUtility.LogVerbose<TimerDisplay>(
                $"TimerDisplay pronto com duração inicial {_configuredDuration:F2}s.",
                context: this);
        }

        private void OnEnable()
        {
            RefreshTimerReference();
            UpdateConfiguredDuration();
            CaptureDefaultColors();
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
            DebugUtility.LogVerbose<TimerDisplay>(
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

            return _gameTimer.RemainingTime;
        }

        private void ApplyDisplay(float seconds)
        {
            string formatted = FormatTime(seconds);
            if (timerText != null)
            {
                if (_lastFormattedValue != formatted)
                {
                    _lastFormattedValue = formatted;
                    DebugUtility.LogVerbose<TimerDisplay>(
                        $"Display atualizado para {formatted}.",
                        context: this);
                }

                timerText.text = formatted;

                if (useTextColorStates)
                {
                    timerText.color = EvaluateColor(seconds, textNormalColor, textWarningColor, textDangerColor);
                }
                else if (_capturedDefaults)
                {
                    timerText.color = _initialTextColor;
                }
            }

            if (timerFillImage == null)
            {
                return;
            }

            float normalized = _configuredDuration > 0f
                ? Mathf.Clamp01(seconds / _configuredDuration)
                : 0f;

            timerFillImage.fillAmount = normalized;

            if (useFillColorStates)
            {
                timerFillImage.color = EvaluateColor(seconds, normalColor, warningColor, dangerColor);
            }
            else if (_capturedDefaults)
            {
                timerFillImage.color = _initialFillColor;
            }
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

        private void CaptureDefaultColors()
        {
            if (_capturedDefaults)
            {
                return;
            }

            if (timerFillImage != null)
            {
                _initialFillColor = timerFillImage.color;
            }

            if (timerText != null)
            {
                _initialTextColor = timerText.color;
            }

            _capturedDefaults = true;
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

        private Color EvaluateColor(float seconds, Color normal, Color warning, Color danger)
        {
            if (seconds <= dangerThreshold)
            {
                return danger;
            }

            if (seconds <= warningThreshold)
            {
                return warning;
            }

            return normal;
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
