using DG.Tweening;
using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.EaterSystem;
using _ImmersiveGames.Scripts.EaterSystem.Animations;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using ImprovedTimers;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado de alimentação: orbita o planeta marcado utilizando DOTween e aplica dano periódico ao alvo.
    /// </summary>
    internal sealed class EaterEatingState : EaterBehaviorState
    {
        private const float DefaultMinimumSurfaceDistance = 1.5f;
        private const float DefaultOrbitDuration = 4f;
        private const float DefaultOrbitApproachDuration = 0.5f;
        private const float OrbitDistanceTolerance = 0.05f;
        private const float DefaultDamageAmount = 10f;
        private const float DefaultDamageInterval = 1f;
        private const float DefaultRecoveryAmount = 5f;
        private const float DefaultRecoveryInterval = 1f;
        private const ResourceType DefaultRecoveryResource = ResourceType.Health;
        private const float IncompatibleRecoveryMultiplier = 0.5f;
        private const float DefaultDevourHealAmount = 25f;
        private const float MinimumTimerInterval = 0.05f;

        private Tween _approachTween;
        private Tween _orbitTween;
        private Transform _currentTarget;
        private Vector3 _radialBasis;
        private float _currentAngle;
        private EaterAnimationController _animationController;
        private bool _missingAnimationLogged;
        private PlanetOrbitFreezeController _orbitFreezeController;
        private float _currentOrbitRadius;
        private PlanetsMaster _activePlanet;
        private IDamageReceiver _currentDamageReceiver;
        private CountdownTimer _damageTimer;
        private CountdownTimer _recoveryTimer;
        private bool _pendingWanderingTransition;
        private bool _missingDamageReceiverLogged;
        private bool _hasReportedDestroyedPlanet;
        private bool _hasReportedDevouredCompatibility;
        private PlanetResourcesSo _cachedTargetResourceAsset;
        private PlanetResources _cachedTargetResourceType;
        private bool _hasCachedTargetResource;
        private bool _hasCompatibilityResult;
        private bool _isTargetCompatible;
        private bool _hasEvaluatedDesiredResource;
        private bool _hasEvaluatedPlanetResource;
        private PlanetResources _evaluatedDesiredResource;
        private PlanetResources _evaluatedPlanetResource;
        private bool _missingAudioEmitterLogged;
        private bool _hasAppliedDevourReward;

        private float OrbitDuration => Config?.OrbitDuration ?? DefaultOrbitDuration;

        private float OrbitApproachDuration
        {
            get
            {
                float duration = OrbitDuration;
                float approach = Config?.OrbitApproachDuration ?? DefaultOrbitApproachDuration;
                approach = Mathf.Max(0.1f, approach);
                return Mathf.Min(approach, duration);
            }
        }

        public EaterEatingState() : base("Eating")
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();

            _pendingWanderingTransition = false;
            StopDamageTimer();
            StopRecoveryTimer();
            _hasReportedDestroyedPlanet = false;
            _hasReportedDevouredCompatibility = false;
            _missingDamageReceiverLogged = false;
            _currentDamageReceiver = null;
            ResetCompatibility();
            _missingAudioEmitterLogged = false;
            _hasAppliedDevourReward = false;

            if (Master != null)
            {
                Master.IsEating = true;
            }

            UpdateEatingAnimation(isEating: true);

            EnsureOrbitFreezeController().TryFreeze(Behavior, Behavior.CurrentTargetPlanet);
            PrepareTarget(Behavior.CurrentTargetPlanet, restartOrbit: true);
        }

        public override void OnExit()
        {
            UpdateEatingAnimation(isEating: false);

            if (Master != null)
            {
                Master.IsEating = false;
            }

            base.OnExit();
            StopTweens();
            EnsureOrbitFreezeController().Release();
            UpdatePlanetMaster(null);

            StopDamageTimer();
            StopRecoveryTimer();
            _currentDamageReceiver = null;
            _currentTarget = null;
            _pendingWanderingTransition = false;
            _missingDamageReceiverLogged = false;
            _hasReportedDevouredCompatibility = false;
            _cachedTargetResourceAsset = null;
            _cachedTargetResourceType = default;
            _hasCachedTargetResource = false;
            ResetCompatibility();
            _missingAudioEmitterLogged = false;
            _hasAppliedDevourReward = false;
        }

        public override void Update()
        {
            base.Update();

            if (_pendingWanderingTransition)
            {
                return;
            }

            Transform target = Behavior.CurrentTargetPlanet;
            EnsureOrbitFreezeController().TryFreeze(Behavior, target);
            if (target != _currentTarget)
            {
                PrepareTarget(target, restartOrbit: true);
                if (_pendingWanderingTransition)
                {
                    return;
                }
            }

            if (_currentTarget == null)
            {
                return;
            }

            if (!EnsureTargetAlive())
            {
                return;
            }

            LookAtTarget();
            TickDamage();
            TickRecovery();
        }

        private void PrepareTarget(Transform target, bool restartOrbit)
        {
            bool targetChanged = !ReferenceEquals(_currentTarget, target);
            if (targetChanged)
            {
                StopTweens();
                _hasReportedDestroyedPlanet = false;
                _hasReportedDevouredCompatibility = false;
                _hasAppliedDevourReward = false;
            }

            if (target == null)
            {
                if (targetChanged)
                {
                    TryReportDevouredPlanetOnTargetCleared();
                }

                _currentTarget = null;

                if (Behavior.ShouldLogStateTransitions)
                {
                    DebugUtility.LogWarning<EaterEatingState>("Estado Eating sem planeta marcado.", Behavior);
                }

                EnsureOrbitFreezeController().Release();
                UpdatePlanetMaster(null);
                _currentDamageReceiver = null;
                StopDamageTimer();
                StopRecoveryTimer();
                _missingDamageReceiverLogged = false;
                ResetCompatibility();
                return;
            }

            _currentTarget = target;
            PlanetsMaster resolvedPlanet = ResolvePlanetMaster(_currentTarget);
            UpdatePlanetMaster(resolvedPlanet);
            _currentDamageReceiver = ResolveDamageReceiver(_currentTarget);
            _missingDamageReceiverLogged = false;
            RearmDamageTimer();
            RearmRecoveryTimer();

            if (targetChanged)
            {
                ResetCompatibility();
            }

            TryResolveTargetCompatibility();

            _radialBasis = ResolveRadialBasis();
            _currentOrbitRadius = ResolveOrbitRadius(_currentTarget);
            Vector3 desiredPosition = _currentTarget.position + _radialBasis * _currentOrbitRadius;
            float distanceToDesired = Vector3.Distance(Transform.position, desiredPosition);

            if (distanceToDesired > 0.05f)
            {
                _approachTween = Transform.DOMove(desiredPosition, OrbitApproachDuration)
                    .SetEase(Ease.InOutSine)
                    .OnUpdate(LookAtTarget)
                    .OnComplete(StartOrbit);
            }
            else if (restartOrbit)
            {
                Transform.position = desiredPosition;
                StartOrbit();
            }
        }

        private void StartOrbit()
        {
            StopOrbitTween();

            if (_currentTarget == null)
            {
                return;
            }

            _radialBasis = ResolveRadialBasis();
            _currentOrbitRadius = ResolveOrbitRadius(_currentTarget);
            _currentAngle = 0f;

            _orbitTween = DOTween.To(() => _currentAngle, angle =>
                {
                    _currentAngle = angle;
                    ApplyOrbitAngle(angle);
                },
                360f,
                OrbitDuration)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .OnUpdate(LookAtTarget);

            if (Behavior.ShouldLogStateTransitions)
            {
                DebugUtility.Log(
                    "Órbita iniciada.",
                    DebugUtility.Colors.Success,
                    context: Behavior,
                    instance: this);
            }
        }

        private void ApplyOrbitAngle(float angle)
        {
            if (_currentTarget == null)
            {
                return;
            }

            Vector3 axis = ResolveOrbitAxis();
            Vector3 radial = Vector3.ProjectOnPlane(_radialBasis, axis);
            if (radial.sqrMagnitude <= Mathf.Epsilon)
            {
                radial = ResolveRadialBasis();
            }
            radial = radial.normalized;
            _radialBasis = radial;
            Vector3 tangent = Vector3.Cross(axis, radial).normalized;

            float radians = angle * Mathf.Deg2Rad;
            Vector3 offset = (radial * Mathf.Cos(radians) + tangent * Mathf.Sin(radians)) * _currentOrbitRadius;
            Transform.position = _currentTarget.position + offset;
        }

        private Vector3 ResolveRadialBasis()
        {
            if (_currentTarget == null)
            {
                return Transform.forward.sqrMagnitude > Mathf.Epsilon ? Transform.forward.normalized : Vector3.forward;
            }

            Vector3 axis = ResolveOrbitAxis();
            Vector3 offset = Transform.position - _currentTarget.position;
            Vector3 planarOffset = Vector3.ProjectOnPlane(offset, axis);
            if (planarOffset.sqrMagnitude <= Mathf.Epsilon)
            {
                planarOffset = Vector3.ProjectOnPlane(Transform.forward, axis);
            }

            if (planarOffset.sqrMagnitude <= Mathf.Epsilon)
            {
                planarOffset = Vector3.ProjectOnPlane(Vector3.right, axis);
            }

            return planarOffset.normalized;
        }

        private Vector3 ResolveOrbitAxis()
        {
            return _currentTarget != null ? _currentTarget.up.normalized : Vector3.up;
        }

        private void LookAtTarget()
        {
            if (_currentTarget == null)
            {
                return;
            }

            Behavior.LookAt(_currentTarget.position);
        }

        private void StopTweens()
        {
            StopOrbitTween();
            if (_approachTween != null && _approachTween.IsActive())
            {
                _approachTween.Kill();
            }
            _approachTween = null;
        }

        private void StopOrbitTween()
        {
            if (_orbitTween != null && _orbitTween.IsActive())
            {
                _orbitTween.Kill();
            }
            _orbitTween = null;
        }

        private PlanetOrbitFreezeController EnsureOrbitFreezeController()
        {
            return _orbitFreezeController ??= new PlanetOrbitFreezeController(this);
        }

        private float ResolveOrbitRadius(Transform target)
        {
            float fallback = Config?.MinimumChaseDistance ?? DefaultMinimumSurfaceDistance;

            if (Behavior == null || target == null)
            {
                return fallback;
            }

            if (Behavior.TryGetOrbitAnchor(target, out float orbitRadius, out _))
            {
                if (orbitRadius > OrbitDistanceTolerance)
                {
                    return orbitRadius;
                }

                fallback = Mathf.Max(orbitRadius, fallback);
            }

            float currentDistance = Vector3.Distance(Transform.position, target.position);
            if (currentDistance > OrbitDistanceTolerance)
            {
                return currentDistance;
            }

            return fallback;
        }

        internal bool ConsumeWanderingTransitionRequest()
        {
            if (!_pendingWanderingTransition)
            {
                return false;
            }

            _pendingWanderingTransition = false;
            return true;
        }

        private bool TryEnsureAnimationController()
        {
            if (_animationController != null)
            {
                return true;
            }

            if (Behavior == null)
            {
                return false;
            }

            if (Behavior.TryGetAnimationController(out EaterAnimationController controller))
            {
                _animationController = controller;
                _missingAnimationLogged = false;
                return true;
            }

            if (!_missingAnimationLogged)
            {
                DebugUtility.LogWarning(
                    "EaterAnimationController não encontrado. Não será possível atualizar animação de alimentação.",
                    Behavior,
                    this);
                _missingAnimationLogged = true;
            }

            return false;
        }

        private void UpdateEatingAnimation(bool isEating)
        {
            if (!TryEnsureAnimationController())
            {
                return;
            }

            _animationController.SetEating(isEating);
        }

        private void TickDamage()
        {
            TryResolveTargetCompatibility();

            float interval = ResolveDamageInterval();

            if (_damageTimer == null)
            {
                RearmDamageTimer(interval);
                return;
            }

            if (_damageTimer.IsRunning)
            {
                _damageTimer.Tick();
            }

            if (!_damageTimer.IsFinished)
            {
                return;
            }

            if (!TryApplyBiteDamage())
            {
                RearmDamageTimer(interval);
                return;
            }

            float nextInterval = ResolveDamageInterval();
            RearmDamageTimer(nextInterval);
        }

        private void TickRecovery()
        {
            if (!TryGetRecoveryConfig(out ResourceType resourceType, out float recoveryAmount, out float recoveryInterval))
            {
                StopRecoveryTimer();
                return;
            }

            recoveryInterval = Mathf.Max(recoveryInterval, MinimumTimerInterval);

            if (_recoveryTimer == null)
            {
                RearmRecoveryTimer(recoveryInterval);
                return;
            }

            if (_recoveryTimer.IsRunning)
            {
                _recoveryTimer.Tick();
            }

            if (!_recoveryTimer.IsFinished)
            {
                return;
            }

            bool hasCompatibility = TryEvaluateCompatibility(out bool isCompatible);
            float multiplier = hasCompatibility && isCompatible ? 1f : IncompatibleRecoveryMultiplier;
            float adjustedAmount = recoveryAmount * multiplier;

            if (adjustedAmount <= Mathf.Epsilon)
            {
                RearmRecoveryTimer(recoveryInterval);
                return;
            }

            if (!TryApplyRecovery(resourceType, adjustedAmount))
            {
                RearmRecoveryTimer(recoveryInterval);
                return;
            }

            if (!TryGetRecoveryConfig(out resourceType, out recoveryAmount, out float nextInterval))
            {
                StopRecoveryTimer();
                return;
            }

            recoveryInterval = Mathf.Max(nextInterval, MinimumTimerInterval);
            RearmRecoveryTimer(recoveryInterval);
        }

        private bool TryApplyBiteDamage()
        {
            IDamageReceiver receiver = _currentDamageReceiver ??= ResolveDamageReceiver(_currentTarget);
            if (receiver == null)
            {
                if (!_missingDamageReceiverLogged && Behavior.ShouldLogStateTransitions)
                {
                    DebugUtility.LogWarning(
                        "Nenhum IDamageReceiver encontrado no planeta consumido. Não será possível aplicar dano.",
                        Behavior,
                        this);
                    _missingDamageReceiverLogged = true;
                }

                return false;
            }

            _missingDamageReceiverLogged = false;

            float damage = Config != null ? Config.EatingDamageAmount : DefaultDamageAmount;
            if (damage <= Mathf.Epsilon)
            {
                return false;
            }

            string attackerId = Master != null ? Master.ActorId : string.Empty;
            string targetId = receiver.GetReceiverId();
            ResourceType resource = Config != null ? Config.EatingDamageResource : ResourceType.Health;
            DamageType damageType = Config != null ? Config.EatingDamageType : DamageType.Physical;
            Vector3 hitPosition = _currentTarget.position;

            var context = new DamageContext(attackerId, targetId, damage, resource, damageType, hitPosition);
            receiver.ReceiveDamage(context);

            if (Master != null && _activePlanet != null)
            {
                Master.OnEventEaterBite(_activePlanet);
            }

            TryPlayBiteSound();

            return true;
        }

        private bool TryApplyRecovery(ResourceType resourceType, float amount)
        {
            if (Behavior == null)
            {
                return false;
            }

            return Behavior.TryRestoreResource(resourceType, amount);
        }

        private float ResolveDamageInterval()
        {
            float interval = Config != null ? Config.EatingDamageInterval : DefaultDamageInterval;
            return Mathf.Max(interval, MinimumTimerInterval);
        }

        private void RearmDamageTimer()
        {
            RearmDamageTimer(ResolveDamageInterval());
        }

        private void RearmDamageTimer(float interval)
        {
            interval = Mathf.Max(interval, MinimumTimerInterval);

            if (_damageTimer == null)
            {
                _damageTimer = new CountdownTimer(interval);
            }

            _damageTimer.Stop();
            _damageTimer.Reset(interval);
            _damageTimer.Start();
        }

        private void StopDamageTimer()
        {
            if (_damageTimer == null)
            {
                return;
            }

            _damageTimer.Stop();
            _damageTimer = null;
        }

        private void RearmRecoveryTimer()
        {
            if (TryGetRecoveryConfig(out _, out _, out float interval))
            {
                RearmRecoveryTimer(interval);
            }
            else
            {
                StopRecoveryTimer();
            }
        }

        private void RearmRecoveryTimer(float interval)
        {
            interval = Mathf.Max(interval, MinimumTimerInterval);

            if (interval <= Mathf.Epsilon)
            {
                StopRecoveryTimer();
                return;
            }

            if (_recoveryTimer == null)
            {
                _recoveryTimer = new CountdownTimer(interval);
            }

            _recoveryTimer.Stop();
            _recoveryTimer.Reset(interval);
            _recoveryTimer.Start();
        }

        private void StopRecoveryTimer()
        {
            if (_recoveryTimer == null)
            {
                return;
            }

            _recoveryTimer.Stop();
            _recoveryTimer = null;
        }

        private bool TryGetRecoveryConfig(out ResourceType resourceType, out float amount, out float interval)
        {
            resourceType = Config != null ? Config.EatingRecoveryResource : DefaultRecoveryResource;
            amount = Config != null ? Config.EatingRecoveryAmount : DefaultRecoveryAmount;
            interval = Config != null ? Config.EatingRecoveryInterval : DefaultRecoveryInterval;

            amount = Mathf.Max(0f, amount);
            interval = Mathf.Max(0f, interval);

            if (amount <= Mathf.Epsilon || interval <= Mathf.Epsilon)
            {
                return false;
            }

            return true;
        }

        private void TryPlayBiteSound()
        {
            SoundData biteSound = Config != null ? Config.EatingBiteSound : null;
            if (biteSound == null || biteSound.clip == null)
            {
                return;
            }

            if (Behavior == null)
            {
                return;
            }

            if (!Behavior.TryGetAudioEmitter(out EntityAudioEmitter emitter))
            {
                if (!_missingAudioEmitterLogged && Behavior.ShouldLogStateTransitions)
                {
                    DebugUtility.LogWarning<EaterEatingState>(
                        "EntityAudioEmitter não encontrado para tocar som de mordida.",
                        Behavior,
                        this);
                    _missingAudioEmitterLogged = true;
                }

                return;
            }

            _missingAudioEmitterLogged = false;

            Vector3 position = _currentTarget != null ? _currentTarget.position : Transform.position;
            AudioContext context = AudioContext.Default(position, emitter.UsesSpatialBlend);
            emitter.Play(biteSound, context);
        }

        private bool TryEvaluateCompatibility(out bool isCompatible)
        {
            if (TryResolveTargetCompatibility())
            {
                isCompatible = _isTargetCompatible;
                return true;
            }

            isCompatible = false;
            return false;
        }

        private void UpdatePlanetMaster(PlanetsMaster newPlanet)
        {
            if (ReferenceEquals(_activePlanet, newPlanet))
            {
                return;
            }

            if (_activePlanet != null && Master != null)
            {
                Master.OnEventEndEatPlanet(_activePlanet);
            }

            _activePlanet = newPlanet;
            CacheTargetResourceData(newPlanet);

            if (_activePlanet != null && Master != null)
            {
                Master.OnEventStartEatPlanet(_activePlanet);
            }
        }

        private bool EnsureTargetAlive()
        {
            if (_activePlanet == null)
            {
                return true;
            }

            if (_activePlanet.IsActive)
            {
                return true;
            }

            HandleDestroyedPlanet();
            return false;
        }

        private void HandleDestroyedPlanet()
        {
            if (_pendingWanderingTransition)
            {
                return;
            }

            ReportDevouredPlanetCompatibility();

            if (Behavior.ShouldLogStateTransitions && !_hasReportedDestroyedPlanet)
            {
                DebugUtility.Log(
                    "Planeta consumido foi destruído. Retornando ao estado de passeio.",
                    DebugUtility.Colors.CrucialInfo,
                    Behavior,
                    this);
                _hasReportedDestroyedPlanet = true;
            }

            Behavior?.ClearOrbitAnchor(_currentTarget);
            UpdatePlanetMaster(null);
            _currentDamageReceiver = null;
            StopDamageTimer();
            _missingDamageReceiverLogged = false;
            _currentTarget = null;
            StopRecoveryTimer();
            ResetCompatibility();

            RequestWanderingTransition("PlanetDestroyed");

            StopTweens();
            EnsureOrbitFreezeController().Release();
        }

        private void ReportDevouredPlanetCompatibility()
        {
            if (_hasReportedDevouredCompatibility)
            {
                return;
            }

            if (Behavior == null)
            {
                return;
            }

            _hasReportedDevouredCompatibility = true;

            TryResolveTargetCompatibility();

            PlanetResources planetResource;
            bool hasPlanetResource;

            if (_hasEvaluatedPlanetResource)
            {
                planetResource = _evaluatedPlanetResource;
                hasPlanetResource = true;
            }
            else
            {
                hasPlanetResource = TryGetTargetResourceType(out planetResource);
            }

            if (!hasPlanetResource)
            {
                DebugUtility.LogWarning<EaterEatingState>(
                    "Planeta devorado não possui recurso configurado. Não foi possível verificar compatibilidade com o desejo.",
                    Behavior,
                    this);
                ResetCachedTargetResourceData();
                return;
            }

            PlanetResources desiredResource;
            bool hasDesiredResource;

            if (_hasEvaluatedDesiredResource)
            {
                desiredResource = _evaluatedDesiredResource;
                hasDesiredResource = true;
            }
            else
            {
                EaterDesireInfo desireInfo = Behavior.GetCurrentDesireInfo();
                hasDesiredResource = desireInfo.TryGetResource(out desiredResource);
            }

            if (!hasDesiredResource)
            {
                DebugUtility.Log<EaterEatingState>(
                    $"Planeta devorado possui recurso {planetResource}, porém o eater não tinha um desejo ativo para comparar.",
                    DebugUtility.Colors.Info,
                    Behavior,
                    this);
                ResetCachedTargetResourceData();
                return;
            }

            bool isCompatible = _hasCompatibilityResult
                ? _isTargetCompatible
                : planetResource == desiredResource;

            if (!_hasCompatibilityResult)
            {
                _isTargetCompatible = isCompatible;
                _hasCompatibilityResult = true;
                _hasEvaluatedPlanetResource = true;
                _hasEvaluatedDesiredResource = true;
                _evaluatedPlanetResource = planetResource;
                _evaluatedDesiredResource = desiredResource;
            }

            if (isCompatible)
            {
                DebugUtility.Log<EaterEatingState>(
                    $"Planeta devorado é compatível com o desejo atual. Desejo: {desiredResource}. Planeta: {planetResource}.",
                    DebugUtility.Colors.Success,
                    Behavior,
                    this);
            }
            else
            {
                DebugUtility.LogWarning<EaterEatingState>(
                    $"Planeta devorado NÃO é compatível com o desejo atual. Desejo: {desiredResource}. Planeta: {planetResource}.",
                    Behavior,
                    this);
            }

            TryApplyDevourReward(isCompatible);

            ResetCachedTargetResourceData();
        }

        private void TryReportDevouredPlanetOnTargetCleared()
        {
            if (_hasReportedDevouredCompatibility)
            {
                return;
            }

            TryResolveTargetCompatibility();

            if (_activePlanet == null)
            {
                if (_hasCachedTargetResource)
                {
                    ReportDevouredPlanetCompatibility();
                }

                return;
            }

            bool planetStillActive;

            try
            {
                planetStillActive = _activePlanet.IsActive;
            }
            catch (MissingReferenceException)
            {
                planetStillActive = false;
            }

            if (planetStillActive)
            {
                return;
            }

            ReportDevouredPlanetCompatibility();
        }

        private bool TryResolveTargetCompatibility()
        {
            if (_hasCompatibilityResult)
            {
                return true;
            }

            if (Behavior == null)
            {
                return false;
            }

            if (!TryGetTargetResourceType(out PlanetResources planetResource))
            {
                return false;
            }

            EaterDesireInfo desireInfo = Behavior.GetCurrentDesireInfo();
            if (!desireInfo.TryGetResource(out PlanetResources desiredResource))
            {
                return false;
            }

            _isTargetCompatible = planetResource == desiredResource;
            _hasCompatibilityResult = true;
            _hasEvaluatedPlanetResource = true;
            _hasEvaluatedDesiredResource = true;
            _evaluatedPlanetResource = planetResource;
            _evaluatedDesiredResource = desiredResource;
            return true;
        }

        private bool TryGetTargetResourceType(out PlanetResources resource)
        {
            if (_activePlanet != null)
            {
                try
                {
                    PlanetResourcesSo resourceAsset = _activePlanet.AssignedResource;
                    if (resourceAsset != null)
                    {
                        resource = resourceAsset.ResourceType;
                        _cachedTargetResourceAsset = resourceAsset;
                        _cachedTargetResourceType = resource;
                        _hasCachedTargetResource = true;
                        return true;
                    }
                }
                catch (MissingReferenceException)
                {
                    // Ignora e tenta usar o cache.
                }
            }

            if (_hasCachedTargetResource)
            {
                resource = _cachedTargetResourceType;
                return true;
            }

            resource = default;
            return false;
        }

        private void ResetCompatibility()
        {
            _hasCompatibilityResult = false;
            _isTargetCompatible = false;
            _hasEvaluatedDesiredResource = false;
            _hasEvaluatedPlanetResource = false;
            _evaluatedDesiredResource = default;
            _evaluatedPlanetResource = default;
        }

        private void CacheTargetResourceData(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return;
            }

            PlanetResourcesSo resourceAsset;

            try
            {
                resourceAsset = planet.AssignedResource;
            }
            catch (MissingReferenceException)
            {
                resourceAsset = null;
            }

            _cachedTargetResourceAsset = resourceAsset;

            if (resourceAsset != null)
            {
                _cachedTargetResourceType = resourceAsset.ResourceType;
                _hasCachedTargetResource = true;
            }
            else
            {
                _cachedTargetResourceType = default;
                _hasCachedTargetResource = false;
            }
        }

        private void ResetCachedTargetResourceData()
        {
            _cachedTargetResourceAsset = null;
            _cachedTargetResourceType = default;
            _hasCachedTargetResource = false;
        }

        private void TryApplyDevourReward(bool isCompatible)
        {
            if (_hasAppliedDevourReward)
            {
                return;
            }

            _hasAppliedDevourReward = true;

            if (!isCompatible)
            {
                return;
            }

            float healAmount = Config != null
                ? Config.EatingCompatibleDevourHealAmount
                : DefaultDevourHealAmount;

            if (healAmount <= Mathf.Epsilon || Behavior == null)
            {
                return;
            }

            bool restored = Behavior.TryApplySelfHealing(ResourceType.Health, healAmount);

            if (!Behavior.ShouldLogStateTransitions)
            {
                return;
            }

            if (restored)
            {
                DebugUtility.Log<EaterEatingState>(
                    $"Recuperação aplicada ao eater ao devorar planeta compatível: +{healAmount:0.##} {ResourceType.Health}.",
                    DebugUtility.Colors.Success,
                    Behavior,
                    this);
            }
            else
            {
                DebugUtility.LogWarning<EaterEatingState>(
                    $"Falha ao recuperar {ResourceType.Health} ao devorar planeta compatível. Valor esperado: {healAmount:0.##}.",
                    Behavior,
                    this);
            }
        }

        private void RequestWanderingTransition(string reason)
        {
            if (_pendingWanderingTransition)
            {
                return;
            }

            _pendingWanderingTransition = true;

            if (!Behavior.ShouldLogStateTransitions)
            {
                return;
            }

            DebugUtility.Log(
                $"Solicitando retorno ao estado de passeio: {reason}.",
                DebugUtility.Colors.CrucialInfo,
                Behavior,
                this);
        }

        private static IDamageReceiver ResolveDamageReceiver(Transform target)
        {
            return target != null ? target.GetComponentInParent<IDamageReceiver>() : null;
        }

        private static PlanetsMaster ResolvePlanetMaster(Transform target)
        {
            return target != null ? target.GetComponentInParent<PlanetsMaster>() : null;
        }
    }
}
