using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ActorsSystem.Authoring;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
namespace _ImmersiveGames.NewScripts.ActorsSystem.Semantic
{
    public readonly struct ActorSetResolvedSelection
    {
        public ActorSetResolvedSelection(ActorSetRef actorSetRef, ActorSetResolvedMember[] members)
        {
            ActorSetRef = actorSetRef;
            Members = members ?? Array.Empty<ActorSetResolvedMember>();
        }

        public ActorSetRef ActorSetRef { get; }
        public ActorSetResolvedMember[] Members { get; }
        public int Count => Members?.Length ?? 0;
        public bool HasEntries => Count > 0;
    }

    public readonly struct ActorSetResolvedMember
    {
        public ActorSetResolvedMember(
            ActorSetMemberId actorSetMemberId,
            string actorSpecId,
            int order,
            bool enabled,
            ActorCardinalitySpec cardinality,
            ActorSpecRecord spec)
        {
            ActorSetMemberId = actorSetMemberId;
            ActorSpecId = string.IsNullOrWhiteSpace(actorSpecId) ? string.Empty : actorSpecId.Trim();
            Order = order < 0 ? 0 : order;
            Enabled = enabled;
            Cardinality = cardinality;
            Spec = spec;
        }

        public ActorSetMemberId ActorSetMemberId { get; }
        public string ActorSpecId { get; }
        public int Order { get; }
        public bool Enabled { get; }
        public ActorCardinalitySpec Cardinality { get; }
        public ActorSpecRecord Spec { get; }
        public bool IsValid => ActorSetMemberId.IsValid && !string.IsNullOrWhiteSpace(ActorSpecId) && Cardinality.IsValid && Spec.IsValid;
    }

    public interface IActorSetSelectionService
    {
        bool TryResolve(ActorSetRef actorSetRef, out ActorSetResolvedSelection selection);
    }

    public sealed class ActorSetSelectionService : IActorSetSelectionService
    {
        private readonly ActorSetCatalogAsset _catalog;
        private readonly IActorSpecCatalogService _actorSpecCatalogService;

        public ActorSetSelectionService(ActorSetCatalogAsset catalog, IActorSpecCatalogService actorSpecCatalogService)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _actorSpecCatalogService = actorSpecCatalogService ?? throw new ArgumentNullException(nameof(actorSpecCatalogService));

            _catalog.ValidateOrFail();
        }

        public bool TryResolve(ActorSetRef actorSetRef, out ActorSetResolvedSelection selection)
        {
            selection = default;
            if (!actorSetRef.IsValid)
            {
                return false;
            }

            if (!_catalog.TryGetSet(actorSetRef, out ActorSetCatalogAsset.SetEntry set) || set == null)
            {
                return false;
            }

            List<ActorSetResolvedMember> ordered = BuildOrderedMembersOrFail(actorSetRef, set);
            ordered.Sort(static (left, right) => left.Order.CompareTo(right.Order));

            var members = new ActorSetResolvedMember[ordered.Count];
            for (int i = 0; i < ordered.Count; i += 1)
            {
                members[i] = ordered[i];
            }

            selection = new ActorSetResolvedSelection(actorSetRef, members);
            return selection.HasEntries;
        }

        private List<ActorSetResolvedMember> BuildOrderedMembersOrFail(ActorSetRef actorSetRef, ActorSetCatalogAsset.SetEntry set)
        {
            var ordered = new List<ActorSetResolvedMember>(set.members.Count);
            for (int i = 0; i < set.members.Count; i += 1)
            {
                ActorSetCatalogAsset.MemberEntry member = set.members[i];
                if (member == null || !member.enabled)
                {
                    continue;
                }

                if (!_actorSpecCatalogService.TryGetByActorSpecId(member.actorSpecId, out ActorSpecRecord spec) || !spec.IsValid)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][Config][ActorsSystem] Missing ActorSpec for actorSetRef='{actorSetRef.Value}' actorSpecId='{member.actorSpecId}'.");
                }

                if (member.cardinality == null)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][Config][ActorsSystem] Missing cardinality for actorSetRef='{actorSetRef.Value}' memberId='{member.actorSetMemberId}'.");
                }

                var cardinality = new ActorCardinalitySpec(
                    member.cardinality.kind,
                    member.cardinality.fixedCount,
                    member.cardinality.minCount,
                    member.cardinality.maxCount);
                if (!cardinality.IsValid)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][Config][ActorsSystem] Invalid cardinality for actorSetRef='{actorSetRef.Value}' memberId='{member.actorSetMemberId}'.");
                }

                var resolvedMember = new ActorSetResolvedMember(
                    new ActorSetMemberId(member.actorSetMemberId),
                    member.actorSpecId,
                    member.order,
                    member.enabled,
                    cardinality,
                    spec);
                if (!resolvedMember.IsValid)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][Config][ActorsSystem] Invalid actor set member for actorSetRef='{actorSetRef.Value}' memberId='{member.actorSetMemberId}'.");
                }

                ordered.Add(resolvedMember);
            }

            if (ordered.Count <= 0)
            {
                DebugUtility.LogWarning(typeof(ActorSetSelectionService),
                    $"[OBS][ActorsSystem] ActorSet sem membros habilitados actorSetRef='{actorSetRef.Value}'.");
            }

            return ordered;
        }
    }
}
