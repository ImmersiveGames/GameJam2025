// PlayerAnimationConfig.cs

using UnityEngine;
namespace _ImmersiveGames.Scripts.AnimationSystems.Config
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Animation/Player Animation Config")]
    public class PlayerAnimationConfig : AnimationConfig
    {
        [Header("Player Specific Animations")]
        public string attackAnimation = "Attack";
        public string specialAnimation = "Special";
        public string jumpAnimation = "Jump";
        public string crouchAnimation = "Crouch";
    
        public int AttackHash => Animator.StringToHash(attackAnimation);
        public int SpecialHash => Animator.StringToHash(specialAnimation);
        public int JumpHash => Animator.StringToHash(jumpAnimation);
        public int CrouchHash => Animator.StringToHash(crouchAnimation);
    }
}