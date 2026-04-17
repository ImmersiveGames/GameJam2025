using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ActorSystem.Contracts.Inbound;
using _ImmersiveGames.NewScripts.ActorSystem.Contracts.Outbound;
using _ImmersiveGames.NewScripts.ActorSystem.ReadModel;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.ActorSystem.Semantic
{
    /// <summary>
    /// Thin non-executor semantic projection for actor relevance.
    /// </summary>
    public sealed class ActorSystemReadModelService : IActorSystemReadModelService
    {
        private readonly IActorSystemSemanticContextProvider _contextProvider;
        private readonly IActorPresenceReadPort _presenceReadPort;
        private readonly List<ActorRuntimePresenceSnapshot> _buffer = new(32);
        private ActorSystemReadModelSnapshot _current = ActorSystemReadModelSnapshot.Empty;

        public ActorSystemReadModelService(
            IActorSystemSemanticContextProvider contextProvider,
            IActorPresenceReadPort presenceReadPort)
        {
            _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
            _presenceReadPort = presenceReadPort ?? throw new ArgumentNullException(nameof(presenceReadPort));

            DebugUtility.Log(typeof(ActorSystemReadModelService),
                "[OBS][ActorSystem] ReadModelService registrado (thin semantic, non-executor).",
                DebugUtility.Colors.Info);
        }

        public ActorSystemReadModelSnapshot Current => _current;

        public bool TryGetCurrent(out ActorSystemReadModelSnapshot snapshot)
        {
            snapshot = _current;
            return _current.IsValid;
        }

        public ActorSystemReadModelSnapshot Refresh()
        {
            ActorSystemReadModelSnapshot previous = _current;
            _current = BuildSnapshot();

            if (!_current.Equals(previous))
            {
                DebugUtility.LogVerbose(typeof(ActorSystemReadModelService),
                    $"[OBS][ActorSystem] ReadModel atualizado relevantActorId='{(string.IsNullOrWhiteSpace(_current.RelevantActorId) ? "<none>" : _current.RelevantActorId)}' runtimeActorCount='{_current.RuntimeActorCount}' reason='{_current.Reason}'.",
                    DebugUtility.Colors.Info);
            }

            return _current;
        }

        public void Clear(string reason = null)
        {
            _current = new ActorSystemReadModelSnapshot(
                ActorSystemSemanticContext.Empty,
                string.Empty,
                0,
                string.IsNullOrWhiteSpace(reason) ? "cleared" : reason.Trim());
        }

        private ActorSystemReadModelSnapshot BuildSnapshot()
        {
            if (!_contextProvider.TryGetCurrent(out ActorSystemSemanticContext context) || !context.IsValid)
            {
                return new ActorSystemReadModelSnapshot(
                    ActorSystemSemanticContext.Empty,
                    string.Empty,
                    0,
                    "no-semantic-context");
            }

            _buffer.Clear();
            _presenceReadPort.TryGetAll(_buffer);

            string relevantActorId = ResolveRelevantActorId(context, _buffer);
            return new ActorSystemReadModelSnapshot(
                context,
                relevantActorId,
                _buffer.Count,
                string.IsNullOrWhiteSpace(relevantActorId) ? "no-runtime-match" : "resolved");
        }

        private static string ResolveRelevantActorId(
            ActorSystemSemanticContext context,
            List<ActorRuntimePresenceSnapshot> runtimeActors)
        {
            if (runtimeActors == null || runtimeActors.Count == 0)
            {
                return string.Empty;
            }

            if (context.HasPrimaryParticipant)
            {
                for (int index = 0; index < runtimeActors.Count; index += 1)
                {
                    ActorRuntimePresenceSnapshot candidate = runtimeActors[index];
                    if (candidate.IsValid && string.Equals(candidate.ActorId, context.PrimaryParticipantId, StringComparison.Ordinal))
                    {
                        return candidate.ActorId;
                    }
                }
            }

            if (context.HasLocalParticipant)
            {
                for (int index = 0; index < runtimeActors.Count; index += 1)
                {
                    ActorRuntimePresenceSnapshot candidate = runtimeActors[index];
                    if (candidate.IsValid && string.Equals(candidate.ActorId, context.LocalParticipantId, StringComparison.Ordinal))
                    {
                        return candidate.ActorId;
                    }
                }
            }

            for (int index = 0; index < runtimeActors.Count; index += 1)
            {
                ActorRuntimePresenceSnapshot candidate = runtimeActors[index];
                if (candidate.IsValid && candidate.IsActive)
                {
                    return candidate.ActorId;
                }
            }

            for (int index = 0; index < runtimeActors.Count; index += 1)
            {
                if (runtimeActors[index].IsValid)
                {
                    return runtimeActors[index].ActorId;
                }
            }

            return string.Empty;
        }
    }
}
