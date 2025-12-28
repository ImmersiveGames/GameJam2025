// DEPRECATED QA TOOL — ver Docs/Reports/QA-Audit-2025-12-27.md
﻿using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA
{
    [System.Obsolete("Deprecated QA tool; see QA-Audit-2025-12-27", false)]
    public sealed class ActorLifecycleHookLogger : ActorLifecycleHookBase
    {
        [SerializeField] private string label = "ActorLifecycleHookLogger";

        public override Task OnBeforeActorDespawnAsync()
        {
            DebugUtility.Log(typeof(ActorLifecycleHookLogger),
                $"[QA] {label} -> OnBeforeActorDespawnAsync (go={gameObject.name})");
            return Task.CompletedTask;
        }

        public override Task OnAfterActorSpawnAsync()
        {
            DebugUtility.Log(typeof(ActorLifecycleHookLogger),
                $"[QA] {label} -> OnAfterActorSpawnAsync (go={gameObject.name})");
            return Task.CompletedTask;
        }

        // Mantemos os outros como no-op para evitar ruído.
    }
}
