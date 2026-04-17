using System;
using ImmersiveGames.GameJam2025.Core.Logging;
using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Loading.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Loading.Bindings
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LoadingHudController : MonoBehaviour
    {
        private const string DefaultLabel = "Loading...";

        [Header("References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private TMP_Text loadingText;
        [SerializeField] private TMP_Text progressPercentText;
        [SerializeField] private Image progressFillImage;
        [SerializeField] private GameObject spinnerVisual;
        [SerializeField] private RectTransform spinnerTransform;
        [SerializeField] private float spinnerDegreesPerSecond = 180f;

        private bool _isVisible;
        private string _currentMessage = DefaultLabel;
        private LoadingProgressSnapshot _currentProgress = new(0f, DefaultLabel);

        public Canvas Canvas => canvas;
        public CanvasGroup RootGroup => rootGroup;
        public TMP_Text LoadingText => loadingText;
        public TMP_Text ProgressPercentText => progressPercentText;
        public Image ProgressFillImage => progressFillImage;
        public GameObject SpinnerVisual => spinnerVisual;
        public RectTransform SpinnerTransform => spinnerTransform;

        private void Awake()
        {
            ResolveReferences();
            ValidateConfigurationOrFail();

            SetVisible(false);
            ApplyLabel(null, null);
            ApplyProgress(_currentProgress);

            DebugUtility.LogVerbose<LoadingHudController>(
                $"[OBS][Loading] LoadingHudRoot ready root='{name}' canvas='{canvas.name}' hasText={(loadingText != null)} hasSpinner={(spinnerVisual != null)}.",
                DebugUtility.Colors.Success);
        }

        public void Show(string phase, string message = null)
        {
            SetVisible(true);
            ApplyLabel(phase, message);
        }

        public void Hide(string phase)
        {
            _ = phase;
            SetVisible(false);
        }

        public void SetMessage(string phase, string message = null)
        {
            ApplyLabel(phase, message);
        }

        public void ApplyProgress(LoadingProgressSnapshot snapshot)
        {
            _currentProgress = snapshot;

            if (loadingText != null)
            {
                loadingText.text = snapshot.StepLabel;
            }

            if (progressPercentText != null)
            {
                progressPercentText.text = $"{snapshot.Percentage}%";
            }

            if (progressFillImage != null)
            {
                progressFillImage.fillAmount = snapshot.NormalizedProgress;
            }
        }

        public void EnsureConfiguredOrFail()
        {
            ResolveReferences();
            ValidateConfigurationOrFail();
        }

        private void SetVisible(bool visible)
        {
            if (rootGroup == null || canvas == null)
            {
                FailFast("root_missing", $"root='{name}' canvasConfigured={(canvas != null)} canvasGroupConfigured={(rootGroup != null)}.");
            }

            if (_isVisible == visible)
            {
                return;
            }

            _isVisible = visible;
            rootGroup.alpha = visible ? 1f : 0f;
            rootGroup.interactable = visible;
            rootGroup.blocksRaycasts = visible;
            if (spinnerVisual != null)
            {
                spinnerVisual.SetActive(visible);
            }
        }

        private void ApplyLabel(string phase, string message)
        {
            _ = phase;
            if (loadingText == null)
            {
                return;
            }

            _currentMessage = string.IsNullOrWhiteSpace(message) ? DefaultLabel : message.Trim();
            loadingText.text = _currentProgress.StepLabel ?? _currentMessage;
        }

        private void ResolveReferences()
        {
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
            }

            if (rootGroup == null)
            {
                rootGroup = GetComponent<CanvasGroup>();
            }

            if (spinnerTransform == null && spinnerVisual != null)
            {
                spinnerTransform = spinnerVisual.transform as RectTransform;
            }
        }

        private void ValidateConfigurationOrFail()
        {
            if (canvas == null)
            {
                FailFast("canvas_missing", $"root='{name}' requires Canvas on the same GameObject.");
            }

            if (rootGroup == null)
            {
                FailFast("canvas_group_missing", $"root='{name}' requires CanvasGroup on the same GameObject.");
            }

            if (loadingText == null)
            {
                FailFast("step_text_missing", $"root='{name}' requires a TMP_Text for the current step label.");
            }

            if (progressPercentText == null)
            {
                FailFast("percentage_text_missing", $"root='{name}' requires a TMP_Text for the percentage label.");
            }

            if (progressFillImage == null)
            {
                FailFast("progress_fill_missing", $"root='{name}' requires an Image for the progress fill.");
            }

            if (spinnerVisual == null)
            {
                FailFast("spinner_visual_missing", $"root='{name}' requires a spinner visual GameObject.");
            }

            if (spinnerTransform == null)
            {
                FailFast("spinner_transform_missing", $"root='{name}' requires a RectTransform for spinner rotation.");
            }
        }

        private void Update()
        {
            if (!_isVisible || spinnerTransform == null)
            {
                return;
            }

            spinnerTransform.Rotate(0f, 0f, -spinnerDegreesPerSecond * Time.unscaledDeltaTime);
        }

        private static void FailFast(string reason, string detail)
        {
            string message = $"[FATAL][Loading] LoadingHudRoot invalid. reason='{reason}'. {detail}";
            DebugUtility.LogError<LoadingHudController>(message);
            throw new InvalidOperationException(message);
        }
    }
}

