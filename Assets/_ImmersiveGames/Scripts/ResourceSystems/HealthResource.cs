using System;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.EventsBus;
using _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem;
using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class HealthResource : ResourceSystem, IDestructible
    {
        protected GameObject modelRoot;
        private ActorMaster _actorMaster;

        private void Start()
        {
            _actorMaster = GetComponentInParent<ActorMaster>();
            modelRoot = _actorMaster.GetModelRoot().gameObject;
        }
        protected override void OnDepleted()
        {
            Debug.Log($"{gameObject.name} died!");
            Deafeat(transform.position);
        }
        public virtual void Deafeat(Vector3 position)
        {
            Vector3 spawnPoint = modelRoot.transform.position;
            Debug.Log($"HealthResource {gameObject.name}: Disparando DeathEvent com posição {spawnPoint}");
            EventBus<DeathEvent>.Raise(new DeathEvent(spawnPoint, gameObject));
            modelRoot.SetActive(false);
        }
        public void Heal(float amount) => Increase(amount);
        public void TakeDamage(float damage) => Decrease(damage);

    }
}