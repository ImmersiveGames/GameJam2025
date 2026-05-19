using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline
{
    public sealed class SessionActivityCatalog
    {
        private readonly IReadOnlyList<SessionActivityDefinition> _definitions;
        private readonly ActivityCatalogAdvanceAtEndMode _advanceAtEndMode;

        public SessionActivityCatalog(IEnumerable<SessionActivityDefinition> definitions, ActivityCatalogAdvanceAtEndMode advanceAtEndMode = ActivityCatalogAdvanceAtEndMode.StopAtEnd)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            SessionActivityDefinition[] materialized = definitions.ToArray();
            if (materialized.Length == 0)
            {
                throw new InvalidOperationException("SessionActivityCatalog requires at least one definition.");
            }

            for (int index = 0; index < materialized.Length; index++)
            {
                if (!materialized[index].IsValid)
                {
                    throw new InvalidOperationException($"SessionActivityCatalog definition at index {index} is invalid.");
                }
            }

            HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < materialized.Length; index++)
            {
                if (!ids.Add(materialized[index].ActivityId))
                {
                    throw new InvalidOperationException($"SessionActivityCatalog has duplicate activityId '{materialized[index].ActivityId}'.");
                }
            }

            _definitions = materialized;
            if (advanceAtEndMode != ActivityCatalogAdvanceAtEndMode.StopAtEnd &&
                advanceAtEndMode != ActivityCatalogAdvanceAtEndMode.LoopToFirst)
            {
                throw new InvalidOperationException($"SessionActivityCatalog advanceAtEndMode '{advanceAtEndMode}' is unsupported.");
            }

            _advanceAtEndMode = advanceAtEndMode;
        }

        public IReadOnlyList<SessionActivityDefinition> Definitions => _definitions;
        public ActivityCatalogAdvanceAtEndMode AdvanceAtEndMode => _advanceAtEndMode;

        public string Summary => string.Join(" | ", _definitions.Select(definition => definition.ToString()));

        public bool TryGetFirst(out SessionActivityDefinition definition)
        {
            return TryGetByOrdinal(1, out definition);
        }

        public bool TryGetNext(SessionActivityDefinition current, out SessionActivityDefinition next)
        {
            return TryGetNext(current, out next, out _);
        }

        public bool TryGetNext(SessionActivityDefinition current, out SessionActivityDefinition next, out bool wrapped)
        {
            wrapped = false;
            if (!current.IsValid)
            {
                next = default;
                return false;
            }

            if (!current.HasNextActivity)
            {
                if (_advanceAtEndMode != ActivityCatalogAdvanceAtEndMode.LoopToFirst)
                {
                    next = default;
                    return false;
                }

                bool hasFirst = TryGetFirst(out next) && next.IsValid;
                wrapped = hasFirst;
                return hasFirst;
            }

            for (int index = 0; index < _definitions.Count; index++)
            {
                SessionActivityDefinition candidate = _definitions[index];
                if (string.Equals(candidate.ActivityId, current.NextActivityId, StringComparison.OrdinalIgnoreCase))
                {
                    next = candidate;
                    return true;
                }
            }

            if (_advanceAtEndMode == ActivityCatalogAdvanceAtEndMode.LoopToFirst)
            {
                bool hasFirst = TryGetFirst(out next) && next.IsValid;
                wrapped = hasFirst;
                return hasFirst;
            }

            next = default;
            return false;
        }

        public bool TryGetNextOrdinal(SessionActivityDefinition current, out SessionActivityDefinition next)
        {
            if (!current.IsValid)
            {
                next = default;
                return false;
            }

            return TryGetByOrdinal(current.ActivityOrdinal + 1, out next) && next.IsValid;
        }

        public bool TryGetByOrdinal(int ordinal, out SessionActivityDefinition definition)
        {
            for (int index = 0; index < _definitions.Count; index++)
            {
                SessionActivityDefinition candidate = _definitions[index];
                if (candidate.ActivityOrdinal == ordinal)
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = default;
            return false;
        }
    }
}

