using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using UnityEngine;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.Actors.Impact.Runtime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/Actors/Impact/Actor Impact Endpoint")]
    public sealed class ActorImpactEndpoint : MonoBehaviour, IActorImpactEndpoint
    {
        [Header("Impact")]
        [Tooltip("Tipo semantico do impacto. Ex.: projectile, melee, hazard, body.")]
        [SerializeField] private string impactKind = "generic";

        [Tooltip("Layers aceitos como alvo de impacto.")]
        [SerializeField] private LayerMask targetLayerMask = ~0;

        [Tooltip("Exige que o alvo resolva um Actor valido antes de registrar impacto. Projetil de dano deve manter ativo para nao consumir lease em cenario/objetos sem Actor.")]
        [SerializeField] private bool requireTargetActorForRegisteredImpact = true;

        [Tooltip("Ignora impacto contra o proprio actor que carrega este endpoint.")]
        [SerializeField] private bool ignoreSelfActor = true;

        [Tooltip("Ignora impacto contra o actor owner que originou este runtime actor. Util para projeteis.")]
        [SerializeField] private bool ignoreOwnerActor = true;

        [Tooltip("Registra apenas o primeiro impacto ate nova configuracao/rent.")]
        [SerializeField] private bool registerSingleImpactUntilReconfigured = true;

        [Tooltip("Escuta OnTriggerEnter.")]
        [SerializeField] private bool detectTriggerEnter = true;

        [Tooltip("Escuta OnCollisionEnter.")]
        [SerializeField] private bool detectCollisionEnter = true;

        [Tooltip("Instala relays nos colliders filhos, incluindo colliders materializados pela Presentation.")]
        [SerializeField] private bool installColliderRelaysInChildren = true;

        [Header("Damage")]
        [Tooltip("Aplica dano automaticamente quando um impacto valido for registrado. Mantem impacto reutilizavel: sem adapter configurado, apenas registra o impacto.")]
        [SerializeField] private bool applyDamageOnRegisteredImpact = true;

        [Tooltip("Dano bruto aplicado quando o impacto registrado deve emitir dano.")]
        [SerializeField] private float impactDamageAmount = 25f;

        [Header("Effects")]
        [Tooltip("Publica um evento passivo para SFX/VFX de impacto antes de qualquer retorno/desmaterializacao tecnica.")]
        [SerializeField] private bool publishImpactEffectEvent = true;

        [Header("Return")]
        [Tooltip("Solicita retorno/desmaterializacao tecnica apos um impacto valido. O handler decide o efeito concreto; este endpoint nao chama PoolService.")]
        [SerializeField] private bool requestReturnAfterRegisteredImpact = true;

        [Tooltip("Quando damage application esta ativa, exige dano aplicado antes de solicitar retorno.")]
        [SerializeField] private bool requireDamageApplicationBeforeReturn = true;

        [Header("QA")]
        [Tooltip("Alvo opcional para QA/Impact/Emit Impact Against Qa Target.")]
        [SerializeField] private GameObject qaTargetOverride;

        private readonly IActorImpactTargetResolver _targetResolver = new ActorImpactTargetResolver();
        private readonly IActorImpactEffectEventStream _impactEffectEventStream = new ActorImpactEffectEventStream();
        private ActorId _ownerActorId;
        private ActorInstanceRuntimeId _ownerActorInstanceRuntimeId;
        private string _configuredImpactKind;
        private IActorImpactDamageApplicationAdapter _damageApplicationAdapter;
        private IActorImpactReturnHandler _returnHandler;
        private bool _impactRegisteredInCurrentLease;

        public ActorId ImpactActorId { get; private set; }
        public ActorInstanceRuntimeId ImpactActorInstanceRuntimeId { get; private set; }
        public ActorId OwnerActorId => _ownerActorId;
        public ActorInstanceRuntimeId OwnerActorInstanceRuntimeId => _ownerActorInstanceRuntimeId;
        public string ImpactKind => string.IsNullOrWhiteSpace(_configuredImpactKind) ? impactKind.TrimToEmpty() : _configuredImpactKind;
        public bool IsConfigured { get; private set; }

        public void Configure(
            ActorId impactActorId,
            ActorInstanceRuntimeId impactActorInstanceRuntimeId,
            ActorId ownerActorId,
            ActorInstanceRuntimeId ownerActorInstanceRuntimeId,
            string configuredImpactKind,
            string source,
            string reason)
        {
            if (!impactActorId.IsValid)
            {
                throw new InvalidOperationException("ActorImpactEndpoint requires a valid impact ActorId.");
            }

            if (!impactActorInstanceRuntimeId.IsValid)
            {
                throw new InvalidOperationException("ActorImpactEndpoint requires a valid impact ActorInstanceRuntimeId.");
            }

            string resolvedImpactKind = string.IsNullOrWhiteSpace(configuredImpactKind)
                ? impactKind.TrimToEmpty()
                : configuredImpactKind.TrimToEmpty();

            if (string.IsNullOrWhiteSpace(resolvedImpactKind))
            {
                throw new InvalidOperationException("ActorImpactEndpoint requires a valid impact kind.");
            }

            ImpactActorId = impactActorId;
            ImpactActorInstanceRuntimeId = impactActorInstanceRuntimeId;
            _ownerActorId = ownerActorId;
            _ownerActorInstanceRuntimeId = ownerActorInstanceRuntimeId;
            _configuredImpactKind = resolvedImpactKind;
            _impactRegisteredInCurrentLease = false;
            IsConfigured = true;

            ConfigureColliderRelays(source, reason);

            DebugUtility.LogVerbose(
                typeof(ActorImpactEndpoint),
                $"event='ActorImpactEndpointConfigured' impactActorId='{ImpactActorId}' impactActorInstanceRuntimeId='{ImpactActorInstanceRuntimeId}' ownerActorId='{_ownerActorId}' ownerActorInstanceRuntimeId='{_ownerActorInstanceRuntimeId}' impactKind='{ImpactKind}' targetLayerMask='{targetLayerMask.value}' requireTargetActorForRegisteredImpact='{requireTargetActorForRegisteredImpact}' ignoreSelfActor='{ignoreSelfActor}' ignoreOwnerActor='{ignoreOwnerActor}' singleImpact='{registerSingleImpactUntilReconfigured}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                DebugUtility.Colors.Info);

            DebugUtility.LogVerbose(
                typeof(ActorImpactEndpoint),
                $"event='ActorImpactEndpointReady' impactActorId='{ImpactActorId}' impactActorInstanceRuntimeId='{ImpactActorInstanceRuntimeId}' ownerActorId='{_ownerActorId}' ownerActorInstanceRuntimeId='{_ownerActorInstanceRuntimeId}' impactKind='{ImpactKind}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                DebugUtility.Colors.Success);
        }


        public void ConfigureDamageApplication(
            IActorImpactDamageApplicationAdapter damageApplicationAdapter,
            string source,
            string reason)
        {
            _damageApplicationAdapter = damageApplicationAdapter;

            DebugUtility.LogVerbose(
                typeof(ActorImpactEndpoint),
                $"event='ActorImpactDamageApplicationConfigured' impactActorId='{ImpactActorId}' impactActorInstanceRuntimeId='{ImpactActorInstanceRuntimeId}' ownerActorId='{_ownerActorId}' ownerActorInstanceRuntimeId='{_ownerActorInstanceRuntimeId}' adapterConfigured='{_damageApplicationAdapter != null && _damageApplicationAdapter.IsConfigured}' applyDamageOnRegisteredImpact='{applyDamageOnRegisteredImpact}' impactDamageAmount='{impactDamageAmount:0.###}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                _damageApplicationAdapter != null && _damageApplicationAdapter.IsConfigured ? DebugUtility.Colors.Success : DebugUtility.Colors.Info);
        }

        public void ConfigureReturnHandler(
            IActorImpactReturnHandler returnHandler,
            string source,
            string reason)
        {
            _returnHandler = returnHandler;

            DebugUtility.LogVerbose(
                typeof(ActorImpactEndpoint),
                $"event='ActorImpactReturnHandlerConfigured' impactActorId='{ImpactActorId}' impactActorInstanceRuntimeId='{ImpactActorInstanceRuntimeId}' ownerActorId='{_ownerActorId}' ownerActorInstanceRuntimeId='{_ownerActorInstanceRuntimeId}' handlerConfigured='{_returnHandler != null && _returnHandler.IsConfigured}' requestReturnAfterRegisteredImpact='{requestReturnAfterRegisteredImpact}' requireDamageApplicationBeforeReturn='{requireDamageApplicationBeforeReturn}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                _returnHandler != null && _returnHandler.IsConfigured ? DebugUtility.Colors.Success : DebugUtility.Colors.Info);
        }

        public bool TryRegisterImpactFromGameObject(
            GameObject targetObject,
            string source,
            string reason,
            out ActorImpactResult result)
        {
            var targetCollider = targetObject == null ? null : targetObject.GetComponentInChildren<Collider>(includeInactive: true);
            return TryRegisterImpact(targetObject, targetCollider, ActorImpactContact.None, source, reason, out result);
        }

        public bool TryRegisterImpactFromCollider(
            Collider targetCollider,
            string source,
            string reason,
            out ActorImpactResult result)
        {
            var targetObject = targetCollider == null ? null : targetCollider.gameObject;
            return TryRegisterImpact(targetObject, targetCollider, ActorImpactContact.None, source, reason, out result);
        }

        internal void NotifyTriggerEnterFromRelay(
            ActorImpactColliderRelay relay,
            Collider other,
            string source,
            string reason)
        {
            if (!detectTriggerEnter)
            {
                return;
            }

            TryRegisterImpactFromCollider(
                other,
                source,
                reason,
                out _);
        }

        internal void NotifyCollisionEnterFromRelay(
            ActorImpactColliderRelay relay,
            Collision collision,
            string source,
            string reason)
        {
            if (!detectCollisionEnter)
            {
                return;
            }

            var targetCollider = collision == null ? null : collision.collider;
            var targetObject = targetCollider == null ? null : targetCollider.gameObject;
            TryRegisterImpact(
                targetObject,
                targetCollider,
                ActorImpactContact.FromCollision(collision),
                source,
                reason,
                out _);
        }

        private void ConfigureColliderRelays(
            string source,
            string reason)
        {
            if (!installColliderRelaysInChildren)
            {
                DebugUtility.LogVerbose(
                    typeof(ActorImpactEndpoint),
                    $"event='ActorImpactColliderRelayConfigurationSkipped' impactActorId='{ImpactActorId}' impactActorInstanceRuntimeId='{ImpactActorInstanceRuntimeId}' reason='relay_install_disabled' source='{source.TrimToEmpty()}' triggerReason='{reason.TrimToEmpty()}'",
                    DebugUtility.Colors.Info);
                return;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>(includeInactive: true);
            int observedCount = colliders?.Length ?? 0;
            int configuredCount = 0;
            int skippedCount = 0;

            if (colliders != null)
            {
                for (int index = 0; index < colliders.Length; index++)
                {
                    var collider = colliders[index];
                    if (collider == null)
                    {
                        skippedCount++;
                        continue;
                    }

                    var nearestEndpoint = collider.GetComponentInParent<ActorImpactEndpoint>();
                    if (nearestEndpoint != this)
                    {
                        skippedCount++;
                        continue;
                    }

                    if (collider.gameObject == gameObject)
                    {
                        // O endpoint já recebe callbacks físicos do próprio GameObject.
                        skippedCount++;
                        continue;
                    }

                    var relay = collider.GetComponent<ActorImpactColliderRelay>();
                    if (relay == null)
                    {
                        relay = collider.gameObject.AddComponent<ActorImpactColliderRelay>();
                    }

                    relay.Configure(this, collider, source, reason);
                    configuredCount++;
                }
            }

            DebugUtility.LogVerbose(
                typeof(ActorImpactEndpoint),
                $"event='ActorImpactColliderRelaysConfigured' impactActorId='{ImpactActorId}' impactActorInstanceRuntimeId='{ImpactActorInstanceRuntimeId}' observedColliderCount='{observedCount}' configuredRelayCount='{configuredCount}' skippedColliderCount='{skippedCount}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                configuredCount > 0 ? DebugUtility.Colors.Success : DebugUtility.Colors.Info);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!detectTriggerEnter)
            {
                return;
            }

            TryRegisterImpactFromCollider(
                other,
                nameof(ActorImpactEndpoint),
                "trigger_enter",
                out _);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!detectCollisionEnter)
            {
                return;
            }

            var targetCollider = collision == null ? null : collision.collider;
            var targetObject = targetCollider == null ? null : targetCollider.gameObject;
            TryRegisterImpact(
                targetObject,
                targetCollider,
                ActorImpactContact.FromCollision(collision),
                nameof(ActorImpactEndpoint),
                "collision_enter",
                out _);
        }

        [ContextMenu("QA/Impact/Emit Impact Against Qa Target")]
        private void QaEmitImpactAgainstQaTarget()
        {
            TryRegisterImpactFromGameObject(
                qaTargetOverride,
                nameof(ActorImpactEndpoint),
                "qa_emit_impact_against_target",
                out _);
        }

        private bool TryRegisterImpact(
            GameObject targetObject,
            Collider targetCollider,
            ActorImpactContact contact,
            string source,
            string reason,
            out ActorImpactResult result)
        {
            if (!IsConfigured)
            {
                result = ActorImpactResult.Reject("impact_endpoint_not_configured");
                LogRejected(default, default, source, reason, result.Reason);
                return false;
            }

            if (registerSingleImpactUntilReconfigured && _impactRegisteredInCurrentLease)
            {
                result = ActorImpactResult.Reject("impact_already_registered_for_current_lease");
                LogRejected(default, default, source, reason, result.Reason);
                return false;
            }

            if (!IsLayerAllowed(targetObject))
            {
                result = ActorImpactResult.Reject("impact_target_layer_not_allowed");
                LogRejected(default, default, source, reason, result.Reason);
                return false;
            }

            _targetResolver.TryResolveImpactTarget(
                targetObject,
                targetCollider,
                source,
                reason,
                out var target);

            ActorImpactIntent intent = new(
                ImpactActorId,
                ImpactActorInstanceRuntimeId,
                _ownerActorId,
                _ownerActorInstanceRuntimeId,
                target.TargetActorId,
                target.TargetActorInstanceRuntimeId,
                ImpactKind,
                target.TargetObjectName,
                target.TargetColliderName,
                contact,
                source,
                reason);

            if (!intent.IsValid)
            {
                result = ActorImpactResult.Reject(intent, target, "impact_intent_invalid");
                LogRejected(intent, target, source, reason, result.Reason);
                return false;
            }

            if (requireTargetActorForRegisteredImpact && !target.HasTargetActor)
            {
                result = ActorImpactResult.Reject(intent, target, "impact_target_actor_missing_or_invalid");
                LogRejected(intent, target, source, reason, result.Reason);
                return false;
            }

            if (ShouldIgnoreTargetActor(target, out string ignoreReason))
            {
                result = ActorImpactResult.Reject(intent, target, ignoreReason);
                LogRejected(intent, target, source, reason, result.Reason);
                return false;
            }

            _impactRegisteredInCurrentLease = true;
            result = ActorImpactResult.RegisteredResult(intent, target);

            DebugUtility.LogVerbose(
                typeof(ActorImpactEndpoint),
                $"event='ActorImpactDetected' impactActorId='{intent.ImpactActorId}' impactActorInstanceRuntimeId='{intent.ImpactActorInstanceRuntimeId}' ownerActorId='{intent.OwnerActorId}' ownerActorInstanceRuntimeId='{intent.OwnerActorInstanceRuntimeId}' targetActorId='{intent.TargetActorId}' targetActorInstanceRuntimeId='{intent.TargetActorInstanceRuntimeId}' targetObject='{intent.TargetObjectName}' targetCollider='{intent.TargetColliderName}' impactKind='{intent.ImpactKind}' hasTargetActor='{target.HasTargetActor}' hasContact='{intent.Contact.HasContact}' contactPoint='{FormatVector(intent.Contact.Point)}' contactNormal='{FormatVector(intent.Contact.Normal)}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                DebugUtility.Colors.Success);

            DebugUtility.LogVerbose(
                typeof(ActorImpactEndpoint),
                $"event='ActorImpactRegistered' impactActorId='{intent.ImpactActorId}' impactActorInstanceRuntimeId='{intent.ImpactActorInstanceRuntimeId}' ownerActorId='{intent.OwnerActorId}' ownerActorInstanceRuntimeId='{intent.OwnerActorInstanceRuntimeId}' targetActorId='{intent.TargetActorId}' targetActorInstanceRuntimeId='{intent.TargetActorInstanceRuntimeId}' impactKind='{intent.ImpactKind}' hasTargetActor='{target.HasTargetActor}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                DebugUtility.Colors.Success);

            bool damageApplicationCompleted = TryApplyDamageFromRegisteredImpact(result, source, reason);
            PublishImpactEffectEventFromRegisteredImpact(result, damageApplicationCompleted, source, reason);
            TryRequestReturnFromRegisteredImpact(result, damageApplicationCompleted, source, reason);

            return true;
        }


        private bool TryApplyDamageFromRegisteredImpact(
            ActorImpactResult impactResult,
            string source,
            string reason)
        {
            if (!applyDamageOnRegisteredImpact)
            {
                DebugUtility.LogVerbose(
                    typeof(ActorImpactEndpoint),
                    $"event='ActorImpactDamageApplicationSkipped' impactActorId='{impactResult.Intent.ImpactActorId}' targetActorId='{impactResult.Intent.TargetActorId}' outcomeReason='impact_damage_application_disabled' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                    DebugUtility.Colors.Info);
                return false;
            }

            if (_damageApplicationAdapter == null || !_damageApplicationAdapter.IsConfigured)
            {
                DebugUtility.LogVerbose(
                    typeof(ActorImpactEndpoint),
                    $"event='ActorImpactDamageApplicationSkipped' impactActorId='{impactResult.Intent.ImpactActorId}' targetActorId='{impactResult.Intent.TargetActorId}' outcomeReason='impact_damage_application_adapter_missing_or_unconfigured' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                    DebugUtility.Colors.Warning);
                return false;
            }

            if (!_damageApplicationAdapter.TryApplyDamage(
                impactResult,
                impactDamageAmount,
                nameof(ActorImpactEndpoint),
                "impact_registered_damage_application",
                out var damageResult))
            {
                DebugUtility.LogVerbose(
                    typeof(ActorImpactEndpoint),
                    $"event='ActorImpactDamageApplicationFailed' impactActorId='{impactResult.Intent.ImpactActorId}' ownerActorId='{impactResult.Intent.OwnerActorId}' targetActorId='{impactResult.Intent.TargetActorId}' rawDamageAmount='{impactDamageAmount:0.###}' outcomeReason='{damageResult.Reason.TrimToEmpty()}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                    DebugUtility.Colors.Warning);
                return false;
            }

            DebugUtility.LogVerbose(
                typeof(ActorImpactEndpoint),
                $"event='ActorImpactDamageApplicationCompleted' impactActorId='{impactResult.Intent.ImpactActorId}' ownerActorId='{impactResult.Intent.OwnerActorId}' targetActorId='{impactResult.Intent.TargetActorId}' rawDamageAmount='{impactDamageAmount:0.###}' outcome='{damageResult.Outcome}' changedFact='{damageResult.HasChangedFact}' thresholdFacts='{damageResult.HasThresholdFacts}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                DebugUtility.Colors.Success);

            return true;
        }


        private void PublishImpactEffectEventFromRegisteredImpact(
            ActorImpactResult impactResult,
            bool damageApplicationCompleted,
            string source,
            string reason)
        {
            if (!publishImpactEffectEvent)
            {
                DebugUtility.LogVerbose(
                    typeof(ActorImpactEndpoint),
                    $"event='ActorImpactEffectEventSkipped' impactActorId='{impactResult.Intent.ImpactActorId}' ownerActorId='{impactResult.Intent.OwnerActorId}' targetActorId='{impactResult.Intent.TargetActorId}' outcomeReason='impact_effect_event_disabled' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                    DebugUtility.Colors.Info);
                return;
            }

            bool returnRequestEligible = IsReturnRequestEligible(damageApplicationCompleted);
            var effectEvent = new ActorImpactEffectEvent(
                impactResult,
                impactDamageAmount,
                applyDamageOnRegisteredImpact,
                damageApplicationCompleted,
                returnRequestEligible,
                nameof(ActorImpactEndpoint),
                "impact_effect_requested_before_return");

            _impactEffectEventStream.Publish(effectEvent);
        }

        private bool IsReturnRequestEligible(bool damageApplicationCompleted)
        {
            if (!requestReturnAfterRegisteredImpact)
            {
                return false;
            }

            if (_returnHandler == null || !_returnHandler.IsConfigured)
            {
                return false;
            }

            return !requireDamageApplicationBeforeReturn ||
                !applyDamageOnRegisteredImpact ||
                damageApplicationCompleted;
        }

        private void TryRequestReturnFromRegisteredImpact(
            ActorImpactResult impactResult,
            bool damageApplicationCompleted,
            string source,
            string reason)
        {
            if (!requestReturnAfterRegisteredImpact)
            {
                DebugUtility.LogVerbose(
                    typeof(ActorImpactEndpoint),
                    $"event='ActorImpactReturnSkipped' impactActorId='{impactResult.Intent.ImpactActorId}' ownerActorId='{impactResult.Intent.OwnerActorId}' targetActorId='{impactResult.Intent.TargetActorId}' outcomeReason='impact_return_disabled' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                    DebugUtility.Colors.Info);
                return;
            }

            if (_returnHandler == null || !_returnHandler.IsConfigured)
            {
                DebugUtility.LogVerbose(
                    typeof(ActorImpactEndpoint),
                    $"event='ActorImpactReturnSkipped' impactActorId='{impactResult.Intent.ImpactActorId}' ownerActorId='{impactResult.Intent.OwnerActorId}' targetActorId='{impactResult.Intent.TargetActorId}' outcomeReason='impact_return_handler_missing_or_unconfigured' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                    DebugUtility.Colors.Info);
                return;
            }

            if (requireDamageApplicationBeforeReturn && applyDamageOnRegisteredImpact && !damageApplicationCompleted)
            {
                DebugUtility.LogVerbose(
                    typeof(ActorImpactEndpoint),
                    $"event='ActorImpactReturnSkipped' impactActorId='{impactResult.Intent.ImpactActorId}' ownerActorId='{impactResult.Intent.OwnerActorId}' targetActorId='{impactResult.Intent.TargetActorId}' outcomeReason='damage_application_not_completed' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                    DebugUtility.Colors.Warning);
                return;
            }

            DebugUtility.LogVerbose(
                typeof(ActorImpactEndpoint),
                $"event='ActorImpactReturnRequested' impactActorId='{impactResult.Intent.ImpactActorId}' ownerActorId='{impactResult.Intent.OwnerActorId}' targetActorId='{impactResult.Intent.TargetActorId}' source='{nameof(ActorImpactEndpoint)}' reason='impact_registered_return_request'",
                DebugUtility.Colors.Info);

            if (!_returnHandler.TryRequestReturn(
                impactResult,
                nameof(ActorImpactEndpoint),
                "impact_registered_return_request",
                out string returnReason))
            {
                DebugUtility.LogVerbose(
                    typeof(ActorImpactEndpoint),
                    $"event='ActorImpactReturnFailed' impactActorId='{impactResult.Intent.ImpactActorId}' ownerActorId='{impactResult.Intent.OwnerActorId}' targetActorId='{impactResult.Intent.TargetActorId}' outcomeReason='{returnReason.TrimToEmpty()}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                    DebugUtility.Colors.Warning);
                return;
            }

            DebugUtility.LogVerbose(
                typeof(ActorImpactEndpoint),
                $"event='ActorImpactReturnCompleted' impactActorId='{impactResult.Intent.ImpactActorId}' ownerActorId='{impactResult.Intent.OwnerActorId}' targetActorId='{impactResult.Intent.TargetActorId}' outcomeReason='{returnReason.TrimToEmpty()}' source='{nameof(ActorImpactEndpoint)}' reason='impact_registered_return_request'",
                DebugUtility.Colors.Success);
        }

        private bool IsLayerAllowed(GameObject targetObject)
        {
            if (targetObject == null)
            {
                return true;
            }

            int targetLayer = targetObject.layer;
            int mask = targetLayerMask.value;
            return (mask & 1 << targetLayer) != 0;
        }

        private bool ShouldIgnoreTargetActor(
            ActorImpactTarget target,
            out string reason)
        {
            reason = string.Empty;
            if (!target.HasTargetActor)
            {
                return false;
            }

            if (ignoreSelfActor &&
                target.TargetActorId == ImpactActorId &&
                target.TargetActorInstanceRuntimeId == ImpactActorInstanceRuntimeId)
            {
                reason = "impact_target_is_self_actor";
                return true;
            }

            if (ignoreOwnerActor &&
                _ownerActorId.IsValid &&
                _ownerActorInstanceRuntimeId.IsValid &&
                target.TargetActorId == _ownerActorId &&
                target.TargetActorInstanceRuntimeId == _ownerActorInstanceRuntimeId)
            {
                reason = "impact_target_is_owner_actor";
                return true;
            }

            return false;
        }

        private void LogRejected(
            ActorImpactIntent intent,
            ActorImpactTarget target,
            string source,
            string reason,
            string outcomeReason)
        {
            DebugUtility.LogVerbose(
                typeof(ActorImpactEndpoint),
                $"event='ActorImpactRejected' impactActorId='{intent.ImpactActorId}' impactActorInstanceRuntimeId='{intent.ImpactActorInstanceRuntimeId}' ownerActorId='{intent.OwnerActorId}' ownerActorInstanceRuntimeId='{intent.OwnerActorInstanceRuntimeId}' targetActorId='{target.TargetActorId}' targetActorInstanceRuntimeId='{target.TargetActorInstanceRuntimeId}' targetObject='{target.TargetObjectName}' targetCollider='{target.TargetColliderName}' impactKind='{ImpactKind}' outcomeReason='{outcomeReason.TrimToEmpty()}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                DebugUtility.Colors.Warning);
        }

        private static string FormatVector(Vector3 value)
        {
            return $"{value.x:0.###},{value.y:0.###},{value.z:0.###}";
        }

        private void OnValidate()
        {
            if (impactDamageAmount < 0f || float.IsNaN(impactDamageAmount) || float.IsInfinity(impactDamageAmount))
            {
                impactDamageAmount = 0f;
            }
        }

    }
}
