using System.Linq;
using _ImmersiveGames.Scripts.SpawnSystem;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
using ObjectSpawnedEvent = _ImmersiveGames.Scripts.Utils.PoolSystems.ObjectSpawnedEvent;

namespace _ImmersiveGames.Scripts.PoolSystem.Tests
{
    public class PoolSystemTester : MonoBehaviour
    {
        [SerializeField] private PoolableObjectData poolableData;
        [SerializeField] private Vector3 spawnPosition = Vector3.zero;
        [SerializeField] private KeyCode activateKey = KeyCode.A; // Ativa um objeto PooledObject
        [SerializeField] private KeyCode returnKey = KeyCode.R;   // Retorna um objeto ativo
        [SerializeField] private KeyCode returnAllKey = KeyCode.T; // Retorna todos os objetos ativos

        private ObjectPool _pool;
        private EventBinding<ObjectSpawnedEvent> _spawnedBinding;
        private EventBinding<ObjectReturnedEvent> _returnedBinding;
        private EventBinding<ObjectDeactivatedEvent> _deactivatedBinding;
        private EventBinding<PoolExhaustedEvent> _exhaustedBinding;

        private void Start()
        {
            if (poolableData == null)
            {
                DebugUtility.LogError<PoolSystemTester>("PoolableObjectData não configurado.", this);
                enabled = false;
                return;
            }

            PoolManager.Instance.RegisterPool(poolableData);
            _pool = PoolManager.Instance.GetPool(poolableData.ObjectName);
            if (_pool == null)
            {
                DebugUtility.LogError<PoolSystemTester>("Falha ao registrar pool.", this);
                enabled = false;
                return;
            }

            // Registrar bindings para eventos
            _spawnedBinding = new EventBinding<ObjectSpawnedEvent>(OnObjectSpawned);
            EventBus<ObjectSpawnedEvent>.Register(_spawnedBinding);

            _returnedBinding = new EventBinding<ObjectReturnedEvent>(OnObjectReturned);
            EventBus<ObjectReturnedEvent>.Register(_returnedBinding);

            _deactivatedBinding = new EventBinding<ObjectDeactivatedEvent>(OnObjectDeactivated);
            EventBus<ObjectDeactivatedEvent>.Register(_deactivatedBinding);

            _exhaustedBinding = new EventBinding<PoolExhaustedEvent>(OnPoolExhausted);
            EventBus<PoolExhaustedEvent>.Register(_exhaustedBinding);
        }

        private void OnDestroy()
        {
            // Desregistrar bindings
            EventBus<ObjectSpawnedEvent>.Unregister(_spawnedBinding);
            EventBus<ObjectReturnedEvent>.Unregister(_returnedBinding);
            EventBus<ObjectDeactivatedEvent>.Unregister(_deactivatedBinding);
            EventBus<PoolExhaustedEvent>.Unregister(_exhaustedBinding);
        }

        private void Update()
        {
            if (Input.GetKeyDown(activateKey))
            {
                var obj = PoolManager.Instance.GetObject(poolableData.ObjectName, spawnPosition);
                if (obj != null)
                {
                    DebugUtility.Log<PoolSystemTester>($"Objeto ativado em {spawnPosition}. Objetos disponíveis: {_pool.GetAvailableCount()}.", "green", this);
                }
            }

            if (Input.GetKeyDown(returnKey))
            {
                var activeObjects = _pool.GetActiveObjects();
                if (activeObjects.Count > 0)
                {
                    _pool.ReturnObject(activeObjects[0]);
                    DebugUtility.Log<PoolSystemTester>($"Objeto retornado manualmente. Objetos disponíveis: {_pool.GetAvailableCount()}.", "green", this);
                }
            }

            if (Input.GetKeyDown(returnAllKey))
            {
                foreach (var obj in _pool.GetActiveObjects().ToArray())
                {
                    _pool.ReturnObject(obj);
                }
                DebugUtility.Log<PoolSystemTester>($"Todos os objetos retornados. Objetos disponíveis: {_pool.GetAvailableCount()}.", "green", this);
            }
        }

        private void OnObjectSpawned(ObjectSpawnedEvent evt)
        {
            DebugUtility.Log<PoolSystemTester>($"Evento: Objeto spawnado no pool '{evt.PoolKey}' em {evt.Position}.", "cyan", this);
        }

        private void OnObjectReturned(ObjectReturnedEvent evt)
        {
            DebugUtility.Log<PoolSystemTester>($"Evento: Objeto retornado ao pool '{evt.PoolKey}'.", "cyan", this);
        }

        private void OnObjectDeactivated(ObjectDeactivatedEvent evt)
        {
            DebugUtility.Log<PoolSystemTester>($"Evento: Objeto desativado no pool '{evt.PoolKey}'.", "cyan", this);
        }

        private void OnPoolExhausted(PoolExhaustedEvent evt)
        {
            DebugUtility.Log<PoolSystemTester>($"Evento: Pool '{evt.PoolKey}' esgotado.", "yellow", this);
        }
    }
}