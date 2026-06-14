using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    // PlayerMovementControlStage
    // Mantido como implementação ativa para controle de movimento de players no pipeline macro.
    // (O rail "Player" paralelo ainda existe aqui para movement control durante activities.
    //  Futura remoção/migração faz parte do H2 do plano de higiene de arquitetura.)
    internal static class PlayerMovementControlStage
    {
        public static IReadOnlyList<MovementControlRecord> Execute(
            SessionActivityIdentity scopeIdentity,
            IReadOnlyList<PlayerActorIdentityRecord> targets,
            bool enable,
            IPlayerMovementControlAdapter adapter,
            ActivityPlayerActorRegistry registry,
            string source,
            string reason)
        {
            MovementControlCommand controlCommand = new(
                scopeIdentity,
                targets,
                enable,
                source,
                reason);

            if (!controlCommand.IsValid)
            {
                throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][MovementControl] Invalid command activityId='{scopeIdentity.ActivityId}' entrySequence='{scopeIdentity.EntrySequence}' enable='{enable}'.");
            }

            IReadOnlyList<MovementControlRecord> records = adapter.Execute(controlCommand, scopeIdentity, registry);
            if (records == null || records.Count != targets.Count)
            {
                throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][MovementControl] Adapter result mismatch activityId='{scopeIdentity.ActivityId}' entrySequence='{scopeIdentity.EntrySequence}' enable='{enable}'.");
            }

            return records;
        }
    }
}
