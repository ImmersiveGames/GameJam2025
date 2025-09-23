using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class ModifierManager
    {
        private readonly List<ResourceModifier> _modifiers = new();
        private readonly string _uniqueId;
        private readonly GameObject _source;
        private readonly string _actorId;

        public ModifierManager(string uniqueId, GameObject source, string actorId)
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
            DebugUtility.LogVerbose<ModifierManager>($"AddModifier: amountPerSecond={amountPerSecond:F2}, duration={duration:F2}, isPermanent={isPermanent}, UniqueId={_uniqueId}, ActorId={_actorId}");
        }

        public void RemoveAllModifiers()
        {
            foreach (var modifier in _modifiers.ToArray())
            {
                _modifiers.Remove(modifier);
                EventBus<ModifierAppliedEvent>.Raise(new ModifierAppliedEvent(_uniqueId, _source, modifier, false, _actorId));
            }
            DebugUtility.LogVerbose<ModifierManager>($"RemoveAllModifiers: Todos os modificadores removidos, UniqueId={_uniqueId}, ActorId={_actorId}");
        }

        public float UpdateAndGetDelta(float deltaTime, float baseAmount = 0f)
        {
            float totalDelta = baseAmount;
            foreach (var modifier in _modifiers.ToArray())
            {
                totalDelta += modifier.amountPerSecond * deltaTime;
                if (!modifier.isPermanent && modifier.Update(deltaTime))
                {
                    _modifiers.Remove(modifier);
                    EventBus<ModifierAppliedEvent>.Raise(new ModifierAppliedEvent(_uniqueId, _source, modifier, false, _actorId));
                }
            }
            return totalDelta;
        }
    }
}