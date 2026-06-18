using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.Actors.ActivitySetup
{
    public readonly struct ActivityActorInstanceSourceResult
    {
        public ActivityActorInstanceSourceResult(
            IReadOnlyList<ActorInstanceRecord> actorInstances,
            IReadOnlyList<ActorParticipationRecord> actorParticipations)
        {
            ActorInstances = actorInstances ?? Array.Empty<ActorInstanceRecord>();
            ActorParticipations = actorParticipations ?? Array.Empty<ActorParticipationRecord>();
        }

        public IReadOnlyList<ActorInstanceRecord> ActorInstances { get; }
        public IReadOnlyList<ActorParticipationRecord> ActorParticipations { get; }
    }

    public interface IActivityActorInstanceSource
    {
        ActivityActorInstanceSourceResult Collect(
            SessionActivityIdentity identity,
            string source,
            string reason);
    }

    public readonly struct ActorInventoryFeedResult
    {
        public ActorInventoryFeedResult(
            SessionActivityIdentity identity,
            IReadOnlyList<ActorInstanceRecord> actorInstances,
            IReadOnlyList<ActorEntryRecord> actorEntries,
            IReadOnlyList<ActorParticipationRecord> actorParticipations,
            string source,
            string reason)
        {
            Identity = identity;
            ActorInstances = actorInstances ?? Array.Empty<ActorInstanceRecord>();
            ActorEntries = actorEntries ?? Array.Empty<ActorEntryRecord>();
            ActorParticipations = actorParticipations ?? Array.Empty<ActorParticipationRecord>();
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity Identity { get; }
        public IReadOnlyList<ActorInstanceRecord> ActorInstances { get; }
        public IReadOnlyList<ActorEntryRecord> ActorEntries { get; }
        public IReadOnlyList<ActorParticipationRecord> ActorParticipations { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => Identity.IsValid;

        public IReadOnlyList<ActorScanTarget> BuildScanTargets(string source)
        {
            List<ActorScanTarget> targets = new();
            for (int index = 0; index < ActorInstances.Count; index++)
            {
                if (ActorScanTarget.TryFromInstance(ActorInstances[index], source, out var target))
                {
                    targets.Add(target);
                }
            }

            return targets;
        }
}

    public sealed class ActorInventoryFeed
    {
        public ActorInventoryFeedResult BuildFromSources(
            SessionActivityIdentity identity,
            IReadOnlyList<IActivityActorInstanceSource> sources,
            string source,
            string reason)
        {
            if (!identity.IsValid)
            {
                throw new InvalidOperationException("ActorInventoryFeed requires valid SessionActivityIdentity.");
            }

            List<ActorInstanceRecord> actorInstances = new();
            List<ActorParticipationRecord> actorParticipations = new();

            if (sources != null)
            {
                for (int index = 0; index < sources.Count; index++)
                {
                    var actorInstanceSource = sources[index];
                    if (actorInstanceSource == null)
                    {
                        continue;
                    }

                    var sourceResult = actorInstanceSource.Collect(identity, source, reason);
                    AppendRecords(sourceResult.ActorInstances, actorInstances);
                    AppendRecords(sourceResult.ActorParticipations, actorParticipations);
                }
            }

            List<ActorEntryRecord> actorEntries = BuildEntries(identity, actorInstances, source, reason);
            return new ActorInventoryFeedResult(identity, actorInstances, actorEntries, actorParticipations, source, reason);
        }

        private static void AppendRecords<TRecord>(IReadOnlyList<TRecord> sourceRecords, List<TRecord> target)
        {
            if (sourceRecords == null || sourceRecords.Count == 0)
            {
                return;
            }

            for (int index = 0; index < sourceRecords.Count; index++)
            {
                target.Add(sourceRecords[index]);
            }
        }

        private static List<ActorEntryRecord> BuildEntries(
            SessionActivityIdentity identity,
            IReadOnlyList<ActorInstanceRecord> actorInstances,
            string source,
            string reason)
        {
            List<ActorEntryRecord> actorEntries = new();
            if (actorInstances == null || actorInstances.Count == 0)
            {
                return actorEntries;
            }

            for (int index = 0; index < actorInstances.Count; index++)
            {
                var instance = actorInstances[index];
                if (!instance.IsValid)
                {
                    continue;
                }

                actorEntries.Add(new ActorEntryRecord(identity, instance, actorEntries.Count, source, reason));
            }

            return actorEntries;
        }
    }
}
