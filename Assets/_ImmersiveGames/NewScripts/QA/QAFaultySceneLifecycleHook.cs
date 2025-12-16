using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.World;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.NewScripts.QA
{
    public enum FaultyLifecyclePhase
    {
        BeforeDespawn,
        AfterDespawn,
        BeforeSpawn,
        AfterSpawn
    }

    public sealed class QAFaultySceneLifecycleHook : IWorldLifecycleHook
    {
        private readonly FaultyLifecyclePhase _faultyPhase;
        private readonly string _label;

        public QAFaultySceneLifecycleHook(string label, FaultyLifecyclePhase faultyPhase)
        {
            _label = string.IsNullOrWhiteSpace(label) ? nameof(QAFaultySceneLifecycleHook) : label;
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

            DebugUtility.Log(typeof(QAFaultySceneLifecycleHook),
                $"[QA] {_label} executed successfully at {methodName}");
            return Task.CompletedTask;
        }
    }
}
