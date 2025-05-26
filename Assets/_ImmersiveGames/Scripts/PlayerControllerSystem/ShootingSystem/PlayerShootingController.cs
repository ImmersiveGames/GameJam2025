using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    public class PlayerShootingController : MonoBehaviour
    {
        [SerializeField] private PlayerWeaponConfig weaponConfig;
        [SerializeField] private ShootingSpawnPoint shootingSpawnPoint;
        [SerializeField] private KeyCode switchWeaponKey = KeyCode.Q; // Temporário para teste

        private PlayerInputActions _inputActions;
        private int _currentWeaponIndex;

        private void Awake()
        {
            if (weaponConfig == null || weaponConfig.Weapons.Count == 0)
            {
                DebugUtility.LogError<PlayerShootingController>("WeaponConfig não configurado ou vazio.", this);
                enabled = false;
                return;
            }
            if (shootingSpawnPoint == null)
            {
                DebugUtility.LogError<PlayerShootingController>("ShootingSpawnPoint não configurado.", this);
                enabled = false;
                return;
            }

            _inputActions = new PlayerInputActions();
            _currentWeaponIndex = weaponConfig.DefaultWeaponIndex;
            shootingSpawnPoint.Initialize(_inputActions, weaponConfig.Weapons[_currentWeaponIndex]);
        }

        private void OnEnable()
        {
            _inputActions.Player.Enable();
        }

        private void OnDisable()
        {
            _inputActions.Player.Disable();
        }

        private void Update()
        {
            if (Input.GetKeyDown(switchWeaponKey))
            {
                SwitchWeapon();
            }
        }

        private void SwitchWeapon()
        {
            _currentWeaponIndex = (_currentWeaponIndex + 1) % weaponConfig.Weapons.Count;
            shootingSpawnPoint.SetSpawnData(weaponConfig.Weapons[_currentWeaponIndex]);
        }

        public void SetWeapon(int index)
        {
            if (index >= 0 && index < weaponConfig.Weapons.Count)
            {
                _currentWeaponIndex = index;
                shootingSpawnPoint.SetSpawnData(weaponConfig.Weapons[_currentWeaponIndex]);
            }
        }
    }
}