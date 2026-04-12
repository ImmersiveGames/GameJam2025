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
        private readonly Dictionary<string, int> _phaseIndexById = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _orderedPhaseIds = new();
        private bool _built;

        public IReadOnlyList<string> PhaseIds
        {
            get
            {
                EnsureBuilt();
                return _orderedPhaseIds;
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

        public PhaseDefinitionAsset ResolveInitialOrFail()
        {
            EnsureBuilt();

            if (phaseDefinitions.Count == 0)
            {
                throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Catalog has no phaseDefinitions entries. asset='{name}'.");
            }

            return phaseDefinitions[0];
        }

        public PhaseDefinitionAsset ResolveNextOrFail(string phaseId)
        {
            if (!TryGetNext(phaseId, out PhaseDefinitionAsset nextPhaseDefinition))
            {
                string normalizedPhaseId = PhaseDefinitionId.Normalize(phaseId);
                throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Missing next phase for phaseId='{normalizedPhaseId}' in catalog '{name}'.");
            }

            return nextPhaseDefinition;
        }

        public bool TryGetNext(string phaseId, out PhaseDefinitionAsset nextPhaseDefinition)
        {
            nextPhaseDefinition = null;

            if (string.IsNullOrWhiteSpace(phaseId))
            {
                return false;
            }

            EnsureBuilt();

            string normalizedPhaseId = PhaseDefinitionId.Normalize(phaseId);
            if (!_phaseIndexById.TryGetValue(normalizedPhaseId, out int index))
            {
                return false;
            }

            int nextIndex = index + 1;
            if (nextIndex >= phaseDefinitions.Count)
            {
                return false;
            }

            nextPhaseDefinition = phaseDefinitions[nextIndex];
            return nextPhaseDefinition != null;
        }

        public PhaseDefinitionAsset ResolvePreviousOrFail(string phaseId)
        {
            if (!TryGetPrevious(phaseId, out PhaseDefinitionAsset previousPhaseDefinition))
            {
                string normalizedPhaseId = PhaseDefinitionId.Normalize(phaseId);
                throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Missing previous phase for phaseId='{normalizedPhaseId}' in catalog '{name}'.");
            }

            return previousPhaseDefinition;
        }

        public bool TryGetPrevious(string phaseId, out PhaseDefinitionAsset previousPhaseDefinition)
        {
            previousPhaseDefinition = null;

            if (string.IsNullOrWhiteSpace(phaseId))
            {
                return false;
            }

            EnsureBuilt();

            string normalizedPhaseId = PhaseDefinitionId.Normalize(phaseId);
            if (!_phaseIndexById.TryGetValue(normalizedPhaseId, out int index))
            {
                return false;
            }

            int previousIndex = index - 1;
            if (previousIndex < 0)
            {
                return false;
            }

            previousPhaseDefinition = phaseDefinitions[previousIndex];
            return previousPhaseDefinition != null;
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
            _phaseIndexById.Clear();
            _orderedPhaseIds.Clear();

            if (phaseDefinitions == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Missing phaseDefinitions list. asset='{name}'.");
            }

            if (phaseDefinitions.Count == 0)
            {
                throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Empty phaseDefinitions list. asset='{name}'.");
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
                _phaseIndexById.Add(phaseId, i);
                _orderedPhaseIds.Add(phaseId);
            }
        }
    }
}
