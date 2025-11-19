using UnityEngine;
namespace _ImmersiveGames.Scripts.AnimationSystems.Interfaces
{
    public interface IActorAnimationController
    {
        void PlayHit();
        void PlayDeath();
        void PlayRevive();
        void PlayIdle();
    }
    public interface IAnimatorProvider
    {
        Animator GetAnimator();
    }
}