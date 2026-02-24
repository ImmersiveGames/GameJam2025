using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings
{
    /// <summary>
    /// Profile editável (ScriptableObject) para parametrizar a transição no NewScripts.
    /// Neste passo cobre apenas parâmetros de Fade (ADR-0009).
    /// A decisão de usar ou não Fade é responsabilidade de TransitionStyle/request, não do Profile.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SceneTransitionProfile",
        menuName = "ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Profiles/SceneTransitionProfile",
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
    }
}
