using System;
using _ImmersiveGames.Scripts.ActorSystems;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    /// <summary>
    /// Interface para gerenciamento de valores de recursos (aumentar, diminuir, obter valores).
    /// </summary>
    public interface IResourceValue
    {
        void Increase(float amount);
        void Decrease(float amount);
        float GetCurrentValue();
        float GetMaxValue();
        float GetPercentage();
    }

    /// <summary>
    /// Interface para gerenciamento de limiares (thresholds) de recursos.
    /// </summary>
    public interface IResourceThreshold
    {
        void CheckThresholds();
        event Action<float> OnThresholdReached;
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
}