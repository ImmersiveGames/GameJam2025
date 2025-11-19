using _ImmersiveGames.Scripts.AnimationSystems.Config;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem.Configs
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Animation/Eater Animation Config")]
    public class EaterAnimationConfig : AnimationConfig
    {
        [Header("Eater Animations")]
        public string eatingAnimation = "isEating";
        public string happyAnimation = "Happy";
        public string madAnimation = "Mad";
        public string biteAnimation = "Bite";
        

        // Propriedades para hashs (cache automático)
        public int EatingHash => Animator.StringToHash(eatingAnimation);
        public int HappyHash => Animator.StringToHash(happyAnimation);
        public int MadHash => Animator.StringToHash(madAnimation);
        public int BiteHash => Animator.StringToHash(biteAnimation);


    }
}