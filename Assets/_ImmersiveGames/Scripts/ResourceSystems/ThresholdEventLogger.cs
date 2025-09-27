using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class ThresholdEventLogger : MonoBehaviour
    {
        private void Start()
        {
            var binding = new EventBinding<ResourceThresholdEvent>(OnThresholdEvent);
            EventBus<ResourceThresholdEvent>.Register(binding);
            DebugUtility.LogVerbose<ThresholdEventLogger>($"👂 Ouvindo eventos de threshold");
        }

        private void OnThresholdEvent(ResourceThresholdEvent evt)
        {
            DebugUtility.LogVerbose<ThresholdEventLogger>(
                $"🎯 THRESHOLD EVENT: {evt.ActorId}.{evt.ResourceType} " +
                $"cruzou {evt.Threshold:P0} ({(evt.IsAscending ? "SUBINDO" : "DESCENDO")}) " +
                $"Current: {evt.CurrentPercentage:P2}");
        }

        private void OnDestroy()
        {
            DebugUtility.LogVerbose<ThresholdEventLogger>($"👂 ThresholdEventLogger destruído");
        }
    }
}