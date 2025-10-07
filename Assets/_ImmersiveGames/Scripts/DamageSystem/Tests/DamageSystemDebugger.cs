using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace _ImmersiveGames.Scripts.DamageSystem.Tests
{
    public class DamageSystemDebugger : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool logAllEvents = true;
        [SerializeField] private bool showVisualDebug = true;
        [SerializeField] private Color debugColor = Color.red;
        
        [Header("Damage Receiver Tests")]
        [SerializeField] private float testDamage = 10f;
        [SerializeField] private ResourceType testResource = ResourceType.Health;
        [SerializeField] private float reviveHealth = 100f;

        [Header("Respawn Settings")]
        [SerializeField] private float testRespawnTime = 3f;
        [SerializeField] private bool testCanRespawn = true;
        [SerializeField] private bool testDeactivateOnDeath = true;

        [Header("Damage Dealer Tests")]
        [SerializeField] private float testDealerDamage = 25f;
        [SerializeField] private ResourceType testDealerResource = ResourceType.Health;
        [SerializeField] private bool testDestroyOnDamage;

        private DamageReceiver _currentReceiver;
        private DamageDealer _currentDealer;
        private List<object> _eventBindings = new List<object>();

        private void Start()
        {
            FindCurrentComponents();
            RegisterEvents();
            
            Debug.Log("🛠️ DamageSystemDebugger started - Monitoring damage system");
            LogCurrentComponents();
        }

        private void FindCurrentComponents()
        {
            _currentReceiver = GetComponent<DamageReceiver>();
            _currentDealer = GetComponent<DamageDealer>();
        }

        private void RegisterEvents()
        {
            if (!logAllEvents) return;

            // Eventos de DamageReceiver
            if (_currentReceiver != null)
            {
                _currentReceiver.EventDamageReceived += OnEventDamageReceived;
                _currentReceiver.EventDeath += OnEventDeath;
                _currentReceiver.EventRevive += OnEventRevive;
            }

            // Eventos de DamageDealer
            if (_currentDealer != null)
            {
                _currentDealer.OnDamageDealt += OnDamageDealt;
                _currentDealer.OnDamageBlocked += OnDamageBlocked;
            }

            // Eventos globais - Correção: criar métodos auxiliares para registro
            RegisterGlobalEvent<ResourceUpdateEvent>(OnResourceUpdated);
            RegisterGlobalEvent<ActorDeathEvent>(OnActorDeath);
            RegisterGlobalEvent<ActorReviveEvent>(OnActorRevive);
            RegisterGlobalEvent<DamageDealtEvent>(OnGlobalDamageDealt);
        }
        
        // Correção: Método genérico para registrar eventos globais
        private void RegisterGlobalEvent<T>(System.Action<T> handler) where T : class, IEvent
        {
            var binding = new EventBinding<T>(handler);
            EventBus<T>.Register(binding);
            _eventBindings.Add(binding);
        }

        // Correção: Método para desregistrar todos os eventos
        private void UnregisterAllEvents()
        {
            foreach (var binding in _eventBindings)
            {
                // Usar reflection para chamar Unregister dinamicamente
                var method = binding.GetType().GetMethod("Unregister");
                method?.Invoke(binding, null);
            }
            _eventBindings.Clear();
        }

        private void LogCurrentComponents()
        {
            Debug.Log("🔍 DamageSystemDebugger found:");
            Debug.Log($"   - DamageReceiver: {_currentReceiver != null}");
            Debug.Log($"   - DamageDealer: {_currentDealer != null}");
            
            if (_currentReceiver != null)
            {
                Debug.Log($"   - Current Health: {_currentReceiver.CurrentHealth}");
                Debug.Log($"   - Is Dead: {_currentReceiver.IsDead}");
                Debug.Log($"   - Can Receive Damage: {_currentReceiver.CanReceiveDamage}");
            }
        }

        #region EVENT HANDLERS

        private void OnEventDamageReceived(float damage, IActor source)
        {
            Debug.Log($"🎯 [RECEIVER] DAMAGE RECEIVED: {damage} from {source?.ActorName ?? "unknown"} " +
                     $"(Health: {_currentReceiver?.CurrentHealth ?? 0f})");
        }

        private void OnEventDeath(IActor actor)
        {
            Debug.Log($"💀 [RECEIVER] LOCAL DEATH: {actor.ActorName}");
        }

        private void OnEventRevive(IActor actor)
        {
            Debug.Log($"🔁 [RECEIVER] LOCAL REVIVE: {actor.ActorName} " +
                     $"(Health: {_currentReceiver?.CurrentHealth ?? 0f})");
        }

        private void OnDamageDealt(float damage, IDamageable target)
        {
            Debug.Log($"⚡ [DEALER] DAMAGE DEALT: {damage} to {target.Actor?.ActorName ?? "unknown"}");
        }

        private void OnDamageBlocked(IDamageable target)
        {
            Debug.Log($"🛡️ [DEALER] DAMAGE BLOCKED: {target.Actor?.ActorName ?? "unknown"}");
        }

        private void OnResourceUpdated(ResourceUpdateEvent evt)
        {
            if (_currentReceiver?.Actor?.ActorId == evt.ActorId)
            {
                Debug.Log($"📊 [RESOURCE] Updated: {evt.ResourceType} = {evt.NewValue.GetCurrentValue()} " +
                         $"(Max: {evt.NewValue.GetMaxValue()})");
            }
        }

        private void OnActorDeath(ActorDeathEvent evt)
        {
            Debug.Log($"🌍 [GLOBAL] ACTOR DEATH: {evt.Actor.ActorName} at {evt.Position}");
        }

        private void OnActorRevive(ActorReviveEvent evt)
        {
            Debug.Log($"🌍 [GLOBAL] ACTOR REVIVE: {evt.Actor.ActorName} at {evt.Position}");
        }

        private void OnGlobalDamageDealt(DamageDealtEvent evt)
        {
            if (evt.SourceActor != null && evt.TargetActor != null)
            {
                Debug.Log($"🌍 [GLOBAL] DAMAGE: {evt.SourceActor.ActorName} → {evt.TargetActor.ActorName} " +
                         $"(Amount: {evt.DamageAmount}, Type: {evt.DamageType})");
            }
        }

        #endregion

        #region DAMAGE RECEIVER CONTEXT MENUS

        [ContextMenu("Receiver/Test Receive Damage")]
        private void TestReceiveDamage()
        {
            if (_currentReceiver != null && !_currentReceiver.IsDead)
            {
                Debug.Log($"🎯 [TEST] Applying {testDamage} {testResource} damage to {GetObjectName()}");
                _currentReceiver.ReceiveDamage(testDamage, null, testResource);
            }
            else
            {
                Debug.LogWarning($"[TEST] Cannot apply damage - No DamageReceiver found or {GetObjectName()} is already dead");
            }
        }

        [ContextMenu("Receiver/Kill Object")]
        private void KillSelectedObject()
        {
            if (_currentReceiver != null)
            {
                _currentReceiver.SetRespawnTime(testRespawnTime);
                _currentReceiver.SetCanRespawn(testCanRespawn);
                _currentReceiver.SetDeactivateOnDeath(testDeactivateOnDeath);

                Debug.Log($"💀 [TEST] Killing {GetObjectName()} - Respawn: {testRespawnTime}s, " +
                         $"CanRespawn: {testCanRespawn}, Deactivate: {testDeactivateOnDeath}");
                
                _currentReceiver.KillImmediately();
            }
        }

        [ContextMenu("Receiver/Kill with Immediate Respawn")]
        private void KillWithImmediateRespawn()
        {
            if (_currentReceiver != null)
            {
                _currentReceiver.SetRespawnTime(0f);
                _currentReceiver.SetCanRespawn(true);
                _currentReceiver.SetDeactivateOnDeath(false);
                
                Debug.Log($"⚡ [TEST] Killing with immediate respawn: {GetObjectName()}");
                _currentReceiver.KillImmediately();
            }
        }

        [ContextMenu("Receiver/Kill with No Respawn")]
        private void KillWithNoRespawn()
        {
            if (_currentReceiver != null)
            {
                _currentReceiver.SetRespawnTime(-1f);
                _currentReceiver.SetCanRespawn(false);
                _currentReceiver.SetDeactivateOnDeath(true);
                
                Debug.Log($"🚫 [TEST] Killing with no respawn: {GetObjectName()}");
                _currentReceiver.KillImmediately();
            }
        }

        [ContextMenu("Receiver/Revive Object")]
        private void ReviveSelectedObject()
        {
            if (_currentReceiver != null && _currentReceiver.IsDead)
            {
                Debug.Log($"🔁 [TEST] Reviving {GetObjectName()} with {reviveHealth} health");
                _currentReceiver.Revive(reviveHealth);
            }
            else
            {
                Debug.LogWarning($"[TEST] Cannot revive - No DamageReceiver found or {GetObjectName()} is not dead");
            }
        }

        [ContextMenu("Receiver/Reset to Initial State")]
        private void ResetSelectedObject()
        {
            if (_currentReceiver != null)
            {
                Debug.Log($"🔄 [TEST] Resetting {GetObjectName()} to initial state");
                _currentReceiver.ResetToInitialState();
            }
        }

        [ContextMenu("Receiver/Check Health Status")]
        private void CheckHealthStatus()
        {
            if (_currentReceiver != null)
            {
                Debug.Log($"[HEALTH STATUS] {GetObjectName()}: " +
                         $"Health = {_currentReceiver.CurrentHealth}, " +
                         $"IsDead = {_currentReceiver.IsDead}, " +
                         $"CanRespawn = {_currentReceiver.CanRespawn}, " +
                         $"CanReceiveDamage = {_currentReceiver.CanReceiveDamage}");
            }
        }

        [ContextMenu("Receiver/Debug Initial Values")]
        private void DebugInitialValues()
        {
            if (_currentReceiver != null)
            {
                var receiver = _currentReceiver;
                Debug.Log($"[INITIAL VALUES] {GetObjectName()}:");
                Debug.Log($"   - Position: {receiver.transform.position}");
                Debug.Log($"   - Respawn: CanRespawn={receiver.CanRespawn}, Time={receiver.RespawnTime}s");
                Debug.Log($"   - Death: DestroyOnDeath={receiver.DestroyOnDeath}, DeactivateOnDeath={receiver.DeactivateOnDeath}");
                
                if (receiver.ResourceBridge != null)
                {
                    var resourceSystem = receiver.ResourceBridge.GetService();
                    Debug.Log("   - Current Resources:");
                    foreach (var resourceEntry in resourceSystem.GetAll())
                    {
                        Debug.Log($"     {resourceEntry.Key}: {resourceEntry.Value.GetCurrentValue()}/{resourceEntry.Value.GetMaxValue()}");
                    }
                }
            }
        }

        #endregion

        #region DAMAGE DEALER CONTEXT MENUS

        [ContextMenu("Dealer/Test Damage in Front")]
        private void TestDamageInFront()
        {
            if (_currentDealer == null)
            {
                Debug.LogWarning("[TEST] No DamageDealer found on this object.");
                return;
            }

            var origin = _currentDealer.transform;
            if (Physics.Raycast(origin.position, origin.forward, out var hit, 10f, _currentDealer.DamageableLayers.value))
            {
                var target = hit.collider.gameObject;
                Debug.Log($"🎯 [TEST] Dealing damage to {target.name}");
                _currentDealer.DebugDealDamageTo(target);
            }
            else
            {
                Debug.Log("[TEST] No target hit in front of dealer.");
            }
        }

        [ContextMenu("Dealer/Test Area Damage")]
        private void TestAreaDamage()
        {
            if (_currentDealer == null)
            {
                Debug.LogWarning("[TEST] No DamageDealer found.");
                return;
            }
        
            var hits = Physics.OverlapSphere(_currentDealer.transform.position, 5f, _currentDealer.DamageableLayers.value);
            Debug.Log($"💥 [TEST] Area damage affecting {hits.Length} targets");
            
            foreach (var hit in hits)
            {
                var damageable = hit.GetComponent<IDamageable>() ?? hit.GetComponentInParent<IDamageable>();
                if (damageable != null && damageable != (IDamageable)_currentReceiver)
                {
                    damageable.ReceiveDamage(testDealerDamage, _currentDealer.Actor, testDealerResource);
                    Debug.Log($"   - Hit: {damageable.Actor?.ActorName ?? hit.name}");
                }
            }
        }

        [ContextMenu("Dealer/Set Damage to 50")]
        private void SetDamageTo50()
        {
            if (_currentDealer != null)
            {
                _currentDealer.SetDamage(50f);
                Debug.Log($"⚡ [TEST] Dealer damage set to 50 for {_currentDealer.Actor?.ActorName ?? name}");
            }
        }

        #endregion

        #region SYSTEM DIAGNOSTICS

        [ContextMenu("System/Check Resource System")]
        private void CheckResourceSystem()
        {
            if (_currentReceiver == null || _currentReceiver.Actor == null)
            {
                Debug.LogWarning("[DIAG] No valid DamageReceiver to inspect.");
                return;
            }

            var actorId = _currentReceiver.Actor.ActorId;
            if (DependencyManager.Instance.TryGetForObject(actorId, out ResourceSystem rs))
            {
                var health = rs.Get(ResourceType.Health)?.GetCurrentValue() ?? 0f;
                Debug.Log($"[DIAG] ResourceSystem for {actorId}: Health = {health}");
                
                // List all resources
                Debug.Log("   All Resources:");
                foreach (var resource in rs.GetAll())
                {
                    Debug.Log($"     {resource.Key}: {resource.Value.GetCurrentValue()}/{resource.Value.GetMaxValue()}");
                }
            }
            else
            {
                Debug.LogWarning($"[DIAG] No ResourceSystem found for ActorId={actorId}");
            }
        }

        [ContextMenu("System/Print System Status")]
        private void PrintSystemStatus()
        {
            Debug.Log("=== DAMAGE SYSTEM STATUS ===");
            
            if (_currentReceiver != null)
            {
                Debug.Log($"📋 RECEIVER: {GetObjectName()}");
                Debug.Log($"   Health: {_currentReceiver.CurrentHealth}");
                Debug.Log($"   IsDead: {_currentReceiver.IsDead}");
                Debug.Log($"   CanReceiveDamage: {_currentReceiver.CanReceiveDamage}");
                Debug.Log($"   CanRespawn: {_currentReceiver.CanRespawn}");
            }
            else
            {
                Debug.Log("📋 RECEIVER: None");
            }

            if (_currentDealer != null)
            {
                Debug.Log($"⚡ DEALER: {GetObjectName()}");
                Debug.Log($"   Damage: {_currentDealer.DamageAmount}");
                Debug.Log($"   Resource: {_currentDealer.DamageResourceType}");
                Debug.Log($"   Type: {_currentDealer.DamageType}");
            }
            else
            {
                Debug.Log("⚡ DEALER: None");
            }
        }

        [ContextMenu("System/Find All Damageable in Scene")]
        private void FindAllDamageable()
        {
            var damageReceivers = FindObjectsByType<DamageReceiver>(FindObjectsSortMode.None);
            Debug.Log($"🔍 [DIAG] Found {damageReceivers.Length} DamageReceivers in scene:");
            
            foreach (var receiver in damageReceivers)
            {
                var status = receiver.IsDead ? "💀 DEAD" : "❤️ ALIVE";
                Debug.Log($"   - {receiver.Actor?.ActorName ?? receiver.name}: " +
                         $"{status} (Health: {receiver.CurrentHealth})");
            }
        }

        [ContextMenu("System/Refresh Components")]
        private void RefreshComponents()
        {
            FindCurrentComponents();
            Debug.Log("🔧 [SYSTEM] Components refreshed");
            LogCurrentComponents();
        }

        #endregion

        #region VISUAL DEBUGGING

        private void OnDrawGizmosSelected()
        {
            if (!showVisualDebug) return;

            // Debug para DamageReceiver
            if (_currentReceiver != null)
            {
                Gizmos.color = _currentReceiver.IsDead ? Color.red : Color.green;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);
                
                #if UNITY_EDITOR
                Handles.Label(transform.position + Vector3.up * 3f, 
                    $"Health: {_currentReceiver.CurrentHealth}\nDead: {_currentReceiver.IsDead}");
                #endif
            }

            // Debug para DamageDealer
            if (_currentDealer != null)
            {
                Gizmos.color = debugColor;
                Gizmos.DrawRay(transform.position, transform.forward * 3f);
                Gizmos.DrawWireSphere(transform.position, 0.3f);
                
                // Mostrar área de dano
                Gizmos.color = new Color(debugColor.r, debugColor.g, debugColor.b, 0.2f);
                Gizmos.DrawSphere(transform.position, 5f);
            }
        }

        #endregion

        private string GetObjectName()
        {
            return _currentReceiver?.Actor?.ActorName ?? gameObject.name;
        }

        private void OnDestroy()
        {
            // Cleanup de eventos do DamageReceiver
            if (_currentReceiver != null)
            {
                _currentReceiver.EventDamageReceived -= OnEventDamageReceived;
                _currentReceiver.EventDeath -= OnEventDeath;
                _currentReceiver.EventRevive -= OnEventRevive;
            }

            // Cleanup de eventos do DamageDealer
            if (_currentDealer != null)
            {
                _currentDealer.OnDamageDealt -= OnDamageDealt;
                _currentDealer.OnDamageBlocked -= OnDamageBlocked;
            }

            // Cleanup de eventos globais
            UnregisterAllEvents();
        }
    }
}