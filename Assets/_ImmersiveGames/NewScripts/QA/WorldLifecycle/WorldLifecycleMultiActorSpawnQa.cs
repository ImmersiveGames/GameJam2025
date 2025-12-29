#if UNITY_EDITOR || DEVELOPMENT_BUILD || NEWSCRIPTS_QA
using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.QA.WorldLifecycle
{
    /// <summary>
    /// QA simples para validar se o ActorRegistry da GameplayScene contém Player e Eater após o reset do WorldLifecycle.
    /// </summary>
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class WorldLifecycleMultiActorSpawnQa : MonoBehaviour
    {
        private const string GameplaySceneName = "GameplayScene";

        private readonly List<IActor> _actors = new(16);
        private EventBinding<WorldLifecycleResetCompletedEvent> _resetCompletedBinding;
        private bool _registered;

        private void Awake()
        {
            _resetCompletedBinding = new EventBinding<WorldLifecycleResetCompletedEvent>(OnWorldResetCompleted);
            RegisterBinding();
        }

        private void OnEnable()
        {
            RegisterBinding();
        }

        private void OnDisable()
        {
            UnregisterBinding();
        }

        private void OnDestroy()
        {
            UnregisterBinding();
        }

        private void RegisterBinding()
        {
            if (_registered)
            {
                return;
            }

            EventBus<WorldLifecycleResetCompletedEvent>.Register(_resetCompletedBinding);
            _registered = true;
        }

        private void UnregisterBinding()
        {
            if (!_registered)
            {
                return;
            }

            EventBus<WorldLifecycleResetCompletedEvent>.Unregister(_resetCompletedBinding);
            _registered = false;
        }

        private void OnWorldResetCompleted(WorldLifecycleResetCompletedEvent evt)
        {
            string activeScene = SceneManager.GetActiveScene().name;
            if (!string.Equals(activeScene, GameplaySceneName, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose(typeof(WorldLifecycleMultiActorSpawnQa),
                    $"[QA][WorldLifecycle] ResetCompleted ignored (activeScene='{activeScene}', expected='{GameplaySceneName}', reason='{evt.Reason ?? "<null>"}').");
                return;
            }

            var provider = DependencyManager.Provider;
            if (!provider.TryGetForScene<IActorRegistry>(GameplaySceneName, out var actorRegistry) || actorRegistry == null)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleMultiActorSpawnQa),
                    $"[QA][WorldLifecycle] IActorRegistry não encontrado para a cena '{GameplaySceneName}'. reason='{evt.Reason ?? "<null>"}'.");
                return;
            }

            _actors.Clear();
            actorRegistry.GetActors(_actors);

            int totalActors = 0;
            int countPlayer = 0;
            int countEater = 0;

            foreach (var actor in _actors)
            {
                if (actor == null)
                {
                    continue;
                }

                totalActors++;

                if (actor is not IActorKindProvider kindProvider)
                {
                    continue;
                }

                if (kindProvider.Kind == ActorKind.Player)
                {
                    countPlayer++;
                }
                else if (kindProvider.Kind == ActorKind.Eater)
                {
                    countEater++;
                }
            }

            DebugUtility.Log(typeof(WorldLifecycleMultiActorSpawnQa),
                $"[QA][WorldLifecycle] Summary: Total={totalActors}, Players={countPlayer}, Eaters={countEater}, " +
                $"scene='{GameplaySceneName}', reason='{evt.Reason ?? "<null>"}'");

            if (countPlayer < 1 || countEater < 1)
            {
                DebugUtility.LogError(typeof(WorldLifecycleMultiActorSpawnQa),
                    $"[QA][WorldLifecycle] ERROR: Expected at least 1 Player and 1 Eater after reset, " +
                    $"but found Players={countPlayer}, Eaters={countEater}. " +
                    $"scene='{GameplaySceneName}', reason='{evt.Reason ?? "<null>"}'");
            }
        }
    }
}
#endif
