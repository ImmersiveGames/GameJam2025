using _ImmersiveGames.Scripts.TimerSystem.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace _ImmersiveGames.Scripts.TimerSystem
{
    public class TimerDisplay : MonoBehaviour
    {
        private GameTimer _gameTimer;
        [SerializeField] private TextMeshProUGUI timerText;

        [SerializeField] private Image timerFillImage;

        [SerializeField] private Color normalColor = Color.green;

        [SerializeField] private Color warningColor = Color.yellow;

        [SerializeField] private Color dangerColor = Color.red;

        [SerializeField] private float warningThreshold = 120f; // 2 minutos

        [SerializeField] private float dangerThreshold = 30f;  // 30 segundos

        private float _initialDuration = 300f; // 5 minutos
        private EventBinding<EventTimerStarted> _timerStartedBinding;
        private EventBinding<EventTimeEnded> _timerEndedBinding;

        private void OnEnable()
        {
            if (_gameTimer == null)
            {
                _gameTimer = GameTimer.Instance;
            }

            if (_gameTimer != null)
            {
                _initialDuration = Mathf.Max(_gameTimer.ConfiguredDuration, 0f);
                RegisterTimerEvents();
            }

            UpdateTimerDisplay();
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
            UpdateTimerDisplay();
        }

        private void RegisterTimerEvents()
        {
            if (_timerStartedBinding != null || _timerEndedBinding != null)
            {
                return;
            }

            _timerStartedBinding = new EventBinding<EventTimerStarted>(HandleTimerStarted);
            EventBus<EventTimerStarted>.Register(_timerStartedBinding);

            _timerEndedBinding = new EventBinding<EventTimeEnded>(HandleTimerEnded);
            EventBus<EventTimeEnded>.Register(_timerEndedBinding);

            DebugUtility.LogVerbose<TimerDisplay>("UI: Timer inicializado!");
        }

        private void HandleTimerStarted(EventTimerStarted evt)
        {
            _initialDuration = Mathf.Max(evt.Duration, 0f);
            UpdateTimerDisplay();
        }

        private void HandleTimerEnded(EventTimeEnded _)
        {
            UpdateTimerDisplay();
        }

        private void UpdateTimerDisplay()
        {
            if (_gameTimer == null)
            {
                return;
            }

            if (timerText != null)
            {
                timerText.text = _gameTimer.GetFormattedTime();
            }

            if (timerFillImage == null)
            {
                return;
            }

            float remainingTime = _gameTimer.RemainingTime;
            float normalizedTime = _initialDuration > 0f ? remainingTime / _initialDuration : 0f;
            normalizedTime = Mathf.Clamp01(normalizedTime);
            timerFillImage.fillAmount = normalizedTime;

            if (remainingTime <= dangerThreshold)
            {
                timerFillImage.color = dangerColor;
            }
            else if (remainingTime <= warningThreshold)
            {
                timerFillImage.color = warningColor;
            }
            else
            {
                timerFillImage.color = normalColor;
            }

            if (remainingTime <= 0f)
            {
                DebugUtility.LogVerbose<TimerDisplay>("UI: Tempo esgotado!");
            }
        }
    }
}
