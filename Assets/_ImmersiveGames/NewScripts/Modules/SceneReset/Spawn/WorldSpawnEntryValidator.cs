using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Eater;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Spawn.Definitions;

namespace _ImmersiveGames.NewScripts.Modules.SceneReset.Spawn
{
    /// <summary>
    /// Valida a configuração mínima da SpawnEntry por kind conhecido.
    /// Mantém a semântica atual: falha retorna false e o call site decide o tratamento.
    /// </summary>
    public sealed class WorldSpawnEntryValidator
    {
        public bool TryValidate(WorldDefinition.SpawnEntry entry)
        {
            if (entry == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnEntryValidator),
                    "SpawnEntry nula ao validar configuração de spawn.");
                return false;
            }

            switch (entry.Kind)
            {
                case WorldSpawnServiceKind.DummyActor:
                    return ValidatePrefab(entry, "DummyActorSpawnService");

                case WorldSpawnServiceKind.Player:
                    return ValidatePrefab(entry, "PlayerSpawnService");

                case WorldSpawnServiceKind.Eater:
                    return ValidateEater(entry);

                default:
                    DebugUtility.LogError(typeof(WorldSpawnEntryValidator),
                        $"WorldSpawnServiceKind não suportado: {entry.Kind}.");
                    return false;
            }
        }

        private static bool ValidatePrefab(WorldDefinition.SpawnEntry entry, string owner)
        {
            if (entry.Prefab != null)
            {
                return true;
            }

            DebugUtility.LogError(typeof(WorldSpawnEntryValidator),
                $"Prefab não configurado para {owner}.");
            return false;
        }

        private static bool ValidateEater(WorldDefinition.SpawnEntry entry)
        {
            if (entry.Prefab == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnEntryValidator),
                    "Prefab não configurado para EaterSpawnService.");
                return false;
            }

            var eaterActor = entry.Prefab.GetComponent<EaterActor>();
            if (eaterActor != null)
            {
                return true;
            }

            DebugUtility.LogError(typeof(WorldSpawnEntryValidator),
                "Prefab sem EaterActor. EaterSpawnService não será criado.");
            return false;
        }
    }
}
