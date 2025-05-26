using System.Collections.Generic;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    [CreateAssetMenu(fileName = "PlayerWeaponConfig", menuName = "Player/PlayerWeaponConfig")]
    public class PlayerWeaponConfig : ScriptableObject
    {
        [SerializeField] private List<ShootingSpawnData> weapons = new List<ShootingSpawnData>();
        [SerializeField] private int defaultWeaponIndex = 0;

        public IReadOnlyList<ShootingSpawnData> Weapons => weapons;
        public int DefaultWeaponIndex => Mathf.Clamp(defaultWeaponIndex, 0, weapons.Count - 1);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (weapons.Count == 0)
            {
                Debug.LogWarning($"PlayerWeaponConfig {name} não tem armas configuradas.", this);
            }
        }
#endif
    }
}