using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.RunRearm.Core;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.RunRearm.Interop;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Hooks;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public sealed partial class SceneScopeCompositionRoot
    {
        private void RegisterRunRearmServices(
            IDependencyProvider provider,
            WorldLifecycleHookRegistry hookRegistry,
            Transform worldRoot)
        {
            // ----------------------------
            // Gameplay Reset (Groups/Targets)
            // ----------------------------
            // Classificador de alvos (Players/Eater/ActorIdSet/All) para reset de gameplay.
            if (!provider.TryGetForScene<IRunRearmTargetClassifier>(_sceneName, out var classifier) || classifier == null)
            {
                provider.TryGetGlobal<IRuntimeModeProvider>(out var runtimeModeProvider);
                provider.TryGetGlobal<IDegradedModeReporter>(out var degradedModeReporter);

                classifier = new DefaultRunRearmTargetClassifier(runtimeModeProvider, degradedModeReporter);
                provider.RegisterForScene(_sceneName, classifier, allowOverride: false);

                DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                    $"IRunRearmTargetClassifier registrado para a cena '{_sceneName}'.");
            }

            // Orquestrador de reset de gameplay (por fases) acionável por participantes do WorldLifecycle.
            if (!provider.TryGetForScene<IRunRearmOrchestrator>(_sceneName, out var gameplayReset) || gameplayReset == null)
            {
                gameplayReset = new RunRearmOrchestrator(_sceneName);
                provider.RegisterForScene(_sceneName, gameplayReset, allowOverride: false);

                DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                    $"IRunRearmOrchestrator registrado para a cena '{_sceneName}'.");
            }

            // Ponte WorldLifecycle soft reset -> Gameplay reset
            var playersResetParticipant = new PlayersRunRearmWorldParticipant();
            provider.RegisterForScene<IRunRearmWorldParticipant>(
                _sceneName,
                playersResetParticipant,
                allowOverride: false);
            DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                $"IRunRearmWorldBridge registrado para a cena '{_sceneName}'.");

            RegisterSceneLifecycleHooks(hookRegistry, worldRoot);
        }
    }
}
