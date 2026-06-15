using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityPlayerActorParticipationExitFactWriter
    {
        public static void EmitStarted(
            ActivityExitActorTeardownCommand command,
            SessionActivityDefinition definition,
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            SessionActivityIdentity stageStartedIdentity)
        {
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.PlayerActorParticipationExitStageStarted,
                stageStartedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actor participation exit stage started.");
            endpoint.EmitSnapshot(
                snapshots,
                "player_actor_participation_exit_stage_started",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actor participation exit stage started.");
        }

        public static void EmitCommandIssued(
            ActivityExitActorTeardownCommand command,
            SessionActivityDefinition definition,
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            SessionActivityIdentity stageStartedIdentity,
            int actorCount)
        {
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.PlayerActorParticipationExitCommandIssued,
                stageStartedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actor participation exit command issued. actors='{actorCount}'.");
            endpoint.EmitSnapshot(
                snapshots,
                "player_actor_participation_exit_command_issued",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actor participation exit command issued. actors='{actorCount}'.");
        }

        public static void EmitExited(
            ActivityExitActorTeardownCommand command,
            SessionActivityDefinition definition,
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            SessionActivityIdentity stageStartedIdentity,
            int exitRecordCount)
        {
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.PlayerActorParticipationExited,
                stageStartedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actor participation exited. actors='{exitRecordCount}'.");
            endpoint.EmitSnapshot(
                snapshots,
                "player_actor_participation_exited",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actor participation exited. actors='{exitRecordCount}'.");
        }

        public static SessionActivityIdentity EmitCompleted(
            ActivityExitActorTeardownCommand command,
            SessionActivityDefinition definition,
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            SessionActivityIdentity stageCompletedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.PlayerActorParticipationExitStageCompleted, entrySequence);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.PlayerActorParticipationExitStageCompleted,
                stageCompletedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actor participation exit stage completed.");
            endpoint.SetCurrentIdentity(stageCompletedIdentity, SessionActivityStage.PlayerActorParticipationExitStageCompleted);
            endpoint.EmitSnapshot(
                snapshots,
                "player_actor_participation_exit_stage_completed",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actor participation exit stage completed.");
            return stageCompletedIdentity;
        }
    }
}
