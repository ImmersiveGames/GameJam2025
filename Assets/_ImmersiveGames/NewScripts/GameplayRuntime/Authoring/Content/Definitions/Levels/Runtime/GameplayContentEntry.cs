using System;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Content.Definitions.Levels.Runtime
{
    public enum GameplayContentEntryRole
    {
        Unknown = 0,
        Main = 1,
        Aux = 2,
        Prototype = 3
    }

    [Serializable]
    public sealed class GameplayContentEntry
    {
        [SerializeField] private string entryId = string.Empty;
        [SerializeField] private GameplayContentEntryRole role = GameplayContentEntryRole.Unknown;
        [SerializeField] private Object configurationReference;
        [SerializeField] private string materializationExpectation = string.Empty;
        [SerializeField] private string observabilityExpectation = string.Empty;

        public string EntryId => entryId.TrimToEmpty();
        public GameplayContentEntryRole Role => role;
        public Object ConfigurationReference => configurationReference;
        public string MaterializationExpectation => materializationExpectation.TrimToEmpty();
        public string ObservabilityExpectation => observabilityExpectation.TrimToEmpty();

        public bool IsValid => TryValidateRuntime(out _);

        public bool TryValidateRuntime(out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(EntryId))
            {
                error = "Entry id is required.";
                return false;
            }

            if (!Enum.IsDefined(typeof(GameplayContentEntryRole), role) || role == GameplayContentEntryRole.Unknown)
            {
                error = $"Invalid gameplay content entry role '{role}'.";
                return false;
            }

            return true;
        }
}
}

