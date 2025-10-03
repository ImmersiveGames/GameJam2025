using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;

namespace _ImmersiveGames.Scripts.SkinSystems
{
    public class SkinController : MonoBehaviour
    {
        [SerializeField] private SkinCollectionData skinCollection;
        
        private SkinService _skinService;
        private PooledObject _pooledObject;
        private EventBinding<SkinUpdateEvent> _binding;

        private void Awake()
        {
            _pooledObject = GetComponent<PooledObject>();
            _skinService = new SkinService();

            // Pool events
            if (_pooledObject?.GetPool != null)
            {
                _pooledObject.GetPool.OnObjectActivated.AddListener(_ => Activate());
                _pooledObject.GetPool.OnObjectReturned.AddListener(_ => Deactivate());
            }

            // EventBus
            _binding = new EventBinding<SkinUpdateEvent>(OnSkinUpdate);
            FilteredEventBus<SkinUpdateEvent>.Register(_binding, this);

            // Init
            if (skinCollection != null)
                _skinService.Initialize(skinCollection, transform, _pooledObject?.Spawner);
        }

        public void ApplySkin(ISkinConfig config, IActor spawner = null) =>
            _skinService.ApplyConfig(config, spawner);

        public void ApplySkinCollection(SkinCollectionData newCollection, IActor spawner = null) =>
            _skinService.ApplyCollection(newCollection, spawner);

        private void Activate() => gameObject.SetActive(true);
        private void Deactivate() => gameObject.SetActive(false);

        private void OnSkinUpdate(SkinUpdateEvent evt)
        {
            if (evt.SkinConfig != null)
                ApplySkin(evt.SkinConfig, evt.Spawner);
        }

        private void OnDestroy()
        {
            if (_pooledObject?.GetPool != null)
            {
                _pooledObject.GetPool.OnObjectActivated.RemoveListener(_ => Activate());
                _pooledObject.GetPool.OnObjectReturned.RemoveListener(_ => Deactivate());
            }
            FilteredEventBus<SkinUpdateEvent>.Unregister(this);
        }
    }
}
