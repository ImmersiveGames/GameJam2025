using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using UnityEngine;
using UnityEngine.Serialization;

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
            [Header("Canonical Identity")]
            [Tooltip("Canonical ActorSpec identifier. Global unique key in ActorsSystem (for example: actor.player).")]
            public string actorSpecId;
            [FormerlySerializedAs("archetypeId")]
            [Tooltip("Operational spawn archetype key resolved by spawn registry (for example: spawn.player). Required.")]
            public string spawnArchetypeId;

            [Header("Canonical Semantics")]
            [Tooltip("Semantic source strategy that defines how canonical occurrences are resolved.")]
            public ActorSpecSourceKind sourceKind = ActorSpecSourceKind.Unknown;
            [Tooltip("Expected semantic role group for this spec.")]
            public ActorSpecRoleGroup roleGroup = ActorSpecRoleGroup.Unknown;
            [Tooltip("Operational recipe detail. Never used as canonical identity.")]
            public ActorOperationalRecipeKind operationalRecipeKind = ActorOperationalRecipeKind.Unknown;

            [Header("Operational Payload")]
            [Tooltip("Logical reference/path for placeholder body payload.")]
            public string placeholderBodyRef;
            [Tooltip("Optional prefab payload used by spawn archetype implementation.")]
            public GameObject placeholderBodyPrefab;

            [Header("Lifecycle Policy")]
            [Tooltip("Integration stage for this spec in runtime lifecycle.")]
            public ActorSpecIntegrationStage integrationStage = ActorSpecIntegrationStage.Unknown;
            [Tooltip("How this spec should be realized operationally.")]
            public ActorSpecRealizationMode realizationMode = ActorSpecRealizationMode.Unknown;
            [Tooltip("Reset/continuity policy applied for this spec.")]
            public ActorSpecContinuityResetPolicy continuityResetPolicy = ActorSpecContinuityResetPolicy.Unknown;
        }

        [SerializeField, Tooltip("Canonical ActorSpecs catalog entries.")] private List<Entry> entries = new();

        private readonly Dictionary<string, Entry> _entriesBySpecId = new(StringComparer.Ordinal);
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
                if (string.IsNullOrWhiteSpace(entry.spawnArchetypeId))
                {
                    throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] ActorSpec '{actorSpecId}' with empty spawnArchetypeId.");
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
                entry.spawnArchetypeId = Normalize(entry.spawnArchetypeId);
                entry.placeholderBodyRef = Normalize(entry.placeholderBodyRef);
                if (string.IsNullOrWhiteSpace(entry.placeholderBodyRef) && entry.placeholderBodyPrefab != null)
                {
                    entry.placeholderBodyRef = entry.placeholderBodyPrefab.name;
                }

                if (_entriesBySpecId.ContainsKey(actorSpecId))
                {
                    throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] Duplicate actorSpecId='{actorSpecId}' in asset='{name}'.");
                }

                _entriesBySpecId.Add(actorSpecId, entry);
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
