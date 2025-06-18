using UnityEngine;
using UnityEngine.InputSystem;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public class PlayerInputSpawnPoint : SpawnPoint
    {
        [SerializeField] private PlayerInput playerInput;

        protected override void Awake()
        {
            if (playerInput == null)
            {
                DebugUtility.LogError<PlayerInputSpawnPoint>("PlayerInput não configurado.", this);
                enabled = false;
                return;
            }

            if (triggerData == null)
            {
                DebugUtility.LogError<PlayerInputSpawnPoint>("TriggerData não configurado.", this);
                enabled = false;
                return;
            }

            base.Awake();
        }

        /*protected override void InitializeTrigger()
        {
            _trigger = SpawnFactory.Instance.CreateTrigger(triggerData, playerInput.actions);
            if (_trigger == null)
            {
                DebugUtility.LogError<PlayerInputSpawnPoint>($"Falha ao criar trigger para {triggerData.triggerType}.", this);
                enabled = false;
                return;
            }
            _trigger.Initialize(this);
        }*/
    }
}