using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.ResourceSystems;
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

            // Correção: Usar a estratégia de respawn delayada
            switch (_receiver.RespawnTime)
            {
                case 0f:
                    Revive();
                    break;
                case > 0f:
                    if (_receiver.DeactivateOnDeath && !_receiver.DestroyOnDeath) 
                        _receiver.gameObject.SetActive(false);
                    _receiver.Invoke(nameof(_receiver.ExecuteDelayedRespawn), _receiver.RespawnTime);
                    break;
                default:
                    _receiver.FinalizeDeath();
                    break;
            }
        }

        public void Revive(float healthAmount = -1)
        {
            if (!_receiver.IsDead) return;

            _receiver.CancelInvoke(nameof(_receiver.ExecuteDelayedRespawn));
            _receiver.SetDead(false);
            _receiver.SetCanReceiveDamage(true);

            if (!_receiver.gameObject.activeSelf) _receiver.gameObject.SetActive(true);

            // Restaurar posição
            var position = _receiver.UseInitialPositionAsRespawn ? 
                _receiver.InitialPosition : _receiver.RespawnPosition;
            _receiver.transform.position = position;
            _receiver.transform.rotation = _receiver.InitialRotation;

            // Restaurar saúde
            float reviveHealth = healthAmount >= 0 ? healthAmount : 
                GetInitialResourceValue(_receiver.PrimaryDamageResource);
            _receiver.ResourceBridge?.GetService().Set(_receiver.PrimaryDamageResource, reviveHealth);

            RaiseReviveEvents();
        }

        public void ResetToInitialState()
        {
            _receiver.CancelInvoke(nameof(_receiver.ExecuteDelayedRespawn));
            _receiver.SetDead(false);
            _receiver.SetCanReceiveDamage(true);

            if (!_receiver.gameObject.activeSelf) _receiver.gameObject.SetActive(true);

            // Restaurar posição e rotação
            _receiver.transform.position = _receiver.InitialPosition;
            _receiver.transform.rotation = _receiver.InitialRotation;

            // Restaurar todos os recursos
            if (_receiver.ResourceBridge != null)
            {
                var resourceSystem = _receiver.ResourceBridge.GetService();
                foreach (var resourceEntry in _receiver.InitialResourceValues)
                {
                    resourceSystem.Set(resourceEntry.Key, resourceEntry.Value);
                }
            }

            RaiseReviveEvents();
        }

        public void CancelRespawn()
        {
            _receiver.CancelInvoke(nameof(_receiver.ExecuteDelayedRespawn));
        }

        private float GetInitialResourceValue(ResourceType resourceType)
        {
            return _receiver.InitialResourceValues.TryGetValue(resourceType, out float value)
                ? value
                : _receiver.ResourceBridge?.GetService().Get(resourceType)?.GetMaxValue() ?? 100f;
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