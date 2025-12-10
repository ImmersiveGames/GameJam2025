using UnityEngine;

namespace _ImmersiveGames.Scripts.AnimationSystems.Config
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Animation/Animation Config")]
    public class AnimationConfig : ScriptableObject
    {
        [Header("Basic Animations")]
        public string idleAnimation = "Idle";
        public string hitAnimation = "GetHit";
        public string deathAnimation = "Die";
        public string reviveAnimation = "Revive";

        // Propriedades para hashs (cache automático)
        public int IdleHash => Animator.StringToHash(idleAnimation);
        public int HitHash => Animator.StringToHash(hitAnimation);
        public int DeathHash => Animator.StringToHash(deathAnimation);
        public int ReviveHash => Animator.StringToHash(reviveAnimation);
    }
}