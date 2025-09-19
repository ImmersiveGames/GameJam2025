using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class ModifierManager : MonoBehaviour
    {
        private readonly List<ResourceModifier> _modifiers = new();
        private IModifierStrategy _strategy = new DefaultModifierStrategy();

        public void AddModifier(float amountPerSecond, float duration, bool isPermanent = false, IActor appliedBy = null)
        {
            var modifier = new ResourceModifier(amountPerSecond, duration, isPermanent);
            _modifiers.Add(modifier);
            var actorId = GetActorId();
            var uniqueId = GetComponent<ResourceSystem>()?.Config.UniqueId ?? "Unknown";
            EventBus<ModifierAppliedEvent>.Raise(new ModifierAppliedEvent(uniqueId, gameObject, modifier, true, actorId, appliedBy));
            DebugUtility.LogVerbose<ModifierManager>($"AddModifier: AmountPerSecond={amountPerSecond:F2}, Duration={duration:F2}, IsPermanent={isPermanent}, ActorId={actorId}, Source={gameObject.name}");
            // TODO: Injetar strategy via DI para customizações
        }

        public void RemoveAllModifiers()
        {
            var actorId = GetActorId();
            var uniqueId = GetComponent<ResourceSystem>()?.Config.UniqueId ?? "Unknown";
            foreach (var modifier in _modifiers.ToArray())
            {
                EventBus<ModifierAppliedEvent>.Raise(new ModifierAppliedEvent(uniqueId, gameObject, modifier, false, actorId));
            }
            _modifiers.Clear();
            DebugUtility.LogVerbose<ModifierManager>($"RemoveAllModifiers: Todos removidos, Source={gameObject.name}");
        }

        public float UpdateAndGetDelta(float deltaTime, float baseValue = 0f)
        {
            float totalDelta = baseValue;
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                var modifier = _modifiers[i];
                totalDelta += _strategy.Apply(modifier, deltaTime);
                if (modifier.Update(deltaTime))
                {
                    EventBus<ModifierAppliedEvent>.Raise(new ModifierAppliedEvent(GetComponent<ResourceSystem>()?.Config.UniqueId ?? "Unknown", gameObject, modifier, false, GetActorId()));
                    _modifiers.RemoveAt(i);
                }
            }
            return totalDelta;
        }

        private class DefaultModifierStrategy : IModifierStrategy
        {
            public float Apply(ResourceModifier modifier, float deltaTime, float baseValue = 0f)
            {
                var delta = modifier.amountPerSecond * deltaTime;
                DebugUtility.LogVerbose<DefaultModifierStrategy>($"Apply: Delta={delta:F5}, AmountPerSecond={modifier.amountPerSecond}, BaseValue={baseValue:F5}");
                return delta;
            }
        }

        private string GetActorId()
        {
            var actor = GetComponentInParent<IActor>();
            return actor?.Name ?? "";
            // TODO: Injetar via DI para consulta mais flexível
        }
    }
}