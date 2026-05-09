using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using UnityEngine;
using UnityEngine.Serialization;
namespace _ImmersiveGames.NewScripts.ActorsSystem.Authoring
{
    [CreateAssetMenu(
        fileName = "ActorSetCatalog",
        menuName = "ImmersiveGames/NewScripts/ActorsSystem/Definitions/ActorSetCatalog",
        order = 32)]
    public sealed class ActorSetCatalogAsset : ScriptableObject, ISerializationCallbackReceiver
    {
        [Serializable]
        public sealed class CardinalityEntry
        {
            [Tooltip("Cardinality strategy for this ActorSet member.")]
            public ActorCardinalityKind kind = ActorCardinalityKind.ExactlyOne;
            [Tooltip("Used only when kind is Fixed.")]
            public int fixedCount = 1;
            [Tooltip("Used only when kind is Range.")]
            public int minCount = 1;
            [Tooltip("Used only when kind is Range.")]
            public int maxCount = 1;
        }

        [Serializable]
        public sealed class MemberEntry
        {
            [FormerlySerializedAs("memberId")]
            [Header("Canonical Member Identity")]
            [Tooltip("Canonical ActorSetMemberId. Unique inside the ActorSet.")]
            public string actorSetMemberId;
            [Tooltip("Referenced canonical ActorSpecId.")]
            public string actorSpecId;
            [Tooltip("Deterministic order for semantic resolution.")]
            public int order;
            [Tooltip("Whether this member is enabled in this ActorSet.")]
            public bool enabled = true;
            [Tooltip("Cardinality definition for occurrences of this member.")]
            public CardinalityEntry cardinality = new();
        }

        [Serializable]
        public sealed class SetEntry
        {
            [FormerlySerializedAs("actorSetRef")]
            [Tooltip("Canonical ActorSetRef identifier. Global unique key for this set.")]
            public string actorSetRefId;
            [Tooltip("Configured members for this ActorSet.")]
            public List<MemberEntry> members = new();
        }

        [SerializeField, Tooltip("Canonical ActorSet catalog entries.")] private List<SetEntry> sets = new();

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

                    member.actorSetMemberId = Normalize(member.actorSetMemberId);
                    if (string.IsNullOrWhiteSpace(member.actorSetMemberId))
                    {
                        throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] Actor set member with empty actorSetMemberId in set='{actorSetRefId}', index={memberIndex}.");
                    }

                    if (!memberIds.Add(member.actorSetMemberId))
                    {
                        throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] Duplicate actorSetMemberId='{member.actorSetMemberId}' in actor set='{actorSetRefId}'.");
                    }

                    member.actorSpecId = Normalize(member.actorSpecId);
                    if (string.IsNullOrWhiteSpace(member.actorSpecId))
                    {
                        throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] Actor set member with empty actorSpecId in set='{actorSetRefId}', index={memberIndex}.");
                    }

                    if (member.cardinality == null)
                    {
                        throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] Actor set member without cardinality in set='{actorSetRefId}' memberId='{member.actorSetMemberId}'.");
                    }

                    var cardinality = new ActorCardinalitySpec(
                        member.cardinality.kind,
                        member.cardinality.fixedCount,
                        member.cardinality.minCount,
                        member.cardinality.maxCount);
                    if (!cardinality.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"[FATAL][Config][ActorsSystem] Invalid cardinality in actor set='{actorSetRefId}' memberId='{member.actorSetMemberId}' kind='{member.cardinality.kind}' fixed='{member.cardinality.fixedCount}' min='{member.cardinality.minCount}' max='{member.cardinality.maxCount}'.");
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
