using _ImmersiveGames.Scripts.AnimationSystems.Config;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.Configs
{
    /// <summary>
    /// Configuração de animações específicas do Eater.
    /// 
    /// Herda de <see cref="AnimationConfig"/> e expõe parâmetros de animação
    /// que podem ser referenciados tanto por nome (para configuração no Animator)
    /// quanto por hash (para uso em runtime sem alocação de strings).
    /// </summary>
    [CreateAssetMenu(menuName = "ImmersiveGames/Animation/Eater Animation Config")]
    public class EaterAnimationConfig : AnimationConfig
    {
        [Header("Eater Animations")]
        [Tooltip("Parâmetro booleano usado para indicar o estado de alimentação (isEating).")]
        public string eatingAnimation = "isEating";

        [Tooltip("Parâmetro/trigger para animação de 'feliz' do Eater.")]
        public string happyAnimation = "Happy";

        [Tooltip("Parâmetro/trigger para animação de 'irritado' do Eater.")]
        public string madAnimation = "Mad";

        // Propriedades para hashs (cache automático)
        public int EatingHash => Animator.StringToHash(eatingAnimation);
        public int HappyHash => Animator.StringToHash(happyAnimation);
        public int MadHash => Animator.StringToHash(madAnimation);
    }
}