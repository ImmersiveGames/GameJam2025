using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    public interface IDamageValidator
    {
        bool CanReceiveDamage(float damage, IActor damageSource, ResourceType targetResource);
    }

    public class DamageValidator : IDamageValidator
    {
        private readonly DamageReceiver _receiver;

        public DamageValidator(DamageReceiver receiver)
        {
            _receiver = receiver;
        }

        public bool CanReceiveDamage(float damage, IActor damageSource, ResourceType targetResource)
        {
            // CORREÇÃO: Verificar se o sistema está inicializado
            if (!_receiver.ResourceSystemInitialized)
            {
                Debug.LogWarning($"DamageValidator: Sistema não inicializado para {_receiver.Actor?.ActorId}");
                return false;
            }

            if (!_receiver.CanReceiveDamage || _receiver.IsDead)
            {
                DebugUtility.LogVerbose<DamageValidator>($"DamageValidator: Não pode receber dano - CanReceiveDamage: {_receiver.CanReceiveDamage}, IsDead: {_receiver.IsDead}");
                return false;
            }

            // CORREÇÃO: Verificar se o dano é válido
            if (damage <= 0f)
            {
                DebugUtility.LogVerbose<DamageValidator>($"DamageValidator: Dano inválido: {damage}");
                return false;
            }

            // CORREÇÃO: Verificar layer do source se disponível
            if (damageSource is MonoBehaviour sourceBehaviour)
            {
                if (!IsInDamageableLayer(sourceBehaviour.gameObject))
                {
                    DebugUtility.LogVerbose<DamageValidator>($"DamageValidator: Source não está em layer damageable: {sourceBehaviour.gameObject.layer}");
                    return false;
                }
            }

            // CORREÇÃO: Verificar se o recurso alvo existe
            if (_receiver.ResourceSystem != null && targetResource != ResourceType.None)
            {
                var targetResourceValue = _receiver.ResourceSystem.Get(targetResource);
                if (targetResourceValue == null)
                {
                    Debug.LogWarning($"DamageValidator: Recurso alvo não encontrado: {targetResource}");
                    return false;
                }
            }

            DebugUtility.LogVerbose<DamageValidator>($"DamageValidator: Dano permitido - {damage} de {damageSource?.ActorId} para {targetResource}");
            return true;
        }

        private bool IsInDamageableLayer(GameObject sourceGameObject)
        {
            return (_receiver.DamageableLayers.value & (1 << sourceGameObject.layer)) != 0;
        }
    }
}