using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline
{
    public sealed class SessionActivityCatalog
    {
        private readonly IReadOnlyList<SessionActivityDefinition> _definitions;

        public SessionActivityCatalog(IEnumerable<SessionActivityDefinition> definitions)
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
        }

        public IReadOnlyList<SessionActivityDefinition> Definitions => _definitions;

        public string Summary => string.Join(" | ", _definitions.Select(definition => definition.ToString()));

        public bool TryGetFirst(out SessionActivityDefinition definition)
        {
            return TryGetByOrdinal(1, out definition);
        }

        public bool TryGetNext(SessionActivityDefinition current, out SessionActivityDefinition next)
        {
            if (!current.IsValid)
            {
                next = default;
                return false;
            }

            if (!current.HasNextActivity)
            {
                next = default;
                return false;
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

            next = default;
            return false;
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

