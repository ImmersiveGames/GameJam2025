using DG.Tweening;
using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Configs;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
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
        private const RuntimeAttributeType DefaultRecoveryResource = RuntimeAttributeType.Health;
        private const float IncompatibleRecoveryMultiplier = 0.5f;
        private const float DefaultDevourHealAmount = 25f;

        private Tween _approachTween;
        private Tween _orbitTween;
        private Transform _currentTarget;
        private Vector3 _radialBasis;
        private float _currentAngle;
        private PlanetOrbitFreezeController _orbitFreezeController;
        private float _currentOrbitRadius;
        private PlanetsMaster _activePlanet;
        private IDamageReceiver _currentDamageReceiver;
        private float _damageTimer;
        private float _recoveryTimer;
        private bool _pendingWanderingTransition;
        private bool _missingDamageReceiverLogged;
        private bool _hasReportedDestroyedPlanet;
        private bool _hasReportedDevouredCompatibility;
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
        private bool _hasLoggedRecoveryCompatibility;
        private bool _planetDestroyedDuringState;

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
            _damageTimer = 0f;
            _recoveryTimer = 0f;
            _hasReportedDestroyedPlanet = false;
            _hasReportedDevouredCompatibility = false;
            _missingDamageReceiverLogged = false;
            _currentDamageReceiver = null;
            ResetCompatibility();
            _missingAudioEmitterLogged = false;
            _hasAppliedDevourReward = false;
            _hasLoggedRecoveryCompatibility = false;
            _planetDestroyedDuringState = false;

            TrySetEatingAnimation(true);

            EnsureOrbitFreezeController().TryFreeze(Behavior, Behavior.CurrentTargetPlanet);
            PrepareTarget(Behavior.CurrentTargetPlanet, restartOrbit: true);
        }

        public override void OnExit()
        {
            TrySetEatingAnimation(false);

            base.OnExit();
            StopTweens();
            EnsureOrbitFreezeController().Release();
            UpdatePlanetMaster(null);

            _currentDamageReceiver = null;
            _currentTarget = null;
            _pendingWanderingTransition = false;
            _missingDamageReceiverLogged = false;
            _hasReportedDevouredCompatibility = false;
            _cachedTargetResourceType = default;
            _hasCachedTargetResource = false;
            _recoveryTimer = 0f;
            ResetCompatibility();
            _missingAudioEmitterLogged = false;
            _hasAppliedDevourReward = false;
            _hasLoggedRecoveryCompatibility = false;
        }

        public override void Update()
        {
            base.Update();

            if (_pendingWanderingTransition)
            {
                return;
            }

            var target = Behavior.CurrentTargetPlanet;
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
            TickDamage(Time.deltaTime);
            TickRecovery(Time.deltaTime);
        }

        internal void SyncDestroyedTargetForTransitions()
        {
            if (_pendingWanderingTransition || _planetDestroyedDuringState)
            {
                return;
            }

            if (_activePlanet == null)
            {
                return;
            }

            bool planetActive;
            try
            {
                planetActive = _activePlanet.IsActive;
            }
            catch (MissingReferenceException)
            {
                planetActive = false;
            }

            if (!planetActive)
            {
                HandleDestroyedPlanet();
            }
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
                _hasLoggedRecoveryCompatibility = false;
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
                _damageTimer = 0f;
                _recoveryTimer = 0f;
                _missingDamageReceiverLogged = false;
                ResetCompatibility();
                return;
            }

            _currentTarget = target;
            var resolvedPlanet = ResolvePlanetMaster(_currentTarget);
            UpdatePlanetMaster(resolvedPlanet);
            _currentDamageReceiver = ResolveDamageReceiver(_currentTarget);
            _missingDamageReceiverLogged = false;
            _damageTimer = 0f;
            _recoveryTimer = 0f;

            if (targetChanged)
            {
                ResetCompatibility();
            }

            TryResolveTargetCompatibility();
            ReportDesiredResourceMatch();

            _radialBasis = ResolveRadialBasis();
            _currentOrbitRadius = ResolveOrbitRadius(_currentTarget);
            var desiredPosition = _currentTarget.position + _radialBasis * _currentOrbitRadius;
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
                DebugUtility.LogVerbose(
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

            var axis = ResolveOrbitAxis();
            var radial = Vector3.ProjectOnPlane(_radialBasis, axis);
            if (radial.sqrMagnitude <= Mathf.Epsilon)
            {
                radial = ResolveRadialBasis();
            }
            radial = radial.normalized;
            _radialBasis = radial;
            var tangent = Vector3.Cross(axis, radial).normalized;

            float radians = angle * Mathf.Deg2Rad;
            var offset = (radial * Mathf.Cos(radians) + tangent * Mathf.Sin(radians)) * _currentOrbitRadius;
            Transform.position = _currentTarget.position + offset;
        }

        private Vector3 ResolveRadialBasis()
        {
            if (_currentTarget == null)
            {
                return Transform.forward.sqrMagnitude > Mathf.Epsilon ? Transform.forward.normalized : Vector3.forward;
            }

            var axis = ResolveOrbitAxis();
            var offset = Transform.position - _currentTarget.position;
            var planarOffset = Vector3.ProjectOnPlane(offset, axis);
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

        private void TrySetEatingAnimation(bool isEating)
        {
            if (Behavior == null || !Behavior.TryGetAnimationController(out var controller))
            {
                return;
            }

            DebugUtility.LogVerbose<EaterEatingState>(
                $"Alterando animação de alimentação para {isEating} (controller={controller != null}, actorId={Behavior?.Master?.ActorId}).");
            controller.SetEating(isEating);
        }

        private void TickDamage(float deltaTime)
        {
            TryResolveTargetCompatibility();

            float interval = Config != null ? Config.EatingDamageInterval : DefaultDamageInterval;
            if (interval <= Mathf.Epsilon)
            {
                interval = DefaultDamageInterval;
            }

            _damageTimer += Mathf.Max(deltaTime, 0f);
            if (_damageTimer < interval)
            {
                return;
            }

            while (_damageTimer >= interval)
            {
                if (!TryApplyBiteDamage())
                {
                    _damageTimer = 0f;
                    return;
                }

                _damageTimer -= interval;
            }
        }

        private void TickRecovery(float deltaTime)
        {
            if (!TryGetRecoveryConfig(out var resourceType, out float recoveryAmount, out float recoveryInterval))
            {
                return;
            }

            _recoveryTimer += Mathf.Max(deltaTime, 0f);
            if (_recoveryTimer < recoveryInterval)
            {
                return;
            }

            bool hasCompatibility = TryEvaluateCompatibility(out bool isCompatible);
            float multiplier = hasCompatibility && isCompatible ? 1f : IncompatibleRecoveryMultiplier;
            float adjustedAmount = recoveryAmount * multiplier;
            if (adjustedAmount <= Mathf.Epsilon)
            {
                _recoveryTimer = 0f;
                return;
            }

            if (Behavior != null && Behavior.ShouldLogStateTransitions && !_hasLoggedRecoveryCompatibility)
            {
                string status = hasCompatibility
                    ? (isCompatible ? "compatível" : "incompatível")
                    : "sem avaliação de compatibilidade";

                DebugUtility.LogVerbose(
                    $"Recuperação durante alimentação ({status}). Quantidade aplicada por ciclo: {adjustedAmount:0.##} {resourceType}.",
                    hasCompatibility && isCompatible ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning,
                    Behavior,
                    this);

                _hasLoggedRecoveryCompatibility = true;
            }

            while (_recoveryTimer >= recoveryInterval)
            {
                if (!TryApplyRecovery(resourceType, adjustedAmount))
                {
                    _recoveryTimer = 0f;
                    return;
                }

                _recoveryTimer -= recoveryInterval;
            }
        }

        private bool TryApplyBiteDamage()
        {
            var receiver = _currentDamageReceiver ??= ResolveDamageReceiver(_currentTarget);
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
            var resource = Config != null ? Config.EatingDamageRuntimeAttribute : RuntimeAttributeType.Health;
            var damageType = Config != null ? Config.EatingDamageType : DamageType.Physical;
            var hitPosition = _currentTarget.position;

            var context = new DamageContext(attackerId, targetId, damage, resource, damageType, hitPosition);
            receiver.ReceiveDamage(context);

            if (Master != null && _activePlanet != null)
            {
                Master.OnEventEaterBite(_activePlanet);
            }

            TryPlayBiteSound();

            return true;
        }

        private bool TryApplyRecovery(RuntimeAttributeType runtimeAttributeType, float amount)
        {
            if (Behavior == null)
            {
                return false;
            }

            return Behavior.TryRestoreResource(runtimeAttributeType, amount);
        }

        private bool TryGetRecoveryConfig(out RuntimeAttributeType runtimeAttributeType, out float amount, out float interval)
        {
            runtimeAttributeType = Config != null ? Config.EatingRecoveryRuntimeAttribute : DefaultRecoveryResource;
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
            var biteSound = Config != null ? Config.EatingBiteSound : null;
            if (biteSound == null || biteSound.clip == null)
            {
                return;
            }

            if (Behavior == null)
            {
                return;
            }

            if (!Behavior.TryGetAudioEmitter(out var emitter))
            {
                if (!_missingAudioEmitterLogged && Behavior.ShouldLogStateTransitions)
                {
                    DebugUtility.LogWarning(
                        "EntityAudioEmitter não encontrado para tocar som de mordida.",
                        Behavior,
                        this);
                    _missingAudioEmitterLogged = true;
                }

                return;
            }

            _missingAudioEmitterLogged = false;

            var position = _currentTarget != null ? _currentTarget.position : Transform.position;
            var context = AudioContext.Default(position, emitter.UsesSpatialBlend);
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
                DebugUtility.LogVerbose(
                    "Planeta consumido foi destruído. Retornando ao estado de passeio.",
                    DebugUtility.Colors.CrucialInfo,
                    Behavior,
                    this);
                _hasReportedDestroyedPlanet = true;
            }

            Behavior?.ClearOrbitAnchor(_currentTarget);
            UpdatePlanetMaster(null);
            _currentDamageReceiver = null;
            _damageTimer = 0f;
            _missingDamageReceiverLogged = false;
            _currentTarget = null;
            _recoveryTimer = 0f;
            ResetCompatibility();

            Behavior?.EndDesires("EatingState.PlanetDestroyed");

            RequestWanderingTransition("PlanetDestroyed");

            StopTweens();
            EnsureOrbitFreezeController().Release();

            _planetDestroyedDuringState = true;
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
                DebugUtility.LogWarning(
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
                var desireInfo = Behavior.GetCurrentDesireInfo();
                hasDesiredResource = desireInfo.TryGetResource(out desiredResource);
            }

            if (!hasDesiredResource)
            {
                DebugUtility.LogVerbose(
                    $"Planeta devorado possui recurso {planetResource}, porém o eater não tinha um desejo ativo para comparar.",
                    DebugUtility.Colors.Warning,
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
                DebugUtility.LogVerbose(
                    $"Planeta devorado é compatível com o desejo atual. Desejo: {desiredResource}. Planeta: {planetResource}.",
                    DebugUtility.Colors.Success,
                    Behavior,
                    this);
            }
            else
            {
                DebugUtility.LogVerbose(
                    $"Planeta devorado NÃO é compatível com o desejo atual. Desejo: {desiredResource}. Planeta: {planetResource}.",
                    DebugUtility.Colors.Warning,
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

            if (!TryGetTargetResourceType(out var planetResource))
            {
                return false;
            }

            var desireInfo = Behavior.GetCurrentDesireInfo();
            if (!desireInfo.TryGetResource(out var desiredResource))
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
                    var resourceAsset = _activePlanet.AssignedResource;
                    if (resourceAsset != null)
                    {
                        resource = resourceAsset.ResourceType;
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

        private void ReportDesiredResourceMatch()
        {
            if (Behavior == null || !Behavior.ShouldLogStateTransitions)
            {
                return;
            }

            if (!TryGetTargetResourceType(out var planetResource))
            {
                DebugUtility.LogWarning(
                    "Não foi possível identificar o recurso do planeta enquanto iniciava a alimentação.",
                    Behavior,
                    this);
                return;
            }

            var desireInfo = Behavior.GetCurrentDesireInfo();
            if (!desireInfo.TryGetResource(out var desiredResource))
            {
                DebugUtility.LogWarning(
                    "Nenhum desejo de recurso definido ao começar a comer.",
                    Behavior,
                    this);
                return;
            }

            bool matches = planetResource == desiredResource;
            string message = matches
                ? $"Recurso do planeta compatível com o desejo atual: {planetResource}."
                : $"Recurso do planeta ({planetResource}) difere do desejo atual ({desiredResource}).";

            DebugUtility.LogVerbose(
                message,
                matches ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning,
                Behavior,
                this);
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

            bool restored = Behavior.TryApplySelfHealing(RuntimeAttributeType.Health, healAmount);

            if (!Behavior.ShouldLogStateTransitions)
            {
                return;
            }

            if (restored)
            {
                DebugUtility.LogVerbose(
                    $"Recuperação aplicada ao eater ao devorar planeta compatível: +{healAmount:0.##} {RuntimeAttributeType.Health}.",
                    DebugUtility.Colors.Success,
                    Behavior,
                    this);
            }
            else
            {
                DebugUtility.LogWarning(
                    $"Falha ao recuperar {RuntimeAttributeType.Health} ao devorar planeta compatível. Valor esperado: {healAmount:0.##}.",
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

            DebugUtility.LogVerbose(
                $"Solicitando retorno ao estado de passeio: {reason}.",
                DebugUtility.Colors.CrucialInfo,
                Behavior,
                this);
        }

        internal bool HasDestroyedPlanetDuringState => _planetDestroyedDuringState;

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
