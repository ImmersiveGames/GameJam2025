using System;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem;
using _ImmersiveGames.Scripts.ResourceSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem
{
    public class EaterHealth : ResourceSystem, IDestructible
    {
       public event Action EventDeath;
        protected override void OnDepleted()
        {
            Debug.Log($"{gameObject.name} died!");
            Deafeat();
            // gameObject.SetActive(false);
        }
        public void Deafeat()
        {
            gameObject.SetActive(false);
            //instansia Sons e efeitos de morte
            EventDeath?.Invoke();
            GameManager.Instance.SetGameOver(true);
        }
        public void Heal(float amount) => Increase(amount);
        public void TakeDamage(float damage) => Decrease(damage);

    }
}