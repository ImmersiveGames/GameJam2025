using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.UI
{
    public enum ActorAttributeUiClearMode
    {
        KeepCurrent = 0,
        SetZero = 1,
        SetOne = 2,
        DisableImage = 3
    }

    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/Actors/Attributes/Actor Attribute Image Fill Sink")]
    public sealed class ActorAttributeImageFillSink : MonoBehaviour, IActorAttributeUiSink
    {
        private const float FillEpsilon = 0.0001f;

        [Header("References")]
        [Tooltip("Imagem cujo fillAmount sera alterado por este sink.")]
        [SerializeField] private Image fillImage;

        [Header("Clear Behavior")]
        [SerializeField] private ActorAttributeUiClearMode clearMode = ActorAttributeUiClearMode.SetZero;

        public bool IsReady => fillImage != null;

        public Image FillImage => fillImage;
        public ActorAttributeUiClearMode ClearMode => clearMode;

        private void Reset()
        {
            if (fillImage == null)
            {
                fillImage = GetComponent<Image>();
            }
        }

        public void Apply(ActorAttributeUiValue value)
        {
            if (fillImage == null)
            {
                LogApplyRejected("missing_fill_image", value, "fillImage is required.");
                return;
            }

            if (!value.IsValid)
            {
                LogApplyRejected("invalid_value", value, "ActorAttributeUiValue is invalid.");
                return;
            }

            if (!TryResolveFillAmount(value, out float fillAmount, out string rejectionReason))
            {
                LogApplyRejected(rejectionReason, value, "Unable to resolve a fill amount.");
                return;
            }

            fillImage.enabled = true;
            fillImage.fillAmount = fillAmount;

            DebugUtility.LogVerbose(
                typeof(ActorAttributeImageFillSink),
                $"event='ActorAttributeImageFillApplied' attributeId='{value.AttributeId}' currentValue={value.CurrentValue} minValue={value.MinValue} maxValue={value.MaxValue} fillAmount={fillAmount} source='{value.Source}' reason='{value.Reason}'",
                DebugUtility.Colors.Info,
                this);
        }

        public void Clear(string reason)
        {
            if (fillImage == null)
            {
                DebugUtility.LogWarning(
                    typeof(ActorAttributeImageFillSink),
                    $"event='ActorAttributeImageFillApplyRejected' reason='missing_fill_image' clearMode='{clearMode}' source='{nameof(ActorAttributeImageFillSink)}' detail='Clear called without a fill Image.'");
                return;
            }

            string normalizedReason = reason.TrimToEmpty();

            switch (clearMode)
            {
                case ActorAttributeUiClearMode.KeepCurrent:
                    break;
                case ActorAttributeUiClearMode.SetZero:
                    fillImage.enabled = true;
                    fillImage.fillAmount = 0f;
                    break;
                case ActorAttributeUiClearMode.SetOne:
                    fillImage.enabled = true;
                    fillImage.fillAmount = 1f;
                    break;
                case ActorAttributeUiClearMode.DisableImage:
                    fillImage.enabled = false;
                    break;
            }

            DebugUtility.LogVerbose(
                typeof(ActorAttributeImageFillSink),
                $"event='ActorAttributeImageFillCleared' clearMode='{clearMode}' fillAmount={fillImage.fillAmount} source='{nameof(ActorAttributeImageFillSink)}' reason='{normalizedReason}'",
                DebugUtility.Colors.Info,
                this);
        }

        private static bool TryResolveFillAmount(ActorAttributeUiValue value, out float fillAmount, out string rejectionReason)
        {
            fillAmount = 0f;
            rejectionReason = string.Empty;

            if (value.HasNormalizedValue)
            {
                if (!IsFinite(value.NormalizedValue))
                {
                    rejectionReason = "normalized_value_invalid";
                    return false;
                }

                fillAmount = Mathf.Clamp01(value.NormalizedValue);
                return true;
            }

            float denominator = value.MaxValue - value.MinValue;
            if (!IsFinite(denominator) || denominator <= FillEpsilon)
            {
                rejectionReason = "invalid_min_max_range";
                return false;
            }

            float resolved = (value.CurrentValue - value.MinValue) / denominator;
            if (!IsFinite(resolved))
            {
                rejectionReason = "resolved_fill_invalid";
                return false;
            }

            fillAmount = Mathf.Clamp01(resolved);
            return true;
        }

        private void LogApplyRejected(string rejectionReason, ActorAttributeUiValue value, string detail)
        {
            DebugUtility.LogWarning(
                typeof(ActorAttributeImageFillSink),
                $"event='ActorAttributeImageFillApplyRejected' rejectionReason='{rejectionReason}' attributeId='{value.AttributeId}' currentValue={value.CurrentValue} minValue={value.MinValue} maxValue={value.MaxValue} hasNormalizedValue={value.HasNormalizedValue} normalizedValue={value.NormalizedValue} source='{value.Source}' reason='{value.Reason}' detail='{detail}'",
                this);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
}
}
