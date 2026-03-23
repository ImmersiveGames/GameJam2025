using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.ActorGroupRearm.Interop;

namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime.SceneReset.Phases
{
    internal sealed class ScopedParticipantsResetPhase : ISceneResetPhase
    {
        public async Task ExecuteAsync(SceneResetContext context, SceneResetHookRunner hookRunner, CancellationToken ct)
        {
            if (context.ResetContext == null)
            {
                return;
            }

            List<IActorGroupRearmWorldParticipant> participants = context.CollectScopedParticipants();
            if (participants.Count == 0)
            {
                DebugUtility.LogVerbose(typeof(SceneResetFacade),
                    "Scoped reset participants step skipped (participants=0)");
                return;
            }

            var filtered = new List<IActorGroupRearmWorldParticipant>();
            foreach (IActorGroupRearmWorldParticipant participant in participants)
            {
                if (participant == null)
                {
                    DebugUtility.LogError(typeof(SceneResetFacade),
                        "Scoped reset participant é nulo e será ignorado.");
                    continue;
                }

                if (!context.ResetContext.Value.ContainsScope(participant.Scope))
                {
                    DebugUtility.LogVerbose(typeof(SceneResetFacade),
                        $"Scoped reset participant skipped by scope: {participant.GetType().Name}");
                    continue;
                }

                filtered.Add(participant);
            }

            if (filtered.Count == 0)
            {
                DebugUtility.LogVerbose(typeof(SceneResetFacade),
                    "Scoped reset participants step skipped (filtered=0)");
                return;
            }

            filtered.Sort(context.CompareResetScopeParticipants);
            LogScopedParticipantOrder(filtered);

            foreach (IActorGroupRearmWorldParticipant participant in filtered)
            {
                ct.ThrowIfCancellationRequested();
                string participantType = participant.GetType().FullName ?? participant.GetType().Name;
                var watch = Stopwatch.StartNew();
                DebugUtility.LogVerbose(typeof(SceneResetFacade),
                    $"Scoped reset started: {participantType}");

                try
                {
                    await participant.ResetAsync(context.ResetContext.Value);
                }
                catch (Exception ex)
                {
                    DebugUtility.LogError(typeof(SceneResetFacade),
                        $"Scoped reset falhou para {participantType}: {ex}");
                    throw;
                }
                finally
                {
                    watch.Stop();
                    if (watch.ElapsedMilliseconds > SceneResetContext.SlowHookWarningMs)
                    {
                        DebugUtility.LogWarning(typeof(SceneResetFacade),
                            $"Scoped reset lento: {participantType} levou {watch.ElapsedMilliseconds}ms.");
                    }

                    DebugUtility.LogVerbose(typeof(SceneResetFacade),
                        $"Scoped reset duration: {participantType} => {watch.ElapsedMilliseconds}ms");
                }

                DebugUtility.LogVerbose(typeof(SceneResetFacade),
                    $"Scoped reset completed: {participantType}");
            }
        }

        private static void LogScopedParticipantOrder(List<IActorGroupRearmWorldParticipant> participants)
        {
            string[] orderedLabels = participants
                .Select(participant =>
                {
                    string typeName = participant?.GetType().Name ?? "<null participant>";
                    return $"{typeName}(scope={participant?.Scope}, order={participant?.Order})";
                })
                .ToArray();

            DebugUtility.LogVerbose(typeof(SceneResetFacade),
                $"Scoped reset execution order: {string.Join(", ", orderedLabels)}");
        }
    }
}
