using _ImmersiveGames.Scripts.HealthSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem
{
    public class EaterHealth : HealthSystem
    {

        private void OnTriggerEnter(Collider other)
        {
            DebugUtility.Log<EaterHealth>($"Collision detected with: {other.name}");
        }

        public void DeathAction()
        {
            Debug.Log($"Morri!");
            GameManager.Instance.SetGameOver(true);
            gameObject.SetActive(false);
        }
        public void DamageAction(float damage)
        {
            DebugUtility.Log<EaterHealth>($"Recebi {damage} de dano!");
        }
        
    }
}