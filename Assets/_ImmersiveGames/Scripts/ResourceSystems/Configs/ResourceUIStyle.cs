using DG.Tweening;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems.Configs
{
    [CreateAssetMenu(menuName = "ImmersiveGames/UI/Resource UI Style")]
    public class ResourceUIStyle : ScriptableObject
    {
        [Header("Colors")]
        public Gradient fillGradient;          // Ex: 1,0 = verde, 0,5 = amarelo, 0,0 = vermelho
        public Color pendingColor = Color.red; // Barra de dano atrasada

        [Header("Animation")]
        public float quickDuration = 0.2f;
        public float slowDuration = 0.8f;
        public float delayBeforeSlow = 0.3f;
        [Header("Additional Effects")]
        public bool enablePulseEffect = false;
        public bool enableTextAnimation = false;
        public Ease animationEase = Ease.OutQuad;
    }
}