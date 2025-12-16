using System.Threading.Tasks;
using DG.Tweening;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameplaySystems.Domain;
using _ImmersiveGames.Scripts.GameplaySystems.Reset;
using _ImmersiveGames.Scripts.PlanetSystems.Managers;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.Behavior
{
    /// <summary>
    /// Participação do Eater no pipeline de reset.
    /// Responsável por limpar bindings, restaurar estado inicial e reconfigurar dependências.
    /// </summary>
    public sealed partial class EaterBehavior : MonoBehaviour, IResetInterfaces, IResetOrder
    {
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private bool _initialTransformCaptured;
        private Rigidbody _rigidbody;
        private IEaterDomain _eaterDomain;
        private bool _rigidbodyWasKinematic;

        // Executa depois dos recursos (ex.: RuntimeAttributeController = -80) e da movimentação do player,
        // garantindo que dados de recursos/AutoFlow já estejam rebindados antes do comportamento.
        public int ResetOrder => 20;

        public Task Reset_CleanupAsync(ResetContext ctx)
        {
            // Cleanup: remove bindings e caches para permitir Restore/Rebind idempotentes.
            NeutralizeMotion();
            ForceStopTweens();
            ForceKinematicDuringReset();
            CleanupDesireBindings();
            PauseAutoFlow("Reset_Cleanup");
            DisposePredicates();
            ResetStateMachineCache();
            ResetDependencyCaches();
            ClearOrbitAnchor();
            _executionAllowed = false;

            return Task.CompletedTask;
        }

        public Task Reset_RestoreAsync(ResetContext ctx)
        {
            // Restore: apenas dados internos, sem reconstruir dependências ou binds.
            _currentDesireInfo = EaterDesireInfo.Inactive;
            ClearOrbitAnchor();

            Vector3 targetPosition;
            Quaternion targetRotation;
            bool hasSpawnTransform = TryGetDomainSpawnTransform(out targetPosition, out targetRotation);

            // Teleporta para transform inicial capturado (fallback) ou SpawnTransform do domínio, zerando movimento para evitar drift.
            if (hasSpawnTransform || _initialTransformCaptured)
            {
                NeutralizeMotion();

                if (!hasSpawnTransform)
                {
                    targetPosition = _initialPosition;
                    targetRotation = _initialRotation;
                }

                if (_rigidbody != null)
                {
                    _rigidbody.position = targetPosition;
                    _rigidbody.rotation = targetRotation;
                    _rigidbody.linearVelocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;
                }
                else
                {
                    transform.SetPositionAndRotation(targetPosition, targetRotation);
                }
            }

            return Task.CompletedTask;
        }

        public Task Reset_RebindAsync(ResetContext ctx)
        {
            // Rebind: re-resolve dependências externas e reconstrói state machine/binds.
            Master ??= GetComponent<EaterMaster>();
            Config = Master != null ? Master.Config : null;

            _planetMarkingManager = PlanetMarkingManager.Instance;
            _playerManager = PlayerManager.Instance;

            TryGetDetectionController(out _);
            TryGetAnimationController(out _);
            TryGetAudioEmitter(out _);
            TryEnsureAutoFlowBridge();

            // A state machine só é construída quando inexistente; o método é idempotente e evita duplicar transições/bindings internos.
            EnsureStatesInitialized();

            if (_autoFlowBridge != null)
            {
                ResumeAutoFlow("Reset_Rebind");
            }

            if (EnsureDesireService())
            {
                _desireService.EventDesireChanged -= HandleDesireChanged;
                _desireService.EventDesireChanged += HandleDesireChanged;
                _desireService.Stop();
                UpdateDesireInfo(EaterDesireInfo.Inactive);
            }

            RestoreKinematicState();
            _executionAllowed = true;

            return Task.CompletedTask;
        }

        private void CleanupDesireBindings()
        {
            if (_desireService != null)
            {
                _desireService.EventDesireChanged -= HandleDesireChanged;
                _desireService.Stop();
                _desireService = null;
            }

            _missingDesireServiceLogged = false;
            _currentDesireInfo = EaterDesireInfo.Inactive;
        }

        private void CaptureInitialTransformIfNeeded()
        {
            if (_initialTransformCaptured)
            {
                return;
            }

            _rigidbody = _rigidbody == null ? GetComponent<Rigidbody>() : _rigidbody;

            // Transform inicial é capturado uma única vez no Awake para servir de referência no Restore.
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _initialTransformCaptured = true;
        }

        internal bool TryGetInitialTransform(out Vector3 position, out Quaternion rotation)
        {
            CaptureInitialTransformIfNeeded();
            position = _initialPosition;
            rotation = _initialRotation;
            return _initialTransformCaptured;
        }

        private void NeutralizeMotion()
        {
            if (_rigidbody == null)
            {
                TryGetComponent(out _rigidbody);
            }

            if (_rigidbody == null)
            {
                return;
            }

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        private void ForceKinematicDuringReset()
        {
            if (_rigidbody == null)
            {
                TryGetComponent(out _rigidbody);
            }

            if (_rigidbody == null)
            {
                return;
            }

            _rigidbodyWasKinematic = _rigidbody.isKinematic;
            _rigidbody.isKinematic = true;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        private void RestoreKinematicState()
        {
            if (_rigidbody == null)
            {
                return;
            }

            _rigidbody.isKinematic = _rigidbodyWasKinematic;
        }

        private void ForceStopTweens()
        {
            // Garante que nenhum tween pendente continue movendo o Eater durante o reset.
            DOTween.Kill(transform);

            if (_rigidbody != null)
            {
                DOTween.Kill(_rigidbody);
            }
        }

        private void DisposePredicates()
        {
            // Predicados descartáveis possuem bindings em EventBus e precisam ser liberados para evitar leaks.
            _deathPredicate?.Dispose();
            _revivePredicate?.Dispose();
            _planetUnmarkedPredicate?.Dispose();

            _deathPredicate = null;
            _revivePredicate = null;
            _wanderingTimeoutPredicate = null;
            _hungryChasingPredicate = null;
            _chasingEatingPredicate = null;
            _planetUnmarkedPredicate = null;
            _eatingHungryPredicate = null;
            _eatingWanderingPredicate = null;
            _missingMasterForPredicatesLogged = false;
        }

        private void ResetStateMachineCache()
        {
            _stateMachine = null;
            _wanderingState = null;
            _hungryState = null;
            _chasingState = null;
            _eatingState = null;
            _deathState = null;
        }

        private void ResetDependencyCaches()
        {
            _autoFlowBridge = null;
            _missingAutoFlowBridgeLogged = false;
            _autoFlowUnavailableLogged = false;
            _missingResourceSystemLogged = false;

            _eaterDomain = null;

            _selfDamageReceiver = null;
            _missingSelfDamageReceiverLogged = false;

            _audioEmitter = null;
            _detectionController = null;
            _animationController = null;
        }

        private bool TryGetDomainSpawnTransform(out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = default;

            if (_eaterDomain == null)
            {
                var sceneName = gameObject.scene.name;
                if (!DependencyManager.Provider.TryGetForScene<IEaterDomain>(sceneName, out _eaterDomain))
                {
                    _eaterDomain = null;
                }
            }

            return _eaterDomain != null && _eaterDomain.TryGetSpawnTransform(out position, out rotation);
        }
    }
}
