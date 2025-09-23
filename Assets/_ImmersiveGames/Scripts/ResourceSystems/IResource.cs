using System;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.NewResourceSystem;
using _ImmersiveGames.Scripts.NewResourceSystem.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public interface IResource : IResourceValue, IResourceThreshold
    {
        ResourceConfigSo Config { get; }
        ResourceType Type { get; }
        string UniqueId { get; }
        string ActorId { get; }
        GameObject Source { get; }
        void AddModifier(float amountPerSecond, float duration, bool isPermanent = false);
        void RemoveAllModifiers();
        void Reset(bool resetSkin = true);
    }
    /// <summary>
    /// Interface para gerenciamento de limiares (thresholds) de recursos.
    /// </summary>
    public interface IResourceThreshold
    {
        void CheckThresholds();
    }
    public interface IResourceModifier
    {
        void AddModifier(float amountPerSecond, float duration, bool isPermanent = false);
        void RemoveAllModifiers();
        float UpdateAndGetDelta(float deltaTime, float baseRate = 0f);
    }

    /// <summary>
    /// Interface para comportamentos específicos de recursos de saúde (cura, dano, morte).
    /// </summary>
    public interface IHealthSpecific
    {
        void Heal(float amount, IActor byActor);
        void TakeDamage(float amount, IActor byActor);
        void OnDeath();
    }

    /// <summary>
    /// Interface para reiniciar recursos ao estado padrão.
    /// </summary>
    public interface IResettable
    {
        void Reset(bool resetSkin = true);
    }
    public interface IAutoChangeStrategy
    {
        float GetBaseRate(ResourceConfigSo config);
    }
    
}