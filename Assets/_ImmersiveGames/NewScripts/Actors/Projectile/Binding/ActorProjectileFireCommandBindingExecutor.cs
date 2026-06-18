using System;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Damage.Runtime;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Projectile.Audio;
using _ImmersiveGames.NewScripts.Actors.Projectile.Contracts;
using _ImmersiveGames.NewScripts.Actors.Projectile.Runtime;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Core;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Projectile.Binding
{
    public readonly struct ActorProjectileFireCommandBindingContext
    {
        public ActorProjectileFireCommandBindingContext(
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            string participantId,
            bool required,
            string source,
            string reason)
        {
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            ParticipantId = participantId.TrimToEmpty();
            Required = required;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public string ParticipantId { get; }
        public bool Required { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            !string.IsNullOrWhiteSpace(Source);
}

    public enum ActorProjectileFireCommandBindingState
    {
        Unknown = 0,
        Executable = 1,
        SkippedOptional = 2
    }

    public readonly struct ActorProjectileFireCommandBindingResult
    {
        public ActorProjectileFireCommandBindingResult(
            ActorProjectileFireCommandBindingState state,
            string observedEndpoint)
        {
            State = state;
            ObservedEndpoint = observedEndpoint.TrimToEmpty();
        }

        public ActorProjectileFireCommandBindingState State { get; }
        public string ObservedEndpoint { get; }
        public bool IsExecutable => State == ActorProjectileFireCommandBindingState.Executable;
        public bool Skipped => State == ActorProjectileFireCommandBindingState.SkippedOptional;
        public bool IsValid => State != ActorProjectileFireCommandBindingState.Unknown && !string.IsNullOrWhiteSpace(ObservedEndpoint);
}

    public sealed class ActorProjectileFireCommandBindingExecutor
    {
        private const string AdapterIdPrefix = "actor.projectile.spawn.adapter.pooled";

        private readonly IPoolService _poolService;
        private readonly IGlobalAudioService _globalAudioService;
        private readonly IActorAttributeEventStream _actorAttributeEventStream;

        public ActorProjectileFireCommandBindingExecutor(
            IPoolService poolService,
            IGlobalAudioService globalAudioService,
            IActorAttributeEventStream actorAttributeEventStream)
        {
            _poolService = poolService ?? throw new ArgumentNullException(nameof(poolService));
            _globalAudioService = globalAudioService ?? throw new ArgumentNullException(nameof(globalAudioService));
            _actorAttributeEventStream = actorAttributeEventStream ?? throw new ArgumentNullException(nameof(actorAttributeEventStream));
        }

        public ActorProjectileFireCommandBindingResult Execute(
            ActorProjectileFireCommandBindingContext context,
            IActorCommandSourceHub commandHub,
            IActorProjectileFireEndpoint projectileFireEndpoint)
        {
            if (!context.IsValid)
            {
                throw new InvalidOperationException("ActorProjectileFireCommandBindingContext is invalid.");
            }

            if (commandHub == null)
            {
                throw new InvalidOperationException($"Actor projectile fire command binding failed: actorId='{context.ActorId}' participantId='{context.ParticipantId}' missing Actor command input hub.");
            }

            if (!commandHub.IsPrepared)
            {
                if (context.Required)
                {
                    throw new InvalidOperationException($"Actor projectile fire command binding failed: actorId='{context.ActorId}' participantId='{context.ParticipantId}' actor command hub is not prepared.");
                }

                DebugUtility.LogVerbose(
                    typeof(ActorProjectileFireCommandBindingExecutor),
                    $"event='ActorCommandBindingSkipped' actorId='{context.ActorId}' actorInstanceRuntimeId='{context.ActorInstanceRuntimeId}' commandId='FirePrimary' state='SkippedOptional' sink='none' source='{nameof(ActorProjectileFireCommandBindingExecutor)}' reason='optional_hub_not_prepared'.",
                    DebugUtility.Colors.Info);

                return new ActorProjectileFireCommandBindingResult(
                    ActorProjectileFireCommandBindingState.SkippedOptional,
                    "optional_hub_not_prepared");
            }

            if (!commandHub.HasBinding(ActorCommandId.FirePrimary, ActorCommandTriggerKind.Pressed))
            {
                if (context.Required)
                {
                    throw new InvalidOperationException($"Actor projectile fire command binding failed: actorId='{context.ActorId}' participantId='{context.ParticipantId}' missing active FirePrimary binding on ActorCommandSourceHub.");
                }

                DebugUtility.LogVerbose(
                    typeof(ActorProjectileFireCommandBindingExecutor),
                    $"event='ActorCommandBindingSkipped' actorId='{context.ActorId}' actorInstanceRuntimeId='{context.ActorInstanceRuntimeId}' commandId='FirePrimary' state='SkippedOptional' sink='none' source='{nameof(ActorProjectileFireCommandBindingExecutor)}' reason='optional_fireprimary_binding_missing'.",
                    DebugUtility.Colors.Info);

                return new ActorProjectileFireCommandBindingResult(
                    ActorProjectileFireCommandBindingState.SkippedOptional,
                    "optional_fireprimary_binding_missing");
            }

            DebugUtility.LogVerbose(
                typeof(ActorProjectileFireCommandBindingExecutor),
                $"event='ActorCommandBindingReadinessObserved' actorId='{context.ActorId}' actorInstanceRuntimeId='{context.ActorInstanceRuntimeId}' participantId='{context.ParticipantId}' commandId='FirePrimary' state='Prepared' hubType='{commandHub.GetType().Name}' hubPrepared='{commandHub.IsPrepared}' source='{context.Source}' reason='{context.Reason}'.",
                DebugUtility.Colors.Info);

            if (projectileFireEndpoint == null)
            {
                if (context.Required)
                {
                    throw new InvalidOperationException($"Actor projectile fire command binding failed: actorId='{context.ActorId}' participantId='{context.ParticipantId}' missing ActorProjectileFireEndpoint.");
                }

                DebugUtility.LogVerbose(
                    typeof(ActorProjectileFireCommandBindingExecutor),
                    $"event='ActorProjectileFireEndpointSkipped' actorId='{context.ActorId}' actorInstanceRuntimeId='{context.ActorInstanceRuntimeId}' participantId='{context.ParticipantId}' commandId='FirePrimary' state='SkippedOptional' reason='optional_projectile_fire_endpoint_missing' source='{nameof(ActorProjectileFireCommandBindingExecutor)}'.",
                    DebugUtility.Colors.Info);

                return new ActorProjectileFireCommandBindingResult(
                    ActorProjectileFireCommandBindingState.SkippedOptional,
                    "optional_projectile_fire_endpoint_missing");
            }

            if (!projectileFireEndpoint.TryGetReadiness(ActorCommandId.FirePrimary, out var readiness) || !readiness.IsPrepared)
            {
                if (context.Required || projectileFireEndpoint.IsRequired)
                {
                    throw new InvalidOperationException($"Actor projectile fire command binding failed: actorId='{context.ActorId}' participantId='{context.ParticipantId}' projectile fire endpoint is not ready. state='{readiness.Kind}' reason='{readiness.Reason}'.");
                }

                DebugUtility.LogVerbose(
                    typeof(ActorProjectileFireCommandBindingExecutor),
                    $"event='ActorProjectileFireEndpointReadinessObserved' actorId='{context.ActorId}' actorInstanceRuntimeId='{context.ActorInstanceRuntimeId}' participantId='{context.ParticipantId}' commandId='FirePrimary' endpointId='{projectileFireEndpoint.EndpointId}' profileId='{projectileFireEndpoint.ProfileId}' fireModeId='{projectileFireEndpoint.DefaultFireModeId}' state='{readiness.Kind}' readinessAccepted='False' source='{context.Source}' reason='{readiness.Reason}'.",
                    DebugUtility.Colors.Info);

                return new ActorProjectileFireCommandBindingResult(
                    ActorProjectileFireCommandBindingState.SkippedOptional,
                    $"{projectileFireEndpoint.GetType().Name}|state={readiness.Kind}|reason={readiness.Reason}");
            }

            DebugUtility.LogVerbose(
                typeof(ActorProjectileFireCommandBindingExecutor),
                $"event='ActorProjectileFireEndpointReadinessObserved' actorId='{context.ActorId}' actorInstanceRuntimeId='{context.ActorInstanceRuntimeId}' participantId='{context.ParticipantId}' commandId='FirePrimary' endpointId='{projectileFireEndpoint.EndpointId}' profileId='{projectileFireEndpoint.ProfileId}' fireModeId='{projectileFireEndpoint.DefaultFireModeId}' state='{readiness.Kind}' readinessAccepted='True' source='{context.Source}' reason='{context.Reason}'.",
                DebugUtility.Colors.Success);

            projectileFireEndpoint.ConfigureSpawnRuntimeStatePoolService(
                _poolService,
                nameof(ActorProjectileFireCommandBindingExecutor),
                "projectile_spawn_runtime_state_pool_service_configured_by_actor_projectile_binding");

            string adapterId = BuildAdapterId(context, projectileFireEndpoint);
            ActorDamageSourceEndpoint damageSourceEndpoint = ResolveDamageSourceEndpoint(projectileFireEndpoint);
            IActorProjectileSpawnAdapter spawnAdapter = new PooledActorProjectileSpawnAdapter(
                adapterId,
                _poolService,
                _actorAttributeEventStream,
                damageSourceEndpoint);
            projectileFireEndpoint.ConfigureSpawnAdapter(
                spawnAdapter,
                nameof(ActorProjectileFireCommandBindingExecutor),
                "projectile_spawn_adapter_configured_by_actor_projectile_binding");

            projectileFireEndpoint.ConfigureFireAudioAdapter(
                new ActorProjectileFireAudioAdapter(_globalAudioService),
                nameof(ActorProjectileFireCommandBindingExecutor),
                "projectile_fire_audio_adapter_configured_by_actor_projectile_binding");

            commandHub.BindCommandSink(ActorCommandId.FirePrimary, projectileFireEndpoint);

            DebugUtility.Log(
                typeof(ActorProjectileFireCommandBindingExecutor),
                $"event='ActorCommandSinkBound' actorId='{context.ActorId}' actorInstanceRuntimeId='{context.ActorInstanceRuntimeId}' participantId='{context.ParticipantId}' commandId='FirePrimary' state='Executable' sink='{projectileFireEndpoint.GetType().Name}' endpointId='{projectileFireEndpoint.EndpointId}' profileId='{projectileFireEndpoint.ProfileId}' fireModeId='{projectileFireEndpoint.DefaultFireModeId}' adapter='{projectileFireEndpoint.SpawnAdapterName}' source='{nameof(ActorProjectileFireCommandBindingExecutor)}' reason='projectile_fire_spawn_adapter_sink_bound'.",
                DebugUtility.Colors.Success);

            return new ActorProjectileFireCommandBindingResult(
                ActorProjectileFireCommandBindingState.Executable,
                $"{projectileFireEndpoint.GetType().Name}|endpointId={projectileFireEndpoint.EndpointId}|profileId={projectileFireEndpoint.ProfileId}|fireModeId={projectileFireEndpoint.DefaultFireModeId}|spawnAdapter={projectileFireEndpoint.SpawnAdapterName}");
        }


        private static ActorDamageSourceEndpoint ResolveDamageSourceEndpoint(
            IActorProjectileFireEndpoint projectileFireEndpoint)
        {
            if (projectileFireEndpoint is not Component component)
            {
                return null;
            }

            Actor actorRoot = component.GetComponentInParent<Actor>(includeInactive: true);
            if (actorRoot != null &&
                actorRoot.CapabilitySurface != null &&
                actorRoot.CapabilitySurface.TryGetEndpoint(out ActorDamageSourceEndpoint surfaceEndpoint) &&
                surfaceEndpoint != null)
            {
                return surfaceEndpoint;
            }

            ActorDamageSourceEndpoint localEndpoint = component.GetComponent<ActorDamageSourceEndpoint>();
            if (localEndpoint != null)
            {
                return localEndpoint;
            }

            return component.GetComponentInParent<ActorDamageSourceEndpoint>(includeInactive: true)
                ?? component.GetComponentInChildren<ActorDamageSourceEndpoint>(includeInactive: true);
        }

        private static string BuildAdapterId(
            ActorProjectileFireCommandBindingContext context,
            IActorProjectileFireEndpoint projectileFireEndpoint)
        {
            string endpointId = NormalizeAdapterSegment(projectileFireEndpoint.EndpointId.ToString());
            if (!string.IsNullOrWhiteSpace(endpointId))
            {
                return $"{AdapterIdPrefix}.{endpointId}";
            }

            string profileId = NormalizeAdapterSegment(projectileFireEndpoint.ProfileId.ToString());
            if (!string.IsNullOrWhiteSpace(profileId))
            {
                return $"{AdapterIdPrefix}.{profileId}";
            }

            string actorId = NormalizeAdapterSegment(context.ActorId.ToString());
            return string.IsNullOrWhiteSpace(actorId)
                ? AdapterIdPrefix
                : $"{AdapterIdPrefix}.{actorId}";
        }

        private static string NormalizeAdapterSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim();
        }
    }
}
