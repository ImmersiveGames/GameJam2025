using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ActorsSystem.Authoring;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Semantic
{
    public readonly struct ActorSetResolvedSelection
    {
        public ActorSetResolvedSelection(ActorSetRef actorSetRef, ActorSpecRecord[] orderedSpecs)
        {
            ActorSetRef = actorSetRef;
            OrderedSpecs = orderedSpecs ?? Array.Empty<ActorSpecRecord>();
        }

        public ActorSetRef ActorSetRef { get; }
        public ActorSpecRecord[] OrderedSpecs { get; }
        public int Count => OrderedSpecs?.Length ?? 0;
        public bool HasEntries => Count > 0;
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

            List<(int order, ActorSpecRecord spec)> ordered = BuildOrderedSpecsOrFail(actorSetRef, set);
            ordered.Sort(static (left, right) => left.order.CompareTo(right.order));

            var specs = new ActorSpecRecord[ordered.Count];
            for (int i = 0; i < ordered.Count; i += 1)
            {
                specs[i] = ordered[i].spec;
            }

            selection = new ActorSetResolvedSelection(actorSetRef, specs);
            return selection.HasEntries;
        }

        private List<(int order, ActorSpecRecord spec)> BuildOrderedSpecsOrFail(ActorSetRef actorSetRef, ActorSetCatalogAsset.SetEntry set)
        {
            var ordered = new List<(int order, ActorSpecRecord spec)>(set.members.Count);
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

                ordered.Add((member.order, spec));
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
