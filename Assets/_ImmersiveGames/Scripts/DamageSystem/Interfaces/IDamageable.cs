using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DamageSystem.Services;
using _ImmersiveGames.Scripts.ResourceSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.DamageSystem
{
    public interface IDamageable
    {
        void ReceiveDamage(float damage, IActor damageSource = null, ResourceType targetResource = ResourceType.None);
        bool IsDead { get; }
        bool CanReceiveDamage { get; }
        IActor Actor { get; }
        float CurrentHealth { get; }
    }

    public interface IDamageSource
    {
        float DamageAmount { get; }
        ResourceType DamageResourceType { get; }
        DamageType DamageType { get; }
        IActor DamageSourceActor { get; }
        LayerMask DamageableLayers { get; }
    }

    public interface IRespawnable
    {
        void Revive(float healthAmount = -1);
        void ResetToInitialState();
        bool CanRespawn { get; }
        bool IsDead { get; }
    }
    public interface IRespawnStrategy // Novo
    {
        void Execute(DamageReceiver receiver);
    }
    
    public interface IDestructionHandler
    {
        void HandleDestruction(GameObject target, bool spawnEffects = true);
        void HandleEffectSpawn(GameObject effectPrefab, Vector3 position, Quaternion rotation);
    }
    public interface IDamageModifier
    {
        float Modify(DamageContext ctx);
    }
    public enum DamageType
    {
        Physical,
        Magical,
        Fire,
        Ice,
        Lightning,
        Poison
    }
    public class DamageContext
    {
        public IActor Source { get; set; }
        public IActor Target { get; set; }
        public float Amount { get; set; }
        public DamageType DamageType { get; set; }
        public ResourceType ResourceType { get; set; } = ResourceType.Health;
        public Vector3 HitPosition { get; set; } = Vector3.zero;
    }
}