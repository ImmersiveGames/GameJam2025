using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow
{
    /// <summary>
    /// Profile editável (ScriptableObject) para parametrizar a transição no NewScripts.
    /// Neste passo cobre apenas Fade (ADR-0009). Loading fica para ADR separado.
    /// </summary>
    [CreateAssetMenu(
        fileName = "startup",
        menuName = "ImmersiveGames/NewScripts/SceneFlow/Transition Profile",
        order = 10)]
    public sealed class SceneTransitionProfile : ScriptableObject
    {
        [Header("Fade")]
        [SerializeField] private bool useFade = true;

        [Min(0f)]
        [SerializeField] private float fadeInDuration = 0.5f;

        [Min(0f)]
        [SerializeField] private float fadeOutDuration = 0.5f;

        [SerializeField] private AnimationCurve fadeInCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [SerializeField] private AnimationCurve fadeOutCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public bool UseFade => useFade;
        public float FadeInDuration => fadeInDuration;
        public float FadeOutDuration => fadeOutDuration;
        public AnimationCurve FadeInCurve => fadeInCurve;
        public AnimationCurve FadeOutCurve => fadeOutCurve;
    }
}
