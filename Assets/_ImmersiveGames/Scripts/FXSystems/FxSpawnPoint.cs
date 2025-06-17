using _ImmersiveGames.Scripts.GameManagerSystems.EventsBus;
using _ImmersiveGames.Scripts.SpawnSystems;
using _ImmersiveGames.Scripts.SpawnSystems.Interfaces;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using System.Linq;

namespace _ImmersiveGames.Scripts.FXSystems
{
    [DebugLevel(DebugLevel.Warning)]
    public class FxSpawnPoint : SpawnPoint
    {
        private EventBinding<DeathEvent> _deathEventBinding;

        protected override void OnEnable()
        {
            base.OnEnable();
            _deathEventBinding = new EventBinding<DeathEvent>(OnDeathEvent);
            EventBus<DeathEvent>.Register(_deathEventBinding);
            DebugUtility.Log<FxSpawnPoint>($"FxSpawnPoint '{name}': Registrado para DeathEvent.", "green", this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_deathEventBinding != null)
            {
                EventBus<DeathEvent>.Unregister(_deathEventBinding);
                DebugUtility.Log<FxSpawnPoint>($"FxSpawnPoint '{name}': Desregistrado de DeathEvent.", "green", this);
            }
        }

        protected override void InitializeTrigger()
        {
            // Forçar o uso de DeathEventTrigger
            if (triggerData == null)
            {
                triggerData = ScriptableObject.CreateInstance<TriggerData>();
                triggerData.triggerType = TriggerType.DeathEventTrigger;
            }
            else if (triggerData.triggerType != TriggerType.DeathEventTrigger)
            {
                DebugUtility.LogWarning<FxSpawnPoint>($"TriggerData especificado não é DeathEventTrigger. Forçando DeathEventTrigger para '{name}'.", this);
                triggerData.triggerType = TriggerType.DeathEventTrigger;
            }

            base.InitializeTrigger();
            if (_trigger is not DeathEventTrigger)
            {
                DebugUtility.LogError<FxSpawnPoint>($"Trigger deve ser DeathEventTrigger para '{name}'.", this);
                enabled = false;
            }
        }

        private void OnDeathEvent(DeathEvent evt)
        {
            if (!spawnData || _trigger == null)
            {
                DebugUtility.LogWarning<FxSpawnPoint>($"SpawnData ou Trigger não configurado em '{name}'.", this);
                return;
            }

            // Passa a posição do DeathEvent para o trigger
            _trigger.CheckTrigger(evt.Position, spawnData);
        }

        protected override void HandleSpawnRequest(SpawnRequestEvent evt)
        {
            if (evt.Data != spawnData)
                return;

            // Chama a lógica base do SpawnPoint
            base.HandleSpawnRequest(evt);

            // Ajustar a escala do objeto spawnado
            if (evt.SourceGameObject != null)
            {
                var pool = poolManager.GetPool(spawnData.PoolableData.ObjectName);
                if (pool != null)
                {
                    var activeObjects = pool.GetActiveObjects();
                    var lastObject = activeObjects.LastOrDefault();
                    if (lastObject != null)
                    {
                        var explosionObject = lastObject.GetGameObject();
                        explosionObject.transform.localScale = evt.SourceGameObject.transform.localScale;
                        DebugUtility.Log<FxSpawnPoint>($"Explosão ajustada para escala {explosionObject.transform.localScale} do objeto {evt.SourceGameObject.name}.", "green", this);
                    }
                    else
                    {
                        DebugUtility.LogWarning<FxSpawnPoint>($"Nenhum objeto ativo encontrado para '{spawnData.PoolableData.ObjectName}' após spawn.", this);
                    }
                }
            }
            else
            {
                DebugUtility.LogWarning<FxSpawnPoint>($"SourceGameObject é nulo para '{spawnData.PoolableData.ObjectName}'. Usando escala padrão.", this);
            }

            // Resetar o trigger para evitar spawns duplicados
            _trigger?.Reset();
            DebugUtility.Log<FxSpawnPoint>($"Trigger resetado após spawn em '{name}'.", "green", this);
        }
    }
}