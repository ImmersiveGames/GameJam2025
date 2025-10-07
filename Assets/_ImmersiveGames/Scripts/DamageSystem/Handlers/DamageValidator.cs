using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems;
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
            if (!_receiver.CanReceiveDamage || _receiver.IsDead)
                return false;

            if (damageSource is MonoBehaviour sourceBehaviour &&
                !IsInDamageableLayer(sourceBehaviour.gameObject))
                return false;

            return true;
        }

        private bool IsInDamageableLayer(GameObject sourceGameObject)
        {
            return (_receiver.DamageableLayers.value & (1 << sourceGameObject.layer)) != 0;
        }
    }
}