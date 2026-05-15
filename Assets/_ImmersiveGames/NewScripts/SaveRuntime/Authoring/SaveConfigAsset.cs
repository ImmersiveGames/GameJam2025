using System;
using _ImmersiveGames.NewScripts.SaveRuntime.Models;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SaveRuntime.Authoring
{
    [CreateAssetMenu(
        fileName = "SaveConfig",
        menuName = "ImmersiveGames/NewScripts/Save/Save Config",
        order = 10)]
    public sealed class SaveConfigAsset : ScriptableObject
    {
        [SerializeField] private string defaultProfileId = "default";
        [SerializeField] private string defaultSlotId = "save";
        [SerializeField] private int schemaVersion = 1;
        [SerializeField] private SaveBackendAsset backend;

        public string DefaultProfileId => Normalize(defaultProfileId);
        public string DefaultSlotId => Normalize(defaultSlotId);
        public int SchemaVersion => schemaVersion;
        public SaveBackendAsset Backend => backend;

        public SaveCurrentState BuildDefaultCurrentStateOrFail()
        {
            ValidateOrThrow();
            return new SaveCurrentState(
                DefaultProfileId,
                DefaultSlotId,
                SchemaVersion,
                revision: 0,
                savedAtUtc: DateTime.UtcNow.ToString("O"));
        }

        public void ValidateOrThrow()
        {
            if (string.IsNullOrWhiteSpace(DefaultProfileId))
            {
                throw new InvalidOperationException($"SaveConfigAsset '{name}' requires defaultProfileId.");
            }

            if (string.IsNullOrWhiteSpace(DefaultSlotId))
            {
                throw new InvalidOperationException($"SaveConfigAsset '{name}' requires defaultSlotId.");
            }

            if (schemaVersion <= 0)
            {
                throw new InvalidOperationException($"SaveConfigAsset '{name}' requires schemaVersion > 0.");
            }

            if (backend == null)
            {
                throw new InvalidOperationException($"SaveConfigAsset '{name}' requires backend.");
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
