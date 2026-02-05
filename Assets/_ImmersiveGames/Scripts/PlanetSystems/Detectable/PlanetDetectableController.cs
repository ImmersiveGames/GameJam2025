using UnityEngine;
using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.DetectionsSystems.Mono;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.Scripts.AudioSystem.Components;
using _ImmersiveGames.Scripts.AudioSystem.System;

namespace _ImmersiveGames.Scripts.PlanetSystems.Detectable
{
    /// <summary>
    /// Detectável de planeta responsável por reagir à entrada em sensores
    /// (Player, Eater, etc.), revelando o recurso do planeta quando
    /// detectado pela primeira vez e disparando feedback de áudio.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/Planet Systems/Planet Detectable Controller")]
    public sealed class PlanetDetectableController : AbstractDetectable
    {
        private PlanetsMaster _planetMaster;

        [Header("Audio")]
        [SerializeField] private EntityAudioEmitter audioEmitter;
        [SerializeField] private SoundData discoverySound;

        protected override void Awake()
        {
            base.Awake();
            EnsureDependencies();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureDependencies();
        }
#endif

        public override void OnEnterDetection(IDetector detector, DetectionType detectionType)
        {
            if (detectionType != myDetectionType)
            {
                return;
            }

            DebugUtility.LogVerbose<PlanetDetectableController>(
                $"Planeta {gameObject.name} detectado por {GetName(detector)}.",
                DebugUtility.Colors.CrucialInfo,
                this);

            if (_planetMaster == null)
            {
                return;
            }

            if (_planetMaster.IsResourceDiscovered)
            {
                DebugUtility.LogVerbose<PlanetDetectableController>(
                    $"Recurso do planeta {gameObject.name} já estava revelado.",
                    null,
                    this);
                return;
            }

            // Quando detectado pelo Player/Eater o recurso é revelado permanentemente.
            _planetMaster.RevealResource();

            TryPlayDiscoveryAudio();
        }

        public override void OnExitDetection(IDetector detector, DetectionType detectionType)
        {
            if (detectionType != myDetectionType)
            {
                return;
            }

            DebugUtility.LogVerbose<PlanetDetectableController>(
                $"Planeta {gameObject.name} saiu do alcance de {GetName(detector)}.",
                null,
                this);
        }

        private void EnsureDependencies()
        {
            if (_planetMaster == null)
            {
                if (!TryGetComponent(out _planetMaster))
                {
                    _planetMaster = GetComponentInParent<PlanetsMaster>();
                }
            }

            audioEmitter ??= GetComponent<EntityAudioEmitter>();

            if (_planetMaster == null)
            {
                DebugUtility.LogError<PlanetDetectableController>(
                    $"PlanetsMaster não encontrado para o detectável {gameObject.name}.",
                    this);
            }
        }

        private void TryPlayDiscoveryAudio()
        {
            if (audioEmitter == null)
            {
                DebugUtility.LogVerbose<PlanetDetectableController>(
                    $"Nenhum EntityAudioEmitter configurado para tocar áudio de descoberta em {gameObject.name}.",
                    null,
                    this);
                return;
            }

            if (discoverySound == null || discoverySound.clip == null)
            {
                return;
            }

            var context = AudioContext.Default(transform.position, audioEmitter.UsesSpatialBlend);
            audioEmitter.Play(discoverySound, context);
        }
    }
}

