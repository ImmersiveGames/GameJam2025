using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    public interface IDeathHandler
    {
        void ExecuteDeath();
    }

    public class DeathHandler : IDeathHandler
    {
        private readonly DamageReceiver _receiver;

        public DeathHandler(DamageReceiver receiver)
        {
            _receiver = receiver;
        }

        public void ExecuteDeath()
        {
            // CORREÇÃO: Verificar se o sistema está inicializado
            if (!_receiver.ResourceSystemInitialized)
            {
                Debug.LogError($"DeathHandler: Tentativa de executar morte sem sistema inicializado para {_receiver.Actor?.ActorId}");
                return;
            }

            Debug.Log($"DeathHandler: Executando morte para {_receiver.Actor?.ActorId}");

            RaiseDeathEvents();
            SpawnDeathEffect();
            
            // CORREÇÃO: Chamar finalização da morte
            _receiver.FinalizeDeath();
        }

        private void RaiseDeathEvents()
        {
            _receiver.OnEventDeath(_receiver.Actor);
            if (_receiver.InvokeEvents)
            {
                try
                {
                    EventBus<ActorDeathEvent>.Raise(new ActorDeathEvent(_receiver.Actor, _receiver.transform.position));
                    DebugUtility.LogVerbose<DeathHandler>($"DeathHandler: Evento de morte disparado para {_receiver.Actor?.ActorId}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"DeathHandler: Erro ao disparar evento de morte: {ex.Message}");
                }
            }
        }

        private void SpawnDeathEffect()
        {
            if (_receiver.DeathEffect != null)
            {
                try
                {
                    _receiver.DestructionHandler.HandleEffectSpawn(
                        _receiver.DeathEffect, 
                        _receiver.transform.position, 
                        _receiver.transform.rotation
                    );
                    DebugUtility.LogVerbose<DeathHandler>($"DeathHandler: Efeito de morte spawnado para {_receiver.Actor?.ActorId}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"DeathHandler: Erro ao spawnar efeito de morte: {ex.Message}");
                }
            }
        }
    }
}