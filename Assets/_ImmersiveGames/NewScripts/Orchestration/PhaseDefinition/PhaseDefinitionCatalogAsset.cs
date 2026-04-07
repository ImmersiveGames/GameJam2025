using System;
using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition
{
    [CreateAssetMenu(
        fileName = "PhaseDefinitionCatalogAsset",
        menuName = "ImmersiveGames/NewScripts/Orchestration/PhaseDefinition/Catalogs/PhaseDefinitionCatalogAsset",
        order = 30)]
    public sealed class PhaseDefinitionCatalogAsset : ScriptableObject, IPhaseDefinitionCatalog, ISerializationCallbackReceiver
    {
        [SerializeField] private List<PhaseDefinitionAsset> phaseDefinitions = new();

        private readonly Dictionary<string, PhaseDefinitionAsset> _cache = new(StringComparer.OrdinalIgnoreCase);
        private bool _built;

        public IReadOnlyCollection<string> PhaseIds
        {
            get
            {
                EnsureBuilt();
                return _cache.Keys;
            }
        }

        public bool TryGet(string phaseId, out PhaseDefinitionAsset phaseDefinition)
        {
            phaseDefinition = null;

            if (string.IsNullOrWhiteSpace(phaseId))
            {
                return false;
            }

            EnsureBuilt();
            return _cache.TryGetValue(PhaseDefinitionId.Normalize(phaseId), out phaseDefinition);
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

            BuildCache();
            _built = true;
        }

        private void BuildCache()
        {
            _cache.Clear();

            if (phaseDefinitions == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Missing phaseDefinitions list. asset='{name}'.");
            }

            for (int i = 0; i < phaseDefinitions.Count; i++)
            {
                PhaseDefinitionAsset phaseDefinition = phaseDefinitions[i];
                if (phaseDefinition == null)
                {
                    throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Null phaseDefinition entry. asset='{name}', index={i}.");
                }

                phaseDefinition.ValidateOrFail(name);

                string phaseId = phaseDefinition.PhaseId.Value;
                if (string.IsNullOrWhiteSpace(phaseId))
                {
                    throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Invalid phaseId resolved from asset. catalog='{name}', phaseAsset='{phaseDefinition.name}'.");
                }

                if (_cache.ContainsKey(phaseId))
                {
                    throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Duplicate phaseId '{phaseId}' in catalog '{name}'.");
                }

                _cache.Add(phaseId, phaseDefinition);
            }
        }
    }
}
