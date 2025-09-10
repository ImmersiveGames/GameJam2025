using System;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem;
using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.Extensions;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems.PlanetDefenseSystem
{
    public sealed class DefensesMaster : MonoBehaviour, IEntity, IHasSkin
    {
        [Header("Configuração")]
        
        private PooledObject _pooledObject;
        private ModelRoot _modelRoot;
        public ModelRoot ModelRoot => _modelRoot ??= this.GetOrCreateComponentInChild<ModelRoot>("ModelRoot");
        public Transform ModelTransform => ModelRoot.transform;
        private void Awake()
        {
            if (!TryGetComponent(out _pooledObject))
            {
                DebugUtility.LogError<DefensesMaster>($"No PooledObject component found on {gameObject.name}. This component is required for DefensesExplosions to function properly.");
            }
        }

        private void OnEnable()
        {
            IsActive = true;
        }
        public event Action EventDefensesDeath;
        public ProjectilesData ProjectilesData => _pooledObject.GetData<ProjectilesData>();
        public void OnDefensesDeath()
        {
            DebugUtility.LogVerbose<DefensesMaster>($"EventDefensesDeath invoked for {gameObject.name}", "yellow", this);
            SetSkinActive(false);
            IsActive = false;
            EventDefensesDeath?.Invoke();
            Invoke(nameof(ReturnPool), 1f);
        }
        public void SetSkinActive(bool active)
        {
            if (_modelRoot != null)
            {
                _modelRoot.gameObject.SetActive(active);
            }
        }
        private void ReturnPool()
        {
            SetSkinActive(true);
            IsActive = true;
            _pooledObject.Deactivate();
        }
        public Transform Transform => transform;
        public bool IsActive { get; set; }
    }
}