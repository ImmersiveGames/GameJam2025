using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ActorsSystem.Authoring;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Semantic
{
    public interface IActorSpecCatalogService
    {
        bool TryGetByActorSpecId(string actorSpecId, out ActorSpecRecord spec);
    }

    public sealed class ActorSpecsCatalogService : IActorSpecCatalogService
    {
        private readonly Dictionary<string, ActorSpecRecord> _bySpecId = new(StringComparer.Ordinal);

        public ActorSpecsCatalogService(ActorSpecsCatalogAsset catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            catalog.ValidateOrFail();
            BuildCacheOrFail(catalog);

            DebugUtility.Log(typeof(ActorSpecsCatalogService),
                $"[OBS][ActorsSystem] ActorSpecs catalog service registrado via asset catalog='{catalog.name}' count='{_bySpecId.Count}'.",
                DebugUtility.Colors.Info);
        }

        public bool TryGetByActorSpecId(string actorSpecId, out ActorSpecRecord spec)
        {
            spec = default;
            if (string.IsNullOrWhiteSpace(actorSpecId))
            {
                return false;
            }

            return _bySpecId.TryGetValue(actorSpecId.Trim(), out spec);
        }

        private void BuildCacheOrFail(ActorSpecsCatalogAsset catalog)
        {
            _bySpecId.Clear();

            IReadOnlyList<ActorSpecsCatalogAsset.Entry> entries = catalog.Entries;
            for (int i = 0; i < entries.Count; i += 1)
            {
                ActorSpecsCatalogAsset.Entry entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                var spec = new ActorSpecRecord(
                    entry.actorSpecId,
                    entry.spawnArchetypeId,
                    entry.sourceKind,
                    entry.roleGroup,
                    entry.operationalRecipeKind,
                    entry.placeholderBodyRef,
                    entry.placeholderBodyPrefab,
                    entry.integrationStage,
                    entry.realizationMode,
                    entry.continuityResetPolicy);

                if (!spec.IsValid)
                {
                    throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] Invalid ActorSpec generated from catalog='{catalog.name}', index={i}.");
                }

                if (_bySpecId.ContainsKey(spec.ActorSpecId))
                {
                    throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] Duplicate actorSpecId='{spec.ActorSpecId}' in catalog='{catalog.name}'.");
                }

                _bySpecId.Add(spec.ActorSpecId, spec);
            }
        }
    }
}
