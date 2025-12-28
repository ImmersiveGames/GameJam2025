// DEPRECATED QA TOOL — ver Docs/Reports/QA-Audit-2025-12-27.md
using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Hooks;

namespace _ImmersiveGames.NewScripts.QA
{
    public enum FaultyLifecyclePhase
    {
        BeforeDespawn,
        AfterDespawn,
        BeforeSpawn,
        AfterSpawn
    }

    [System.Obsolete("Deprecated QA tool; see QA-Audit-2025-12-27", false)]
    public sealed class QaFaultySceneLifecycleHook : IWorldLifecycleHook
    {
        private readonly FaultyLifecyclePhase _faultyPhase;
        private readonly string _label;

        public QaFaultySceneLifecycleHook(string label, FaultyLifecyclePhase faultyPhase)
        {
            _label = string.IsNullOrWhiteSpace(label) ? nameof(QaFaultySceneLifecycleHook) : label;
            _faultyPhase = faultyPhase;
        }

        public Task OnBeforeDespawnAsync()
        {
            return MaybeThrowAsync(FaultyLifecyclePhase.BeforeDespawn, nameof(OnBeforeDespawnAsync));
        }

        public Task OnAfterDespawnAsync()
        {
            return MaybeThrowAsync(FaultyLifecyclePhase.AfterDespawn, nameof(OnAfterDespawnAsync));
        }

        public Task OnBeforeSpawnAsync()
        {
            return MaybeThrowAsync(FaultyLifecyclePhase.BeforeSpawn, nameof(OnBeforeSpawnAsync));
        }

        public Task OnAfterSpawnAsync()
        {
            return MaybeThrowAsync(FaultyLifecyclePhase.AfterSpawn, nameof(OnAfterSpawnAsync));
        }

        private Task MaybeThrowAsync(FaultyLifecyclePhase currentPhase, string methodName)
        {
            if (_faultyPhase == currentPhase)
            {
                throw new InvalidOperationException($"[QA] {_label} forced failure at {methodName}");
            }

            DebugUtility.Log(typeof(QaFaultySceneLifecycleHook),
                $"[QA] {_label} executed successfully at {methodName}");
            return Task.CompletedTask;
        }
    }
}
