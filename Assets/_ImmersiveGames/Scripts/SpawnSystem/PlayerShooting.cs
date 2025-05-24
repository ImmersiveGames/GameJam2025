using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.SpawnSystem;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public class PlayerShooting : MonoBehaviour
    {
        [SerializeField] private ObjectSpawner spawner;
        [SerializeField] private List<SpawnStrategy> shootingStrategies;
        [SerializeField] private int initialStrategyIndex = 0;
        [SerializeField] private bool debugMode;

        private PlayerInputActions inputActions;

        private void Awake()
        {
            inputActions = new PlayerInputActions();
            if (spawner == null)
            {
                DebugUtility.LogError<PlayerShooting>("Spawner não configurado.", this);
                enabled = false;
                return;
            }
            if (shootingStrategies == null || shootingStrategies.Count == 0)
            {
                DebugUtility.LogError<PlayerShooting>("Nenhuma estratégia configurada em shootingStrategies.", this);
                enabled = false;
                return;
            }
            if (shootingStrategies[initialStrategyIndex] == null || shootingStrategies[initialStrategyIndex].ProjectileData == null)
            {
                DebugUtility.LogError<PlayerShooting>("SpawnStrategy ou ProjectileData inválido na estratégia inicial.", this);
                enabled = false;
                return;
            }

            SetInitialStrategy();
        }

        private void SetInitialStrategy()
        {
            int index = Mathf.Clamp(initialStrategyIndex, 0, shootingStrategies.Count - 1);
            spawner.SetStrategy(shootingStrategies[index]);
            if (debugMode)
            {
                DebugUtility.LogVerbose<PlayerShooting>($"Estratégia inicial configurada: {shootingStrategies[index].name}.", "cyan", this);
            }
        }

        private void OnEnable()
        {
            inputActions.Player.Enable();
            inputActions.Player.Fire.performed += OnFirePerformed;
            inputActions.Player.Fire.canceled += OnFireCanceled;
            if (debugMode)
            {
                DebugUtility.LogVerbose<PlayerShooting>("Input habilitado.", "blue", this);
            }
        }

        private void OnDisable()
        {
            inputActions.Player.Disable();
            inputActions.Player.Fire.performed -= OnFirePerformed;
            inputActions.Player.Fire.canceled -= OnFireCanceled;
            if (debugMode)
            {
                DebugUtility.LogVerbose<PlayerShooting>("Input desabilitado.", "blue", this);
            }
        }

        private void OnFirePerformed(InputAction.CallbackContext context)
        {
            if (debugMode)
            {
                DebugUtility.LogVerbose<PlayerShooting>("Input de disparo detectado.", "green", this);
            }
            spawner.StartFiring();
        }

        private void OnFireCanceled(InputAction.CallbackContext context)
        {
            if (debugMode)
            {
                DebugUtility.LogVerbose<PlayerShooting>("Input de disparo cancelado.", "red", this);
            }
            spawner.StopFiring();
        }

        public void SetStrategy(int strategyIndex)
        {
            if (strategyIndex >= 0 && strategyIndex < shootingStrategies.Count)
            {
                if (shootingStrategies[strategyIndex] == null || shootingStrategies[strategyIndex].ProjectileData == null)
                {
                    DebugUtility.LogWarning<PlayerShooting>($"SpawnStrategy ou ProjectileData inválido no índice {strategyIndex}.", this);
                    return;
                }
                spawner.SetStrategy(shootingStrategies[strategyIndex]);
                if (debugMode)
                {
                    DebugUtility.LogVerbose<PlayerShooting>($"Estratégia alterada para {shootingStrategies[strategyIndex].name}.", "cyan", this);
                }
            }
            else
            {
                DebugUtility.LogWarning<PlayerShooting>($"Índice de estratégia inválido: {strategyIndex}.", this);
            }
        }
    }
}