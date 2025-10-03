using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEditor;
using UnityEngine;
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
                _currentReceiver.OnDamageReceived += OnDamageReceived;
                _currentReceiver.OnDeath += OnDeath;
                _currentReceiver.OnRevive += OnRevive;
            }

            // Eventos de DamageDealer
            if (_currentDealer != null)
            {
                _currentDealer.OnDamageDealt += OnDamageDealt;
                _currentDealer.OnDamageBlocked += OnDamageBlocked;
            }

            // Eventos globais
            var deathBinding = new EventBinding<ActorDeathEvent>(OnActorDeath);
            EventBus<ActorDeathEvent>.Register(deathBinding);

            var reviveBinding = new EventBinding<ActorReviveEvent>(OnActorRevive);
            EventBus<ActorReviveEvent>.Register(reviveBinding);

            var damageDealtBinding = new EventBinding<DamageDealtEvent>(OnGlobalDamageDealt);
            EventBus<DamageDealtEvent>.Register(damageDealtBinding);
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
            }
        }

        #region EVENT HANDLERS

        private void OnDamageReceived(float damage, IActor source)
        {
            Debug.Log($"🎯 DAMAGE RECEIVED: {damage} from {source?.ActorName ?? "unknown"}");
        }

        private void OnDeath(IActor actor)
        {
            Debug.Log($"💀 LOCAL DEATH: {actor.ActorName}");
        }

        private void OnRevive(IActor actor)
        {
            Debug.Log($"🔁 LOCAL REVIVE: {actor.ActorName}");
        }

        private void OnDamageDealt(float damage, IDamageable target)
        {
            Debug.Log($"⚡ DAMAGE DEALT: {damage} to {target.Actor?.ActorName ?? "unknown"}");
        }

        private void OnDamageBlocked(IDamageable target)
        {
            Debug.Log($"🛡️ DAMAGE BLOCKED: {target.Actor?.ActorName ?? "unknown"}");
        }

        private void OnActorDeath(ActorDeathEvent evt)
        {
            Debug.Log($"🌍 GLOBAL DEATH: {evt.Actor.ActorName} at {evt.Position}");
        }

        private void OnActorRevive(ActorReviveEvent evt)
        {
            Debug.Log($"🌍 GLOBAL REVIVE: {evt.Actor.ActorName} at {evt.Position}");
        }

        private void OnGlobalDamageDealt(DamageDealtEvent evt)
        {
            if (evt.SourceActor != null && evt.TargetActor != null)
            {
                Debug.Log($"🌍 GLOBAL DAMAGE: {evt.SourceActor.ActorName} → {evt.TargetActor.ActorName} " +
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
                _currentReceiver.ReceiveDamage(testDamage, null, testResource);
                Debug.Log($"🎯 Applied {testDamage} damage to {GetObjectName()}");
            }
            else
            {
                Debug.LogWarning("No DamageReceiver found or object is already dead");
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

                _currentReceiver.KillImmediately();
                Debug.Log($"💀 Killed {GetObjectName()} - Respawn: {testRespawnTime}s");
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
                
                _currentReceiver.KillImmediately();
                Debug.Log($"⚡ Killed with immediate respawn: {GetObjectName()}");
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
                
                _currentReceiver.KillImmediately();
                Debug.Log($"🚫 Killed with no respawn: {GetObjectName()}");
            }
        }

        [ContextMenu("Receiver/Revive Object")]
        private void ReviveSelectedObject()
        {
            if (_currentReceiver != null && _currentReceiver.IsDead)
            {
                _currentReceiver.Revive(reviveHealth);
                Debug.Log($"🔁 Revived {GetObjectName()} with {reviveHealth} health");
            }
            else
            {
                Debug.LogWarning("No DamageReceiver found or object is not dead");
            }
        }

        [ContextMenu("Receiver/Reset to Initial State")]
        private void ResetSelectedObject()
        {
            if (_currentReceiver != null)
            {
                _currentReceiver.ResetToInitialState();
                Debug.Log($"🔄 Reset {GetObjectName()} to initial state");
            }
        }

        [ContextMenu("Receiver/Check Health Status")]
        private void CheckHealthStatus()
        {
            if (_currentReceiver != null)
            {
                _currentReceiver.CheckHealthStatus();
            }
        }

        [ContextMenu("Receiver/Debug Respawn Settings")]
        private void DebugRespawnSettings()
        {
            if (_currentReceiver != null)
            {
                _currentReceiver.DebugInitialValues();
            }
        }

        #endregion

        #region DAMAGE DEALER CONTEXT MENUS


        [ContextMenu("Dealer/Test Damage in Front")]
        private void TestDamageInFront()
        {
            if (_currentDealer == null)
            {
                Debug.LogWarning("No DamageDealer found on this object.");
                return;
            }

            var origin = _currentDealer.transform;
            if (Physics.Raycast(origin.position, origin.forward, out var hit, 10f, _currentDealer.DamageableLayers.value))
            {
                var target = hit.collider.gameObject;
                _currentDealer.DebugDealDamageTo(target);
            }
            else
            {
                Debug.Log("No target hit in front of dealer.");
            }
        }
        
        [ContextMenu("System/Check Receiver Health")]
        private void CheckReceiverHealth()
        {
            if (_currentReceiver == null || _currentReceiver.Actor == null)
            {
                Debug.LogWarning("No valid DamageReceiver to inspect.");
                return;
            }

            var actorId = _currentReceiver.Actor.ActorId;
            if (DependencyManager.Instance.TryGetForObject(actorId, out ResourceSystem rs))
            {
                var health = rs.Get(ResourceType.Health)?.GetCurrentValue();
                Debug.Log($"[Health Check] ActorId={actorId}, Health={health}");
            }
            else
            {
                Debug.LogWarning($"No ResourceSystem found for ActorId={actorId}");
            }
        }


        [ContextMenu("Dealer/Set Damage to 50")]
        private void SetDamageTo50()
        {
            if (_currentDealer != null)
            {
                _currentDealer.SetDamage(50f);
                Debug.Log($"⚡ Dealer damage set to 50 for {_currentDealer.Actor?.ActorName ?? name}");
            }
        }

        [ContextMenu("Dealer/Toggle Destroy On Damage")]
        private void ToggleDestroyOnDamage()
        {
            if (_currentDealer != null)
            {
                // Note: Você precisaria adicionar um setter para DestroyOnDamage no DamageDealer
                // _currentDealer.SetDestroyOnDamage(!testDestroyOnDamage);
                testDestroyOnDamage = !testDestroyOnDamage;
                Debug.Log($"🔧 Destroy On Damage: {testDestroyOnDamage}");
            }
        }

        [ContextMenu("Dealer/Test Area Damage")]
        private void TestAreaDamage()
        {
            if (_currentDealer == null)
            {
                Debug.LogWarning("No DamageDealer found.");
                return;
            }
        
            var hits = Physics.OverlapSphere(_currentDealer.transform.position, 5f, _currentDealer.DamageableLayers.value);
            foreach (var hit in hits)
            {
                var damageable = hit.GetComponent<IDamageable>() ?? hit.GetComponentInParent<IDamageable>();
                if (damageable != null && damageable != (IDamageable)_currentReceiver)
                {
                    damageable.ReceiveDamage(testDealerDamage, _currentDealer.Actor, testDealerResource);
                    Debug.Log($"💥 Area damage to {damageable.Actor?.ActorName ?? hit.name}");
                }
            }
        }

        #endregion

        #region SYSTEM MANAGEMENT

        [ContextMenu("System/Refresh Components")]
        private void RefreshComponents()
        {
            FindCurrentComponents();
            LogCurrentComponents();
        }

        [ContextMenu("System/Print System Status")]
        private void PrintSystemStatus()
        {
            Debug.Log("=== DAMAGE SYSTEM STATUS ===");
            
            if (_currentReceiver != null)
            {
                Debug.Log($"Receiver: {GetObjectName()}");
                Debug.Log($"  Health: {_currentReceiver.CurrentHealth}");
                Debug.Log($"  IsDead: {_currentReceiver.IsDead}");
                Debug.Log($"  CanReceiveDamage: {_currentReceiver.CanReceiveDamage}");
            }
            else
            {
                Debug.Log("Receiver: None");
            }

            if (_currentDealer != null)
            {
                Debug.Log($"Dealer: {GetObjectName()}");
                Debug.Log($"  Damage: {_currentDealer.DamageAmount}");
                Debug.Log($"  Resource: {_currentDealer.DamageResourceType}");
                Debug.Log($"  Type: {_currentDealer.DamageType}");
            }
            else
            {
                Debug.Log("Dealer: None");
            }
        }

        [ContextMenu("System/Find All Damageable in Scene")]
        private void FindAllDamageable()
        {
            DamageReceiver[] damageReceivers = FindObjectsByType<DamageReceiver>( FindObjectsSortMode.None);
            Debug.Log($"🔍 Found {damageReceivers.Length} DamageReceivers in scene:");
            foreach (var damageable in damageReceivers)
            {
                Debug.Log($"   - {damageable.Actor?.ActorName ?? damageable.name} " +
                         $"(Health: {damageable.CurrentHealth}, Dead: {damageable.IsDead})");
            }
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
                
                // Mostrar health como texto (precisa de Handles, mas não disponível fora do Editor)
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
            }
        }

        #endregion

        private string GetObjectName()
        {
            return _currentReceiver?.Actor?.ActorName ?? gameObject.name;
        }

        private void OnDestroy()
        {
            // Cleanup de eventos
            if (_currentReceiver != null)
            {
                _currentReceiver.OnDamageReceived -= OnDamageReceived;
                _currentReceiver.OnDeath -= OnDeath;
                _currentReceiver.OnRevive -= OnRevive;
            }

            if (_currentDealer != null)
            {
                _currentDealer.OnDamageDealt -= OnDamageDealt;
                _currentDealer.OnDamageBlocked -= OnDamageBlocked;
            }

            EventBus<ActorDeathEvent>.Clear();
            EventBus<ActorReviveEvent>.Clear();
            EventBus<DamageDealtEvent>.Clear();
        }
    }
}