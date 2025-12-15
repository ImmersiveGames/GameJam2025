using UnityEngine;
using UnityEngine.UI;
namespace ImmersiveGames.RuntimeAttributes.Animation
{
    public interface IFillAnimationStrategy
    {
        void Initialize(Image main, Image residual, FillAnimationProfile profile, MonoBehaviour owner);
        void SetInstant(float value);
        void AnimateTo(float target);
        void Cancel();
    }
}