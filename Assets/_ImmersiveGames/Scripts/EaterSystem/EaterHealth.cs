using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.EventsBus;
using _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem
{
    public class EaterHealth : ResourceSystem, IDestructible
    {
        private GameObject _modelRoot;

        protected override void Awake()
        {
            base.Awake();
            _modelRoot = transform.GetComponentInChildren<ModelRoot>().gameObject;
        }
        protected override void OnDepleted()
        {
            Debug.Log($"{gameObject.name} died!");
            Deafeat(transform.position);
        }
        public void Deafeat(Vector3 position)
        {
            _modelRoot.SetActive(false);
            // Dispara DeathEvent com a posição do objeto
            EventBus<DeathEvent>.Raise(new DeathEvent(position, gameObject));
            GameManager.Instance.SetGameOver(true);
        }
        public void Heal(float amount) => Increase(amount);
        public void TakeDamage(float damage) => Decrease(damage);

    }
}