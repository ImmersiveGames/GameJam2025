using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Authoring
{
    [CreateAssetMenu(
        fileName = "ActorSpecsCatalog",
        menuName = "ImmersiveGames/NewScripts/ActorsSystem/Definitions/ActorSpecsCatalog",
        order = 31)]
    public sealed class ActorSpecsCatalogAsset : ScriptableObject, ISerializationCallbackReceiver
    {
        [Serializable]
        public sealed class Entry
        {
            public string actorSpecId;
            public ActorSpecSourceKind sourceKind = ActorSpecSourceKind.Unknown;
            public ActorSpecRoleGroup roleGroup = ActorSpecRoleGroup.Unknown;
            public ActorOperationalRecipeKind operationalRecipeKind = ActorOperationalRecipeKind.Unknown;
            public string placeholderBodyRef;
            public GameObject placeholderBodyPrefab;
            public ActorSpecIntegrationStage integrationStage = ActorSpecIntegrationStage.Unknown;
            public ActorSpecRealizationMode realizationMode = ActorSpecRealizationMode.Unknown;
            public ActorSpecContinuityResetPolicy continuityResetPolicy = ActorSpecContinuityResetPolicy.Unknown;
        }

        [SerializeField] private List<Entry> entries = new();

        private readonly Dictionary<string, Entry> _entriesBySpecId = new(StringComparer.Ordinal);
        private readonly Dictionary<ActorOperationalRecipeKind, Entry> _entriesByRecipeKind = new();
        private bool _built;

        public IReadOnlyList<Entry> Entries => entries;

        public bool TryGetBySpecId(string actorSpecId, out Entry entry)
        {
            entry = null;
            if (string.IsNullOrWhiteSpace(actorSpecId))
            {
                return false;
            }

            EnsureBuilt();
            return _entriesBySpecId.TryGetValue(actorSpecId.Trim(), out entry);
        }

        public bool TryGetByRecipeKind(ActorOperationalRecipeKind recipeKind, out Entry entry)
        {
            entry = null;
            if (recipeKind == ActorOperationalRecipeKind.Unknown)
            {
                return false;
            }

            EnsureBuilt();
            return _entriesByRecipeKind.TryGetValue(recipeKind, out entry);
        }

        public void ValidateOrFail()
        {
            EnsureBuilt();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _built = false;
            ValidateOrFail();
        }
#endif

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            _built = false;
        }

        private void EnsureBuilt()
        {
            if (_built)
            {
                return;
            }

            BuildCacheOrFail();
            _built = true;
        }

        private void BuildCacheOrFail()
        {
            _entriesBySpecId.Clear();
            _entriesByRecipeKind.Clear();

            if (entries == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] Missing actor specs entries list. asset='{name}'.");
            }

            if (entries.Count == 0)
            {
                throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] Empty actor specs entries list. asset='{name}'.");
            }

            for (int i = 0; i < entries.Count; i += 1)
            {
                Entry entry = entries[i];
                if (entry == null)
                {
                    throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] Null actor spec entry. asset='{name}', index={i}.");
                }

                string actorSpecId = Normalize(entry.actorSpecId);
                if (string.IsNullOrWhiteSpace(actorSpecId))
                {
                    throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] ActorSpec entry with empty actorSpecId. asset='{name}', index={i}.");
                }

                if (entry.sourceKind == ActorSpecSourceKind.Unknown)
                {
                    throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] ActorSpec '{actorSpecId}' with unknown sourceKind.");
                }

                if (entry.roleGroup == ActorSpecRoleGroup.Unknown)
                {
                    throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] ActorSpec '{actorSpecId}' with unknown roleGroup.");
                }

                if (entry.operationalRecipeKind == ActorOperationalRecipeKind.Unknown)
                {
                    throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] ActorSpec '{actorSpecId}' with unknown operationalRecipeKind.");
                }

                if (entry.integrationStage == ActorSpecIntegrationStage.Unknown)
                {
                    throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] ActorSpec '{actorSpecId}' with unknown integrationStage.");
                }

                if (entry.realizationMode == ActorSpecRealizationMode.Unknown)
                {
                    throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] ActorSpec '{actorSpecId}' with unknown realizationMode.");
                }

                if (entry.continuityResetPolicy == ActorSpecContinuityResetPolicy.Unknown)
                {
                    throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] ActorSpec '{actorSpecId}' with unknown continuityResetPolicy.");
                }

                entry.actorSpecId = actorSpecId;
                entry.placeholderBodyRef = Normalize(entry.placeholderBodyRef);
                if (string.IsNullOrWhiteSpace(entry.placeholderBodyRef) && entry.placeholderBodyPrefab != null)
                {
                    entry.placeholderBodyRef = entry.placeholderBodyPrefab.name;
                }

                if (_entriesBySpecId.ContainsKey(actorSpecId))
                {
                    throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] Duplicate actorSpecId='{actorSpecId}' in asset='{name}'.");
                }

                if (_entriesByRecipeKind.ContainsKey(entry.operationalRecipeKind))
                {
                    throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] Duplicate operationalRecipeKind='{entry.operationalRecipeKind}' in asset='{name}'.");
                }

                _entriesBySpecId.Add(actorSpecId, entry);
                _entriesByRecipeKind.Add(entry.operationalRecipeKind, entry);
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
