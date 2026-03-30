using System;
using _ImmersiveGames.NewScripts.Core.Identifiers;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Spawn
{
    internal static class ActorSpawnActorIdHelper
    {
        public static bool EnsureActorId(
            Type ownerType,
            IUniqueIdFactory uniqueIdFactory,
            string currentActorId,
            GameObject idSource,
            string actorLabel,
            Action<string> assignActorId)
        {
            if (!string.IsNullOrWhiteSpace(currentActorId))
            {
                return true;
            }

            if (assignActorId == null)
            {
                DebugUtility.LogError(ownerType,
                    $"Assign callback ausente; não é possível aplicar ActorId para {actorLabel}.");
                return false;
            }

            if (uniqueIdFactory == null)
            {
                DebugUtility.LogError(ownerType,
                    $"IUniqueIdFactory ausente; não é possível gerar ActorId para {actorLabel}.");
                return false;
            }

            string actorId = uniqueIdFactory.GenerateId(idSource);
            if (string.IsNullOrWhiteSpace(actorId))
            {
                DebugUtility.LogError(ownerType,
                    $"IUniqueIdFactory retornou ActorId vazio; abortando spawn de {actorLabel}.");
                return false;
            }

            assignActorId(actorId);
            return true;
        }
    }
}
