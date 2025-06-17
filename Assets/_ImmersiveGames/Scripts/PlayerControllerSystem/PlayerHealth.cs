using _ImmersiveGames.Scripts.PlayerControllerSystem.EventBus;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    public class PlayerHealth : HealthResource
    {
        private PlayerMaster _playerMaster;
        protected override void Awake()
        {
            base.Awake();
            _playerMaster = GetComponent<PlayerMaster>();
            if (_playerMaster == null)
            {
                DebugUtility.LogError<PlayerHealth>("PlayerMaster não encontrado no GameObject.");
            }
        }

        public override void TakeDamage(float damage)
        {
            base.TakeDamage(damage);
            _playerMaster.OnEventPlayerTakeDamage();
        }
        public void Defeat(Vector3 position)
        {
            EventBus<PlayerDiedEvent>.Raise(new PlayerDiedEvent(position, gameObject));
            DebugUtility.LogVerbose<PlayerHealth>($"Jogador derrotado na posição {position}.");
        }
    }
}