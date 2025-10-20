using System.Collections.Generic;
using _ImmersiveGames.Scripts.DamageSystem.Strategies;
using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.ResourceSystems.Bind;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;

namespace _ImmersiveGames.Scripts.DamageSystem.Commands
{
    public class DamageCommandContext
    {
        public DamageContext Request { get; }
        public ResourceType TargetResource { get; }
        public InjectableEntityResourceBridge Bridge { get; }
        public IDamageStrategy Strategy { get; }
        public DamageCooldownModule CooldownModule { get; }
        public DamageLifecycleModule LifecycleModule { get; }
        public DamageExplosionModule ExplosionModule { get; }
        public ResourceSystem ResourceSystem { get; set; }
        public float CalculatedDamage { get; set; }
        public float PreviousCalculatedDamage { get; set; }
        public float PreviousLastDamageTime { get; set; }
        public bool DamageApplied { get; set; }
        public bool DamageCalculated { get; set; }
        public float? PreviousCooldownTimestamp { get; set; }
        public bool CooldownRegistered { get; set; }
        public DamageEvent? RaisedDamageEvent { get; set; }
        public bool? PreviousDeathState { get; set; }
        public bool DeathStateChanged { get; set; }
        public Dictionary<ResourceType, float> ResourceSnapshot { get; } = new();

        public DamageCommandContext(
            DamageContext request,
            ResourceType targetResource,
            InjectableEntityResourceBridge bridge,
            IDamageStrategy strategy,
            DamageCooldownModule cooldownModule,
            DamageLifecycleModule lifecycleModule,
            DamageExplosionModule explosionModule)
        {
            Request = request;
            TargetResource = targetResource;
            Bridge = bridge;
            Strategy = strategy;
            CooldownModule = cooldownModule;
            LifecycleModule = lifecycleModule;
            ExplosionModule = explosionModule;
            CalculatedDamage = request?.DamageValue ?? 0f;
            PreviousCalculatedDamage = CalculatedDamage;
        }

        public bool HasValidResourceSystem() => ResourceSystem != null;

        public void CaptureResourceSnapshot()
        {
            ResourceSnapshot.Clear();

            if (!HasValidResourceSystem())
            {
                return;
            }

            foreach (var resourceType in ResourceSystem.GetAllRegisteredTypes())
            {
                var value = ResourceSystem.Get(resourceType);
                if (value != null)
                {
                    ResourceSnapshot[resourceType] = value.GetCurrentValue();
                }
            }

            PreviousLastDamageTime = ResourceSystem.LastDamageTime;
        }

        public void RestoreResourceSnapshot()
        {
            if (!HasValidResourceSystem() || ResourceSnapshot.Count == 0)
            {
                return;
            }

            foreach (var snapshot in ResourceSnapshot)
            {
                ResourceSystem.Set(snapshot.Key, snapshot.Value);
            }

            ResourceSystem.RestoreLastDamageTime(PreviousLastDamageTime);
            ResourceSnapshot.Clear();
        }
    }
}
