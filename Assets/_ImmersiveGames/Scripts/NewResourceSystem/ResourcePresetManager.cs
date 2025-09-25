using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.NewResourceSystem
{
    public class ResourcePresetManager : MonoBehaviour
    {
        public Dictionary<string, List<ResourceConfig>> Presets { get; } = new()
        {
            { "warrior", new List<ResourceConfig>
                {
                    new() { Type = ResourceType.Health, InitialValue = 150, MaxValue = 150, Enabled = true },
                    new() { Type = ResourceType.Stamina, InitialValue = 120, MaxValue = 120, Enabled = true },
                    new() { Type = ResourceType.Mana, InitialValue = 30, MaxValue = 30, Enabled = true }
                }
            },
            { "mage", new List<ResourceConfig>
                {
                    new() { Type = ResourceType.Health, InitialValue = 80, MaxValue = 80, Enabled = true },
                    new() { Type = ResourceType.Stamina, InitialValue = 60, MaxValue = 60, Enabled = true },
                    new() { Type = ResourceType.Mana, InitialValue = 100, MaxValue = 100, Enabled = true }
                }
            },
            { "archer", new List<ResourceConfig>
                {
                    new() { Type = ResourceType.Health, InitialValue = 100, MaxValue = 100, Enabled = true },
                    new() { Type = ResourceType.Stamina, InitialValue = 100, MaxValue = 100, Enabled = true },
                    new() { Type = ResourceType.Energy, InitialValue = 50, MaxValue = 50, Enabled = true }
                }
            }
        };

        public void ApplyPreset(EntityResourceSystem system, string presetName)
        {
            if (Presets.TryGetValue(presetName.ToLower(), out var configs))
            {
                system.ClearResources();
                foreach (var config in configs)
                {
                    if (config.Enabled)
                        system.AddResource(config.Type, config.InitialValue, config.MaxValue);
                }
            }
            else
            {
                DebugUtility.LogWarning<EntityResourceSystem>($"Unknown preset: {presetName}");
            }
        }
    }
}