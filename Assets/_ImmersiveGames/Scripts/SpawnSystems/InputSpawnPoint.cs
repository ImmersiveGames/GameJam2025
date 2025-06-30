using _ImmersiveGames.Scripts.SpawnSystems.DynamicPropertiesSystem;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    [DebugLevel(DebugLevel.Warning)]
    public class InputSpawnPoint : SpawnPoint
    {
        [SerializeField] private PlayerInput playerInput;

        protected override void Awake()
        {
            base.Awake();
            if (playerInput == null)
            {
                DebugUtility.LogWarning<InputSpawnPoint>($"PlayerInput não configurado em '{name}'. InputSystemTrigger não funcionará.", this);
            }
        }

        protected override void InitializeTrigger()
        {
            InputActionAsset inputAsset = playerInput != null ? playerInput.actions : null;
            SpawnTrigger = EnhancedSpawnFactory.Instance.CreateTrigger(triggerData, inputAsset);
            if (SpawnTrigger == null)
            {
                DebugUtility.LogError<InputSpawnPoint>($"Falha ao criar trigger para {triggerData?.triggerType} em '{name}'.", this);
                return;
            }
            SpawnTrigger.Initialize(this);
        }
    }
}