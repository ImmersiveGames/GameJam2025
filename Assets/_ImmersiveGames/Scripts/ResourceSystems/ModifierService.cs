using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class ModifierService : IResourceModifier
    {
        private readonly List<ResourceModifier> _modifiers = new();
        private readonly string _uniqueId;
        private readonly GameObject _source;
        private readonly string _actorId;

        public ModifierService(string uniqueId, GameObject source, string actorId)
        {
            _uniqueId = uniqueId;
            _source = source;
            _actorId = actorId;
        }

        public void AddModifier(float amountPerSecond, float duration, bool isPermanent = false)
        {
            var modifier = new ResourceModifier(amountPerSecond, duration, isPermanent);
            _modifiers.Add(modifier);
            EventBus<ModifierAppliedEvent>.Raise(new ModifierAppliedEvent(_uniqueId, _source, modifier, true, _actorId));
            DebugUtility.LogVerbose<ModifierService>($"AddModifier: AmountPerSecond={amountPerSecond:F2}, Duration={duration:F2}, IsPermanent={isPermanent}, UniqueId={_uniqueId}, Source={_source.name}");
        }

        public void RemoveAllModifiers()
        {
            foreach (var modifier in _modifiers)
            {
                EventBus<ModifierAppliedEvent>.Raise(new ModifierAppliedEvent(_uniqueId, _source, modifier, false, _actorId));
            }
            _modifiers.Clear();
            DebugUtility.LogVerbose<ModifierService>($"RemoveAllModifiers: Cleared all modifiers, UniqueId={_uniqueId}, Source={_source.name}");
        }

        public float UpdateAndGetDelta(float deltaTime, float baseRate = 0f)
        {
            float delta = baseRate;
            var expiredModifiers = new List<ResourceModifier>();

            foreach (var modifier in _modifiers)
            {
                if (!modifier.isPermanent)
                {
                    if (modifier.Update(deltaTime))
                    {
                        expiredModifiers.Add(modifier);
                        continue;
                    }
                }
                delta += modifier.amountPerSecond * deltaTime;
            }

            foreach (var modifier in expiredModifiers)
            {
                _modifiers.Remove(modifier);
                EventBus<ModifierAppliedEvent>.Raise(new ModifierAppliedEvent(_uniqueId, _source, modifier, false, _actorId));
            }

            DebugUtility.LogVerbose<ModifierService>($"UpdateAndGetDelta: Delta={delta:F2}, ActiveModifiers={_modifiers.Count}");
            return delta;
        }
    }
}