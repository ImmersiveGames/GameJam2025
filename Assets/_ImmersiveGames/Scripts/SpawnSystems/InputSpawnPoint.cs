using _ImmersiveGames.Scripts.GameManagerSystems;
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
                DebugUtility.LogWarning<InputSpawnPoint>($"PlayerInput não configurado em '{name}'. InputSystemTriggerOld não funcionará.", this);
            }
        }

        protected override void InitializeTrigger()
        {
            InputActionAsset inputAsset = playerInput != null ? playerInput.actions : null;
            SpawnTriggerOld = EnhancedSpawnFactory.Instance.CreateTrigger(triggerData, inputAsset);
            if (SpawnTriggerOld == null)
            {
                DebugUtility.LogError<InputSpawnPoint>($"Falha ao criar trigger para {triggerData?.triggerType} em '{name}'.", this);
                return;
            }
            SpawnTriggerOld.Initialize(this);
        }

        protected override void Update()
        {
            if (!GameManager.Instance.ShouldPlayingGame())
            {
                DebugUtility.LogVerbose<InputSpawnPoint>($"Input ignorado em '{name}' porque o jogo não está no estado de execução.", "yellow", this);
                return;
            }

            base.Update();
        }
    }
}