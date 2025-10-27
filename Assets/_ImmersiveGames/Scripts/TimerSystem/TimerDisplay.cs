using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.TimerSystem.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace _ImmersiveGames.Scripts.TimerSystem
{
    [DisallowMultipleComponent]
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

        private void Awake()
        {
            TryResolveTimerText();
            TryResolveFillImage();
            EnsureGameTimerReference();
            SyncInitialDuration();
            UpdateTimerDisplay();
        }

        private void OnEnable()
        {
            EnsureGameTimerReference();
            SyncInitialDuration();
            RegisterTimerEvents();
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
            if (_gameTimer == null)
            {
                EnsureGameTimerReference();
                SyncInitialDuration();
                RegisterTimerEvents();
            }

            UpdateTimerDisplay();
        }

        private void RegisterTimerEvents()
        {
            if (_gameTimer == null)
            {
                return;
            }

            bool registered = false;

            if (_timerStartedBinding == null)
            {
                _timerStartedBinding = new EventBinding<EventTimerStarted>(HandleTimerStarted);
                EventBus<EventTimerStarted>.Register(_timerStartedBinding);
                registered = true;
            }

            if (_timerEndedBinding == null)
            {
                _timerEndedBinding = new EventBinding<EventTimeEnded>(HandleTimerEnded);
                EventBus<EventTimeEnded>.Register(_timerEndedBinding);
                registered = true;
            }

            if (registered)
            {
                DebugUtility.LogVerbose<TimerDisplay>("UI: Timer monitorando eventos do sistema.");
            }
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
            float remainingTime = ResolveRemainingTime();

            if (timerText != null)
            {
                timerText.text = FormatTime(remainingTime);
            }

            if (timerFillImage == null)
            {
                return;
            }

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

            if (remainingTime <= 0f && _gameTimer != null && _gameTimer.HasActiveSession)
            {
                DebugUtility.LogVerbose<TimerDisplay>("UI: Tempo esgotado!");
            }
        }

        private float ResolveRemainingTime()
        {
            if (_gameTimer == null)
            {
                return Mathf.Max(_initialDuration, 0f);
            }

            if (_initialDuration <= 0f)
            {
                _initialDuration = Mathf.Max(_gameTimer.ConfiguredDuration, 0f);
            }

            float remaining = _gameTimer.RemainingTime;

            if (!_gameTimer.HasActiveSession && remaining <= 0f)
            {
                return Mathf.Max(_initialDuration, 0f);
            }

            return remaining;
        }

        private void EnsureGameTimerReference()
        {
            if (_gameTimer == null)
            {
                _gameTimer = GameTimer.Instance;
            }
        }

        private void SyncInitialDuration()
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

        private void TryResolveTimerText()
        {
            if (timerText == null)
            {
                timerText = GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        private void TryResolveFillImage()
        {
            if (timerFillImage == null)
            {
                timerFillImage = GetComponentInChildren<Image>(true);
            }
        }

        private static string FormatTime(float timeRemaining)
        {
            float clamped = Mathf.Max(timeRemaining, 0f);
            int minutes = Mathf.FloorToInt(clamped / 60f);
            int seconds = Mathf.FloorToInt(clamped % 60f);
            return $"{minutes:00}:{seconds:00}";
        }
    }
}
