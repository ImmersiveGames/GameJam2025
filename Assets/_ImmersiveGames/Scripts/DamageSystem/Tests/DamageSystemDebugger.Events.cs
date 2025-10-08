// Path: _ImmersiveGames/Scripts/DamageSystem/Tests/DamageSystemDebugger.Events.cs
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.ActorSystems;
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
            Debug.Log($"[Debugger] {GetObjectName()} recebeu {damage} de {source?.ActorName ?? "unknown"}");
        }

        private void OnLocalDeath(IActor actor)
        {
            Debug.Log($"[Debugger] {GetObjectName()} morreu.");
            _audio?.PlaySound(_audio.AudioConfig?.deathSound);
        }

        private void OnLocalRevive(IActor actor)
        {
            Debug.Log($"[Debugger] {GetObjectName()} reviveu.");
            _audio?.PlaySound(_audio.AudioConfig?.reviveSound);
        }

        private void OnResourceUpdatedEvent(ResourceUpdateEvent evt)
        {
            if (!IsVerbose) return;
            if (_receiver == null || evt.ActorId != _receiver.Actor?.ActorId) return;
            Debug.Log($"[Debugger] Resource {evt.ResourceType} updated: {evt.NewValue}");
        }

        private void OnGlobalDamageEvent(DamageDealtEvent evt)
        {
            if (!IsVerbose) return;
            if (_receiver?.Actor == null) return;
            if (evt.TargetActor.ActorId != _receiver.Actor.ActorId) return;
            Debug.Log($"[Debugger] Global damage: {evt.SourceActor.ActorName} → {evt.TargetActor.ActorName} ({evt.DamageAmount})");
        }
    }
}
