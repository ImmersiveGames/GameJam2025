using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.ActorSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem.Tests
{
    /// <summary>
    /// Módulo de integração de eventos globais e locais do DamageSystemDebugger.
    /// Responsável por capturar e exibir mudanças de dano, morte, revive, e atualizações de recursos.
    /// </summary>
    public partial class DamageSystemDebugger
    {
        private EventBinding<ResourceUpdateEvent> _onResourceUpdate;
        private EventBinding<DamageDealtEvent> _onDamageEvent;

        // --- REGISTRO PRINCIPAL ---
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
            _onResourceUpdate = new EventBinding<ResourceUpdateEvent>(OnResourceUpdatedEvent);
            _onDamageEvent = new EventBinding<DamageDealtEvent>(OnGlobalDamageEvent);

            EventBus<ResourceUpdateEvent>.Register(_onResourceUpdate);
            EventBus<DamageDealtEvent>.Register(_onDamageEvent);
        }

        private void UnregisterGlobalEvents()
        {
            if (_onResourceUpdate != null)
                EventBus<ResourceUpdateEvent>.Unregister(_onResourceUpdate);

            if (_onDamageEvent != null)
                EventBus<DamageDealtEvent>.Unregister(_onDamageEvent);
        }

        // --- EVENTOS LOCAIS ---
        private void OnLocalDamageReceived(float damage, IActor source)
        {
            if (!IsVerbose) return;
            Debug.Log($"💥 [{GetObjectName()}] recebeu {damage} de dano. Fonte: {source?.ActorName ?? "Desconhecida"}");
        }

        private void OnLocalDeath(IActor actor)
        {
            Debug.Log($"💀 [{actor?.ActorName ?? GetObjectName()}] morreu.");

            if (_audio != null)
                _audio.PlayCustomShootSound(_audio.AudioConfig?.deathSound, 1f);
        }

        private void OnLocalRevive(IActor actor)
        {
            Debug.Log($"✨ [{actor?.ActorName ?? GetObjectName()}] foi revivido.");

            if (_audio != null)
                _audio.PlayCustomShootSound(_audio.AudioConfig?.reviveSound, 1f);
        }

        // --- EVENTOS GLOBAIS ---
        private void OnResourceUpdatedEvent(ResourceUpdateEvent evt)
        {
            if (!IsVerbose) return;
            if (_receiver == null || evt.ActorId != _receiver.Actor?.ActorId)
                return;

            Debug.Log($"📊 [{GetObjectName()}] Recurso {evt.ResourceType} atualizado: {evt.NewValue}");
        }

        private void OnGlobalDamageEvent(DamageDealtEvent evt)
        {
            if (!IsVerbose) return;

            var receiverId = _receiver.Actor.ActorId;
            if (evt.TargetActor.ActorId != receiverId) return;

            Debug.Log($"⚡ Evento global: {evt.SourceActor.ActorName} causou {evt.DamageAmount} de dano a {evt.TargetActor.ActorName}");
        }

        // --- TESTES DE EVENTOS ---
        [ContextMenu("Events/Trigger Test Events")]
        private void TriggerTestEvents()
        {
            if (_receiver == null)
            {
                Debug.LogWarning("⚠️ Nenhum DamageReceiver para testar eventos.");
                return;
            }

            _receiver.ReceiveDamage(5f, null, ResourceType.Health);
            _receiver.Revive(50f);
            Debug.Log("🧪 Teste de eventos disparado.");
        }

        [ContextMenu("Events/Toggle Verbose Mode")]
        private void ToggleVerbose()
        {
            logAllEvents = !logAllEvents;
            Debug.Log($"🔁 Verbose mode {(logAllEvents ? "ativado" : "desativado")}.");
        }
    }
}
