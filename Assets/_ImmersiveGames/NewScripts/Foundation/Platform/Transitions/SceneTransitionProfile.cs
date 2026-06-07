using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Transitions
{
    /// <summary>
    /// Profile editÃ¡vel (ScriptableObject) para parametrizar a transiÃ§Ã£o no NewScripts.
    /// Neste passo cobre apenas parÃ¢metros de Fade (ADR-0009).
    /// A decisÃ£o de usar ou nÃ£o Fade Ã© responsabilidade de TransitionStyle/request, nÃ£o do Profile.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SceneTransitionProfile",
        menuName = "ImmersiveGames/Transitions/SceneTransitionProfile",
        order = 30)]
    public sealed class SceneTransitionProfile : ScriptableObject
    {
        [Header("Fade")]
        [Min(0f)]
        [SerializeField] private float fadeInDuration = 0.5f;

        [Min(0f)]
        [SerializeField] private float fadeOutDuration = 0.5f;

        [SerializeField] private AnimationCurve fadeInCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [SerializeField] private AnimationCurve fadeOutCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public float FadeInDuration => fadeInDuration;
        public float FadeOutDuration => fadeOutDuration;
        public AnimationCurve FadeInCurve => fadeInCurve;
        public AnimationCurve FadeOutCurve => fadeOutCurve;

        public bool TryValidate(out string errorMessage)
        {
            if (fadeInDuration < 0f)
            {
                errorMessage = $"fadeInDuration cannot be negative. asset='{name}' fadeInDuration='{fadeInDuration}'.";
                return false;
            }

            if (fadeOutDuration < 0f)
            {
                errorMessage = $"fadeOutDuration cannot be negative. asset='{name}' fadeOutDuration='{fadeOutDuration}'.";
                return false;
            }

            if (!HasCurve(fadeInCurve))
            {
                errorMessage = $"fadeInCurve is required. asset='{name}'.";
                return false;
            }

            if (!HasCurve(fadeOutCurve))
            {
                errorMessage = $"fadeOutCurve is required. asset='{name}'.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public void ValidateOrFail(string owner, string context)
        {
            if (TryValidate(out string errorMessage))
            {
                return;
            }

            throw new InvalidOperationException(
                $"[FATAL][Config][Fade] SceneTransitionProfile invalido. owner='{owner}', context='{context}', asset='{name}', detail='{errorMessage}'.");
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnValidate()
        {
            if (TryValidate(out string errorMessage) || string.IsNullOrWhiteSpace(errorMessage))
            {
                return;
            }

            DebugUtility.LogWarning(
                typeof(SceneTransitionProfile),
                $"[Config][Editor] profile='{name}' invalido. detail='{errorMessage}'");
        }
#endif

        private static bool HasCurve(AnimationCurve curve)
        {
            return curve != null && curve.keys != null && curve.keys.Length > 0;
        }
    }
}


