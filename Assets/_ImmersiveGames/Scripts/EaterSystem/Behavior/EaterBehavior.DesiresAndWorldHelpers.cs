using System.Collections.Generic;
using _ImmersiveGames.Scripts.EaterSystem.Events;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Managers;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _ImmersiveGames.Scripts.EaterSystem.Behavior
{
    /// <summary>
    /// Parte da implementação do Eater focada em:
    /// - sistema de desejos (EaterDesireService / EaterDesireInfo);
    /// - helpers de mundo: players, órbita e movimentação utilitária.
    /// </summary>
    public sealed partial class EaterBehavior : MonoBehaviour
    {
        private PlayerManager _playerManager;
        private PlanetMarkingManager _planetMarkingManager;

        private EaterDesireService _desireService;
        private EaterDesireInfo _currentDesireInfo = EaterDesireInfo.Inactive;
        private bool _missingDesireServiceLogged;

        private Transform _lastOrbitTarget;
        private float _lastOrbitRadius = -1f;
        private float _lastSurfaceStopDistance = -1f;

        private bool TryGetClosestPlayer(out Transform player, out float sqrDistance)
        {
            _playerManager ??= PlayerManager.Instance;
            IReadOnlyList<Transform> players = _playerManager?.Players;
            player = null;
            sqrDistance = 0f;

            if (players == null || players.Count == 0)
            {
                return false;
            }

            float bestDistance = float.MaxValue;
            Transform bestPlayer = null;
            Vector3 origin = transform.position;

            foreach (Transform candidate in players)
            {
                if (candidate == null)
                {
                    continue;
                }

                float candidateDistance = (candidate.position - origin).sqrMagnitude;
                if (candidateDistance < bestDistance)
                {
                    bestDistance = candidateDistance;
                    bestPlayer = candidate;
                }
            }

            if (bestPlayer == null)
            {
                return false;
            }

            player = bestPlayer;
            sqrDistance = bestDistance;
            return true;
        }

        private Vector3 ApplyPlayerBounds(Vector3 desiredPosition)
        {
            if (!TryGetClosestPlayerAnchor(out Vector3 anchor, out _))
            {
                return desiredPosition;
            }

            float maxDistance = Mathf.Max(0f, Config?.WanderingMaxDistanceFromPlayer ?? 0f);
            float minDistance = Mathf.Max(0f, Config?.WanderingMinDistanceFromPlayer ?? 0f);

            if (maxDistance <= 0f && minDistance <= 0f)
            {
                return desiredPosition;
            }

            if (maxDistance > 0f && maxDistance < minDistance)
            {
                maxDistance = minDistance;
            }

            Vector3 offset = desiredPosition - anchor;
            float sqrMagnitude = offset.sqrMagnitude;

            if (maxDistance > 0f)
            {
                float maxDistanceSqr = maxDistance * maxDistance;
                if (sqrMagnitude > maxDistanceSqr)
                {
                    desiredPosition = anchor + offset.normalized * maxDistance;
                    offset = desiredPosition - anchor;
                    sqrMagnitude = offset.sqrMagnitude;
                }
            }

            if (minDistance > 0f)
            {
                float minDistanceSqr = minDistance * minDistance;
                if (sqrMagnitude < minDistanceSqr)
                {
                    Vector3 direction = offset.sqrMagnitude > Mathf.Epsilon ? offset.normalized : transform.forward;
                    if (direction.sqrMagnitude <= Mathf.Epsilon)
                    {
                        direction = Vector3.forward;
                    }

                    desiredPosition = anchor + direction * minDistance;
                }
            }

            return desiredPosition;
        }

        /// <summary>
        /// Registra informações sobre o último ponto em que a perseguição foi interrompida.
        /// Mantém a distância radial calculada a partir do centro do planeta e a separação da superfície.
        /// </summary>
        internal void RegisterOrbitAnchor(Transform target, Vector3 targetCenter, float surfaceStopDistance)
        {
            if (target == null)
            {
                ClearOrbitAnchor();
                return;
            }

            _lastOrbitTarget = target;
            _lastSurfaceStopDistance = Mathf.Max(0f, surfaceStopDistance);
            float computedRadius = Vector3.Distance(transform.position, targetCenter);
            _lastOrbitRadius = Mathf.Max(computedRadius, 0f);
        }

        /// <summary>
        /// Obtém o último ponto de parada registrado para o planeta informado.
        /// </summary>
        internal bool TryGetOrbitAnchor(Transform target, out float orbitRadius, out float surfaceStopDistance)
        {
            if (ReferenceEquals(target, _lastOrbitTarget) && _lastOrbitRadius > 0f)
            {
                orbitRadius = _lastOrbitRadius;
                surfaceStopDistance = _lastSurfaceStopDistance;
                return true;
            }

            orbitRadius = 0f;
            surfaceStopDistance = 0f;
            return false;
        }

        /// <summary>
        /// Limpa o ponto de parada registrado, evitando reaproveitar dados obsoletos.
        /// </summary>
        internal void ClearOrbitAnchor(Transform target = null)
        {
            if (target != null && !ReferenceEquals(target, _lastOrbitTarget))
            {
                return;
            }

            _lastOrbitTarget = null;
            _lastOrbitRadius = -1f;
            _lastSurfaceStopDistance = -1f;
        }

        internal float GetRandomRoamingSpeed()
        {
            if (Config == null)
            {
                return 0f;
            }

            float min = Config.MinSpeed;
            float max = Config.MaxSpeed;
            return Random.Range(min, max);
        }

        internal float GetChaseSpeed()
        {
            if (Config == null)
            {
                return 0f;
            }

            float baseSpeed = Config.MaxSpeed;
            return baseSpeed * Config.MultiplierChase;
        }

        internal void Move(Vector3 direction, float speed, float deltaTime, bool respectPlayerBounds)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon || speed <= 0f)
            {
                return;
            }

            Vector3 displacement = direction.normalized * (speed * deltaTime);
            Translate(displacement, respectPlayerBounds);
        }

        internal void Translate(Vector3 displacement, bool respectPlayerBounds)
        {
            Vector3 desiredPosition = transform.position + displacement;
            if (respectPlayerBounds)
            {
                desiredPosition = ApplyPlayerBounds(desiredPosition);
            }

            transform.position = desiredPosition;
        }

        internal void RotateTowards(Vector3 direction, float deltaTime)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            float rotationSpeed = Config != null ? Config.RotationSpeed : 5f;
            rotationSpeed = Mathf.Max(0f, rotationSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, deltaTime * rotationSpeed);
        }

        internal void LookAt(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - transform.position;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = targetRotation;
        }

        internal bool TryGetClosestPlayerAnchor(out Vector3 anchor, out float distance)
        {
            if (TryGetClosestPlayer(out Transform player, out float sqrDistance))
            {
                anchor = player.position;
                distance = Mathf.Sqrt(sqrDistance);
                return true;
            }

            anchor = default;
            distance = 0f;
            return false;
        }

        internal bool BeginDesires(string reason, bool forceRestart = false)
        {
            if (!EnsureDesireService())
            {
                return false;
            }

            if (forceRestart)
            {
                _desireService.Stop();
            }

            bool resumed = !forceRestart && _desireService.TryResume();
            if (resumed)
            {
                if (logStateTransitions)
                {
                    DebugUtility.LogVerbose($"Desejos retomados ({reason}).", DebugUtility.Colors.CrucialInfo, this, this);
                }

                return true;
            }

            bool started = _desireService.Start();
            if (logStateTransitions && started)
            {
                DebugUtility.LogVerbose($"Desejos ativados ({reason}).", DebugUtility.Colors.CrucialInfo, this, this);
            }

            return started;
        }

        internal bool EndDesires(string reason)
        {
            bool stopped = false;

            if (_desireService != null)
            {
                stopped = _desireService.Stop();
                if (logStateTransitions && stopped)
                {
                    DebugUtility.LogVerbose($"Desejos pausados ({reason}).", DebugUtility.Colors.CrucialInfo, this, this);
                }
            }

            if (!stopped)
            {
                EnsureNoActiveDesire(reason);
            }

            return stopped;
        }

        internal bool SuspendDesires(string reason)
        {
            if (_desireService == null)
            {
                return false;
            }

            bool suspended = _desireService.Suspend();
            if (logStateTransitions && suspended)
            {
                DebugUtility.LogVerbose($"Desejos suspensos mantendo seleção atual ({reason}).", DebugUtility.Colors.CrucialInfo, this, this);
            }

            return suspended;
        }

        private void EnsureNoActiveDesire(string reason)
        {
            if (_currentDesireInfo is { ServiceActive: false, HasDesire: false })
            {
                return;
            }

            if (logStateTransitions)
            {
                DebugUtility.LogVerbose($"Desejos finalizados ({reason}).", DebugUtility.Colors.CrucialInfo, this, this);
            }

            UpdateDesireInfo(EaterDesireInfo.Inactive);
        }

        public EaterDesireInfo GetCurrentDesireInfo()
        {
            return _currentDesireInfo;
        }

        private bool EnsureDesireService()
        {
            if (_desireService != null)
            {
                return true;
            }

            if (Master == null || Config == null)
            {
                if (logStateTransitions && !_missingDesireServiceLogged)
                {
                    DebugUtility.LogWarning("Não foi possível inicializar o serviço de desejos (Master ou Config ausentes).", this, this);
                    _missingDesireServiceLogged = true;
                }

                return false;
            }

            _desireService = new EaterDesireService(Master, Config, _audioEmitter);
            _desireService.EventDesireChanged += HandleDesireChanged;
            _missingDesireServiceLogged = false;
            return true;
        }

        private void HandleDesireChanged(EaterDesireInfo info)
        {
            UpdateDesireInfo(info);
        }

        private void UpdateDesireInfo(EaterDesireInfo info)
        {
            _currentDesireInfo = info;
            EventDesireChanged?.Invoke(info);
            EventBus<EaterDesireInfoChangedEvent>.Raise(new EaterDesireInfoChangedEvent(this, info));
        }
    }
}
