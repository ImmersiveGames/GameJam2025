using System;
using System.Collections.Generic;
using System.Linq;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionActivityPipeline
{
    public sealed class SessionActivityMiniCatalog
    {
        private const string CatalogSource = "SessionActivityMiniCatalog";
        private readonly IReadOnlyList<SessionActivityDefinition> _definitions;

        public SessionActivityMiniCatalog()
        {
            _definitions = new[]
            {
                new SessionActivityDefinition(
                    activityId: "activity_01",
                    displayName: "Activity 01",
                    activityOrdinal: 1,
                    hasActivation: true,
                    hasGameplayContent: true,
                    hasPhaseResultPresentation: true,
                    nextActivityId: "activity_02",
                    source: CatalogSource),
                new SessionActivityDefinition(
                    activityId: "activity_02",
                    displayName: "Activity 02",
                    activityOrdinal: 2,
                    hasActivation: true,
                    hasGameplayContent: true,
                    hasPhaseResultPresentation: false,
                    nextActivityId: string.Empty,
                    source: CatalogSource),
            };
        }

        public SessionActivityMiniCatalog(IEnumerable<SessionActivityDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            SessionActivityDefinition[] materialized = definitions.ToArray();
            if (materialized.Length == 0)
            {
                throw new InvalidOperationException("SessionActivityMiniCatalog requires at least one definition.");
            }

            for (int index = 0; index < materialized.Length; index++)
            {
                if (!materialized[index].IsValid)
                {
                    throw new InvalidOperationException($"SessionActivityMiniCatalog definition at index {index} is invalid.");
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
