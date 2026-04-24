using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Authoring
{
    [CreateAssetMenu(
        fileName = "ActorSetCatalog",
        menuName = "ImmersiveGames/NewScripts/ActorsSystem/Definitions/ActorSetCatalog",
        order = 32)]
    public sealed class ActorSetCatalogAsset : ScriptableObject, ISerializationCallbackReceiver
    {
        [Serializable]
        public sealed class MemberEntry
        {
            public string actorSpecId;
            public int order;
            public bool enabled = true;
        }

        [Serializable]
        public sealed class SetEntry
        {
            public string actorSetRefId;
            public List<MemberEntry> members = new();
        }

        [SerializeField] private List<SetEntry> sets = new();

        private readonly Dictionary<string, SetEntry> _setByRefId = new(StringComparer.Ordinal);
        private bool _built;

        public IReadOnlyList<SetEntry> Sets => sets;

        public bool TryGetSet(ActorSetRef actorSetRef, out SetEntry set)
        {
            set = null;
            if (!actorSetRef.IsValid)
            {
                return false;
            }

            EnsureBuilt();
            return _setByRefId.TryGetValue(actorSetRef.Value, out set);
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
            _setByRefId.Clear();

            if (sets == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] Missing actor set list. asset='{name}'.");
            }

            if (sets.Count <= 0)
            {
                throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] Empty actor set list. asset='{name}'.");
            }

            for (int i = 0; i < sets.Count; i += 1)
            {
                SetEntry set = sets[i];
                if (set == null)
                {
                    throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] Null actor set entry. asset='{name}', index={i}.");
                }

                string actorSetRefId = Normalize(set.actorSetRefId);
                if (string.IsNullOrWhiteSpace(actorSetRefId))
                {
                    throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] Actor set with empty actorSetRefId. asset='{name}', index={i}.");
                }

                set.actorSetRefId = actorSetRefId;
                if (_setByRefId.ContainsKey(actorSetRefId))
                {
                    throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] Duplicate actorSetRefId='{actorSetRefId}' in asset='{name}'.");
                }

                if (set.members == null || set.members.Count <= 0)
                {
                    throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] Actor set '{actorSetRefId}' has no members.");
                }

                var memberIds = new HashSet<string>(StringComparer.Ordinal);
                for (int memberIndex = 0; memberIndex < set.members.Count; memberIndex += 1)
                {
                    MemberEntry member = set.members[memberIndex];
                    if (member == null)
                    {
                        throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] Null actor set member in set='{actorSetRefId}', index={memberIndex}.");
                    }

                    member.actorSpecId = Normalize(member.actorSpecId);
                    if (string.IsNullOrWhiteSpace(member.actorSpecId))
                    {
                        throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] Actor set member with empty actorSpecId in set='{actorSetRefId}', index={memberIndex}.");
                    }

                    if (!memberIds.Add(member.actorSpecId))
                    {
                        throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] Duplicate actorSpecId='{member.actorSpecId}' in actor set='{actorSetRefId}'.");
                    }
                }

                _setByRefId.Add(actorSetRefId, set);
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
