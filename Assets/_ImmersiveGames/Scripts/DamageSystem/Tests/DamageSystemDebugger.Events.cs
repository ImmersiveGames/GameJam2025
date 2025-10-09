// Path: _ImmersiveGames/Scripts/DamageSystem/Tests/DamageSystemDebugger.Events.cs
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem.Tests
{
    public partial class DamageSystemDebugger
    {
        private EventBinding<ResourceUpdateEvent> _resourceBinding;
        private EventBinding<DamageDealtEvent> _damageBinding;

        private void RegisterLocalEvents()
        {
            if (_receiver == null) return;
            _receiver.EventDamageReceived += OnLocalDamageReceived;
            _receiver.EventDeath += OnLocalDeath;
            _receiver.EventRevive += OnLocalRevive;
        }

        private void UnregisterLocalEvents()
        {
            if (_receiver == null) return;
            _receiver.EventDamageReceived -= OnLocalDamageReceived;
            _receiver.EventDeath -= OnLocalDeath;
            _receiver.EventRevive -= OnLocalRevive;
        }

        private void RegisterGlobalEvents()
        {
            _resourceBinding = new EventBinding<ResourceUpdateEvent>(OnResourceUpdatedEvent);
            _damageBinding = new EventBinding<DamageDealtEvent>(OnGlobalDamageEvent);
            EventBus<ResourceUpdateEvent>.Register(_resourceBinding);
            EventBus<DamageDealtEvent>.Register(_damageBinding);
        }

        private void UnregisterGlobalEvents()
        {
            if (_resourceBinding != null) EventBus<ResourceUpdateEvent>.Unregister(_resourceBinding);
            if (_damageBinding != null) EventBus<DamageDealtEvent>.Unregister(_damageBinding);
        }

        private void OnLocalDamageReceived(float damage, IActor source)
        {
            if (!IsVerbose) return;
            DebugUtility.LogVerbose<DamageSystemDebugger>($"[Debugger] {GetObjectName()} recebeu {damage} de {source?.ActorName ?? "unknown"}");
        }

        private void OnLocalDeath(IActor actor)
        {
            DebugUtility.LogVerbose<DamageSystemDebugger>($"[Debugger] {GetObjectName()} morreu.");
            var config = _audio?.GetAudioConfig();
            if (config?.deathSound != null) _audio.TestPlaySoundPublic(config.deathSound, deathVolume); // Usa wrapper para validação integrada
        }

        private void OnLocalRevive(IActor actor)
        {
            DebugUtility.LogVerbose<DamageSystemDebugger>($"[Debugger] {GetObjectName()} reviveu.");
            var config = _audio?.GetAudioConfig();
            if (config?.reviveSound != null) _audio.TestPlaySoundPublic(config.reviveSound, reviveVolume);
        }

        private void OnResourceUpdatedEvent(ResourceUpdateEvent evt)
        {
            if (!IsVerbose) return;
            if (_receiver == null || evt.ActorId != _receiver.Actor?.ActorId) return;
            DebugUtility.LogVerbose<DamageSystemDebugger>($"[Debugger] Resource {evt.ResourceType} updated: {evt.NewValue}");
        }

        private void OnGlobalDamageEvent(DamageDealtEvent evt)
        {
            if (!IsVerbose) return;
            if (_receiver?.Actor == null) return;
            if (evt.TargetActor.ActorId != _receiver.Actor.ActorId) return;
            DebugUtility.LogVerbose<DamageSystemDebugger>($"[Debugger] Global damage: {evt.SourceActor.ActorName} → {evt.TargetActor.ActorName} ({evt.DamageAmount})");
        }
    }
}