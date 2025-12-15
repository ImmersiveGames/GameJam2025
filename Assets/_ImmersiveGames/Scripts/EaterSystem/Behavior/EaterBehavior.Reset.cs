using System.Threading.Tasks;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameplaySystems.Reset;
using _ImmersiveGames.Scripts.PlanetSystems.Managers;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.Behavior
{
    /// <summary>
    /// Participação do Eater no pipeline de reset.
    /// Responsável por limpar bindings, restaurar estado inicial e reconfigurar dependências.
    /// </summary>
    public sealed partial class EaterBehavior : MonoBehaviour, IResetInterfaces, IResetOrder
    {
        private Pose _initialPose;
        private bool _initialPoseCaptured;
        private Rigidbody _rigidbody;

        // Executa depois dos recursos (ex.: RuntimeAttributeController = -80) e da movimentação do player,
        // garantindo que dados de recursos/AutoFlow já estejam rebindados antes do comportamento.
        public int ResetOrder => 20;

        public Task Reset_CleanupAsync(ResetContext ctx)
        {
            // Cleanup: remove bindings e caches para permitir Restore/Rebind idempotentes.
            NeutralizeMotion();
            CleanupDesireBindings();
            PauseAutoFlow("Reset_Cleanup");
            DisposePredicates();
            ResetStateMachineCache();
            ResetDependencyCaches();
            ClearOrbitAnchor();

            return Task.CompletedTask;
        }

        public Task Reset_RestoreAsync(ResetContext ctx)
        {
            // Restore: apenas dados internos, sem reconstruir dependências ou binds.
            _currentDesireInfo = EaterDesireInfo.Inactive;
            ClearOrbitAnchor();

            // Teleporta para pose inicial capturada, zerando movimento para evitar drift (padrão Player).
            if (_initialPoseCaptured)
            {
                NeutralizeMotion();

                if (_rigidbody != null)
                {
                    _rigidbody.position = _initialPose.position;
                    _rigidbody.rotation = _initialPose.rotation;
                    _rigidbody.linearVelocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;
                }
                else
                {
                    transform.SetPositionAndRotation(_initialPose.position, _initialPose.rotation);
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

        private void CaptureInitialPoseIfNeeded()
        {
            if (_initialPoseCaptured)
            {
                return;
            }

            _rigidbody = _rigidbody == null ? GetComponent<Rigidbody>() : _rigidbody;

            // Pose inicial é capturada uma única vez no Awake para servir de referência no Restore.
            _initialPose = new Pose(transform.position, transform.rotation);
            _initialPoseCaptured = true;
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

            _selfDamageReceiver = null;
            _missingSelfDamageReceiverLogged = false;

            _audioEmitter = null;
            _detectionController = null;
            _animationController = null;
        }
    }
}
