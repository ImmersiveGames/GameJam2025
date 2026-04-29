using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.PreferencesRuntime.Contracts;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.PreferencesRuntime.Config
{
    [CreateAssetMenu(
        fileName = "VideoDefaults",
        menuName = "ImmersiveGames/NewScripts/Preferences/Video Defaults",
        order = 4)]
    public sealed class VideoDefaultsAsset : ScriptableObject
    {
        [Header("Defaults")]
        [SerializeField] private int defaultResolutionWidth = 1920;
        [SerializeField] private int defaultResolutionHeight = 1080;
        [SerializeField] private bool defaultFullscreen = true;

        [Header("Common Presets")]
        [SerializeField] private List<Vector2Int> resolutionPresets = new()
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1366, 768),
            new Vector2Int(1600, 900),
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440),
        };

        public int DefaultResolutionWidth => defaultResolutionWidth;
        public int DefaultResolutionHeight => defaultResolutionHeight;
        public bool DefaultFullscreen => defaultFullscreen;
        public IReadOnlyList<Vector2Int> ResolutionPresets => resolutionPresets;

        public VideoPreferencesSnapshot CreateDefaultSnapshot(string profileId, string slotId)
        {
            return new VideoPreferencesSnapshot(
                profileId,
                slotId,
                defaultResolutionWidth,
                defaultResolutionHeight,
                defaultFullscreen);
        }

        private void OnValidate()
        {
            if (defaultResolutionWidth <= 0)
            {
                throw new InvalidOperationException("[FATAL][Config][Video] defaultResolutionWidth must be positive.");
            }

            if (defaultResolutionHeight <= 0)
            {
                throw new InvalidOperationException("[FATAL][Config][Video] defaultResolutionHeight must be positive.");
            }

            if (resolutionPresets == null || resolutionPresets.Count == 0)
            {
                throw new InvalidOperationException("[FATAL][Config][Video] resolutionPresets must contain at least one preset.");
            }

            bool foundDefault = false;
            var seen = new HashSet<Vector2Int>();
            for (int i = 0; i < resolutionPresets.Count; i++)
            {
                Vector2Int preset = resolutionPresets[i];
                if (preset.x <= 0 || preset.y <= 0)
                {
                    throw new InvalidOperationException($"[FATAL][Config][Video] resolutionPresets[{i}] must be positive.");
                }

                if (!seen.Add(preset))
                {
                    throw new InvalidOperationException($"[FATAL][Config][Video] duplicate video preset detected: {preset.x}x{preset.y}.");
                }

                if (preset.x == defaultResolutionWidth && preset.y == defaultResolutionHeight)
                {
                    foundDefault = true;
                }
            }

            if (!foundDefault)
            {
                throw new InvalidOperationException("[FATAL][Config][Video] default resolution must be present in resolutionPresets.");
            }
        }
    }
}

