using DG.Tweening;
using UnityEngine;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.UI.Animation
{
    [CreateAssetMenu(menuName = "ImmersiveGames/UI/Fill Animation Profile")]
    public class FillAnimationProfile : ScriptableObject
    {
        public FillAnimationType animationType = FillAnimationType.BasicReactive;

        [Header("Main Bar")]
        [Range(0.05f, 2f)] public float mainSpeed = 0.3f;
        public Ease mainEase = Ease.OutQuad;

        [Header("Residual Bar")]
        [Range(0f, 2f)] public float residualDelay = 0.3f;
        [Range(0.05f, 2f)] public float residualSpeed = 0.6f;
        public Ease residualEase = Ease.OutCubic;

        [Header("Color Transition")]
        [Min(0f)] public float colorTransitionDuration = 0.2f;
        public Ease colorTransitionEase = Ease.OutQuad;
    }
}
