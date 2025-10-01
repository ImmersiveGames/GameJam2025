using DG.Tweening;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Configs
{
    [CreateAssetMenu(menuName = "ImmersiveGames/UI/Resource UI Style")]
    public class ResourceUIStyle : ScriptableObject
    {
        [Header("Basic Colors")]
        public Gradient fillGradient;
        public Color pendingColor = new Color(1f, 1f, 1f, 0.6f);

        [Header("Animation Timing")]
        public float quickDuration = 0.2f;
        public float slowDuration = 0.8f;
        public float delayBeforeSlow = 0.3f;

        [Header("Basic Animation")]
        public Ease basicEase = Ease.OutQuad;

        [Header("Advanced Animation Settings")]
        public bool enableAdvancedEffects = true;
        
        [Header("Heal Effects")]
        public float healScaleIntensity = 1.1f;
        public float healScaleDuration = 0.2f;
        public Vector2 healMoveDistance = new Vector2(0f, 5f);
        
        [Header("Damage Effects")] 
        public float damageScaleIntensity = 0.95f;
        public float damageScaleDuration = 0.1f;
        public float damageShakeStrength = 8f;
        public float damageShakeDuration = 0.4f;
        public int damageShakeVibrato = 15;
        
        [Header("Advanced Ease")]
        public Ease advancedEase = Ease.OutBounce;
        public Ease healEase = Ease.OutSine;
        public Ease damageEase = Ease.InOutSine;

        [Header("Smooth Animation Settings")]
        public Ease smoothCurrentEase = Ease.InOutCubic;
        public Ease smoothPendingEase = Ease.InOutSine;
        public float smoothCurrentDuration = 0.4f;
        public float smoothPendingDuration = 1.2f;

        [Header("Pulse Animation Settings")]
        public bool enablePulseEffect = true;
        public float pulseScale = 1.02f;
        public float pulseDuration = 0.8f;
        public Ease pulseEase = Ease.InOutSine;

        [Header("Text Animation Settings")]
        public bool enableTextAnimation;
        public float textScaleIntensity = 1.2f;
        public float textAnimationDuration = 0.3f;
        public Ease textEase = Ease.OutBack;
        
        [Header("Sound & Feedback")]
        public bool enableSoundFeedback;
        // Podemos adicionar AudioClips aqui depois
    }
}