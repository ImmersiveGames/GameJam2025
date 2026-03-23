using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.ActorGroupRearm.Core;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.ActorGroupRearm.Interop;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Hooks;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public sealed partial class SceneScopeCompositionRoot
    {
        private void RegisterActorGroupRearmServices(
            IDependencyProvider provider,
            SceneResetHookRegistry hookRegistry,
            Transform worldRoot)
        {
            // ----------------------------
            // ActorGroupRearm (grupos/targets)
            // ----------------------------
            // Classificador de alvos can?nicos por grupo/ids para reset de gameplay.
            if (!provider.TryGetForScene<IActorGroupRearmTargetClassifier>(_sceneName, out var classifier) || classifier == null)
            {
                classifier = new DefaultActorGroupRearmTargetClassifier();
                provider.RegisterForScene(_sceneName, classifier, allowOverride: false);

                DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                    $"IActorGroupRearmTargetClassifier registrado para a cena '{_sceneName}'.");
            }

            // Orquestrador de reset de gameplay (por fases) acion?vel por participantes do WorldLifecycle.
            if (!provider.TryGetForScene<IActorGroupRearmOrchestrator>(_sceneName, out var gameplayReset) || gameplayReset == null)
            {
                gameplayReset = new ActorGroupRearmOrchestrator(_sceneName);
                provider.RegisterForScene(_sceneName, gameplayReset, allowOverride: false);

                DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                    $"IActorGroupRearmOrchestrator registrado para a cena '{_sceneName}'.");
            }

            // Ponte WorldLifecycle soft reset -> ActorGroupRearm
            var playersResetParticipant = new PlayersActorGroupRearmWorldParticipant();
            provider.RegisterForScene<IActorGroupRearmWorldParticipant>(
                _sceneName,
                playersResetParticipant,
                allowOverride: false);
            DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                $"IActorGroupRearmWorldBridge registrado para a cena '{_sceneName}'.");

            RegisterSceneLifecycleHooks(hookRegistry, worldRoot);
        }
    }
}

