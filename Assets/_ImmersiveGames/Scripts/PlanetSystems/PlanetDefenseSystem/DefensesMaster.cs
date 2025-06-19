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
    public sealed class DefensesMaster : PooledObject, IEntity, IHasSkin
    {
        private ModelRoot _modelRoot;
        public ModelRoot ModelRoot => _modelRoot ??= this.GetOrCreateComponentInChild<ModelRoot>("ModelRoot");
        public Transform ModelTransform => ModelRoot.transform;

        private void OnEnable()
        {
            IsActive = true;
        }
        public event Action EventDefensesDeath;
        public ProjectilesData ProjectilesData => Data as ProjectilesData;
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
            Deactivate();
        }
        public Transform Transform => transform;
        public bool IsActive { get; set; }
    }
}