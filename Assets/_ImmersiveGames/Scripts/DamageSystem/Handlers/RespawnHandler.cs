using System.Collections.Generic;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    public interface IRespawnHandler
    {
        void HandleRespawn();
        void Revive(float healthAmount = -1);
        void ResetToInitialState();
        void CancelRespawn();
    }

    public class RespawnHandler : IRespawnHandler
    {
        private readonly DamageReceiver _receiver;
        private Coroutine _respawnCoroutine;

        public RespawnHandler(DamageReceiver receiver)
        {
            _receiver = receiver;
        }

        public void HandleRespawn()
        {
            if (!_receiver.CanRespawnConfig)
            {
                _receiver.FinalizeDeath();
                return;
            }

            // CORREÇÃO: Usar a estratégia de respawn delayada
            if (_receiver.RespawnTime <= 0f)
            {
                Revive();
            }
            else if (_receiver.RespawnTime > 0f)
            {
                if (_receiver.DeactivateOnDeath && !_receiver.DestroyOnDeath) 
                    _receiver.gameObject.SetActive(false);
                
                // CORREÇÃO: Usar corrotina em vez de Invoke para melhor controle
                _respawnCoroutine = _receiver.StartCoroutine(DelayedRespawn());
            }
            else
            {
                _receiver.FinalizeDeath();
            }
        }

        private System.Collections.IEnumerator DelayedRespawn()
        {
            yield return new WaitForSeconds(_receiver.RespawnTime);
            Revive();
            _respawnCoroutine = null;
        }

        public void Revive(float healthAmount = -1)
        {
            if (!_receiver.IsDead) return;

            // CORREÇÃO: Cancelar corrotina de respawn
            CancelRespawn();

            _receiver.SetDead(false);
            _receiver.SetCanReceiveDamage(true);

            // CORREÇÃO: Ativar objeto se estiver desativado
            if (!_receiver.gameObject.activeSelf) 
                _receiver.gameObject.SetActive(true);

            // Restaurar posição
            var position = _receiver.UseInitialPositionAsRespawn ? 
                _receiver.InitialPosition : _receiver.RespawnPosition;
            _receiver.transform.position = position;
            _receiver.transform.rotation = _receiver.InitialRotation;

            // CORREÇÃO: Restaurar saúde usando ResourceSystem (não ResourceBridge antigo)
            if (_receiver.ResourceSystem != null)
            {
                float reviveHealth = healthAmount >= 0 ? healthAmount : 
                    GetInitialResourceValue(_receiver.PrimaryDamageResource);
                _receiver.ResourceSystem.Set(_receiver.PrimaryDamageResource, reviveHealth);
            }
            else
            {
                Debug.LogWarning($"RespawnHandler: ResourceSystem não disponível para reviver {_receiver.Actor?.ActorId}");
            }

            RaiseReviveEvents();
        }

        public void ResetToInitialState()
        {
            // CORREÇÃO: Cancelar corrotina de respawn
            CancelRespawn();

            _receiver.SetDead(false);
            _receiver.SetCanReceiveDamage(true);

            // CORREÇÃO: Ativar objeto se estiver desativado
            if (!_receiver.gameObject.activeSelf) 
                _receiver.gameObject.SetActive(true);

            // Restaurar posição e rotação
            _receiver.transform.position = _receiver.InitialPosition;
            _receiver.transform.rotation = _receiver.InitialRotation;

            // CORREÇÃO: Restaurar todos os recursos usando ResourceSystem (não ResourceBridge antigo)
            if (_receiver.ResourceSystem != null)
            {
                foreach (KeyValuePair<ResourceType, float> resourceEntry in _receiver.InitialResourceValues)
                {
                    _receiver.ResourceSystem.Set(resourceEntry.Key, resourceEntry.Value);
                }
            }
            else
            {
                Debug.LogWarning($"RespawnHandler: ResourceSystem não disponível para resetar estado de {_receiver.Actor?.ActorId}");
            }

            RaiseReviveEvents();
        }

        public void CancelRespawn()
        {
            if (_respawnCoroutine != null)
            {
                _receiver.StopCoroutine(_respawnCoroutine);
                _respawnCoroutine = null;
            }
        }

        private float GetInitialResourceValue(ResourceType resourceType)
        {
            // CORREÇÃO: Usar valores iniciais armazenados ou máximo do recurso
            if (_receiver.InitialResourceValues.TryGetValue(resourceType, out float value))
            {
                return value;
            }
            
            // Fallback: usar valor máximo do recurso
            return _receiver.ResourceSystem?.Get(resourceType)?.GetMaxValue() ?? 100f;
        }

        private void RaiseReviveEvents()
        {
            _receiver.OnEventRevive(_receiver.Actor);
            if (_receiver.InvokeEvents)
            {
                EventBus<ActorReviveEvent>.Raise(new ActorReviveEvent(_receiver.Actor, _receiver.transform.position));
            }
        }
    }
}