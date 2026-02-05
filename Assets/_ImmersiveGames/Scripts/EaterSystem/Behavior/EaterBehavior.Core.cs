using System;
using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.AudioSystem.Components;
using _ImmersiveGames.Scripts.EaterSystem.Animations;
using _ImmersiveGames.Scripts.EaterSystem.Configs;
using _ImmersiveGames.Scripts.EaterSystem.Detections;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Managers;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem.Behavior
{
    /// <summary>
    /// Controle básico do comportamento do Eater.
    /// Cria os estados conhecidos e integra com desejos, recursos, animação, etc.
    /// </summary>
    [RequireComponent(typeof(EaterMaster), typeof(EaterAnimationController), typeof(AnimationSystems.Components.AnimationResolver))]
    [AddComponentMenu("ImmersiveGames/Eater/Eater Behavior")]
    [DefaultExecutionOrder(10)]
    public sealed partial class EaterBehavior : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField, Tooltip("Registra mudanças de estado para depuração básica.")]
        private bool logStateTransitions = true;

        internal bool ShouldLogStateTransitions => logStateTransitions;

        public event Action<EaterDesireInfo> EventDesireChanged;

        internal EaterMaster Master { get; private set; }

        internal EaterConfigSo Config { get; private set; }

        internal Transform CurrentTargetPlanet => _planetMarkingManager?.CurrentlyMarkedPlanet != null
            ? _planetMarkingManager.CurrentlyMarkedPlanet.transform
            : null;

        private void Awake()
        {
            Master = GetComponent<EaterMaster>();
            Config = Master != null ? Master.Config : null;
            _audioEmitter = GetComponent<EntityAudioEmitter>();
            _detectionController = GetComponent<EaterDetectionController>();
            _animationController = GetComponent<EaterAnimationController>();
            _planetMarkingManager = PlanetMarkingManager.Instance;
            _playerManager = PlayerManager.Instance;
            CaptureInitialPoseIfNeeded();
            TryEnsureAutoFlowBridge();
            EnsureDesireService();
            EnsureStatesInitialized();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            Master ??= GetComponent<EaterMaster>();
            Config = Master != null ? Master.Config : null;
            _audioEmitter = GetComponent<EntityAudioEmitter>();
            _detectionController = GetComponent<EaterDetectionController>();
            _animationController = GetComponent<EaterAnimationController>();
        }
#endif

        private void Update()
        {
            _desireService?.Update();
            _stateMachine?.Update();
        }

        private void OnDestroy()
        {
            if (_desireService != null)
            {
                _desireService.EventDesireChanged -= HandleDesireChanged;
                _desireService.Stop();
            }

            _deathPredicate?.Dispose();
            _deathPredicate = null;

            _revivePredicate?.Dispose();
            _revivePredicate = null;

            _planetUnmarkedPredicate?.Dispose();
            _planetUnmarkedPredicate = null;
            _eatingWanderingPredicate = null;
        }
    }
}
