using System.Collections.Generic;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation.Binders;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Configs;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Services;

namespace _ImmersiveGames.Scripts.DamageSystem.Commands
{
    public class DamageCommandContext
    {
        public DamageContext Request { get; }
        public RuntimeAttributeType TargetRuntimeAttribute { get; }
        public RuntimeAttributeController Component { get; }
        public IDamageStrategy Strategy { get; }
        public DamageCooldownModule CooldownModule { get; }
        public RuntimeAttributeContext RuntimeAttributeContext { get; set; }
        public float CalculatedDamage { get; set; }
        public float PreviousCalculatedDamage { get; set; }
        private float PreviousLastDamageTime { get; set; }
        public bool DamageApplied { get; set; }
        public bool DamageCalculated { get; set; }
        public float? PreviousCooldownTimestamp { get; set; }
        public bool CooldownRegistered { get; set; }
        public DamageEvent? RaisedDamageEvent { get; set; }
        public bool? PreviousDeathState { get; set; }
        public bool DeathStateChanged { get; set; }
        private Dictionary<RuntimeAttributeType, float> ResourceSnapshot { get; } = new();

        public DamageCommandContext(
            DamageContext request,
            RuntimeAttributeType targetRuntimeAttribute,
            RuntimeAttributeController component,
            IDamageStrategy strategy,
            DamageCooldownModule cooldownModule)
        {
            Request = request;
            TargetRuntimeAttribute = targetRuntimeAttribute;
            Component = component;
            Strategy = strategy;
            CooldownModule = cooldownModule;
            CalculatedDamage = request?.damageValue ?? 0f;
            PreviousCalculatedDamage = CalculatedDamage;
        }

        public bool HasValidResourceSystem() => RuntimeAttributeContext != null;

        public void CaptureResourceSnapshot()
        {
            ResourceSnapshot.Clear();

            if (!HasValidResourceSystem())
            {
                return;
            }

            foreach (var resourceType in RuntimeAttributeContext.GetAllRegisteredTypes())
            {
                var value = RuntimeAttributeContext.Get(resourceType);
                if (value != null)
                {
                    ResourceSnapshot[resourceType] = value.GetCurrentValue();
                }
            }

            PreviousLastDamageTime = RuntimeAttributeContext.LastDamageTime;
        }

        public void RestoreResourceSnapshot()
        {
            if (!HasValidResourceSystem() || ResourceSnapshot.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<RuntimeAttributeType, float> snapshot in ResourceSnapshot)
            {
                RuntimeAttributeContext.Set(snapshot.Key, snapshot.Value);
            }

            RuntimeAttributeContext.RestoreLastDamageTime(PreviousLastDamageTime);
            ResourceSnapshot.Clear();
        }
    }
}
