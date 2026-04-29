using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.GameplayRuntime.GameplayReset.Coordination;
using _ImmersiveGames.NewScripts.GameplayRuntime.GameplayReset.Core;
using _ImmersiveGames.NewScripts.GameplayRuntime.GameplayReset.Discovery;
using _ImmersiveGames.NewScripts.GameplayRuntime.GameplayReset.Integration;
using _ImmersiveGames.NewScripts.ResetFlow.SceneReset.Hooks;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    public sealed partial class SceneScopeCompositionRoot
    {
        private void RegisterActorGroupGameplayResetServices(
            IDependencyProvider provider,
            SceneResetHookRegistry hookRegistry,
            Transform worldRoot)
        {
            // ----------------------------
            // ActorGroupGameplayReset (grupos/targets)
            // ----------------------------
            // Classificador de alvos canônicos por grupo/ids para reset de gameplay.
            if (!provider.TryGetForScene<IActorGroupGameplayResetTargetClassifier>(_sceneName, out var classifier) || classifier == null)
            {
                classifier = new ActorGroupGameplayResetDefaultTargetClassifier();
                provider.RegisterForScene(_sceneName, classifier, allowOverride: false);

                DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                    $"IActorGroupGameplayResetTargetClassifier registrado para a cena '{_sceneName}'.");
            }

            // Orquestrador de reset de gameplay (por fases) acionável por participantes do reset.
            if (!provider.TryGetForScene<IActorGroupGameplayResetOrchestrator>(_sceneName, out var gameplayReset) || gameplayReset == null)
            {
                gameplayReset = new ActorGroupGameplayResetOrchestrator(_sceneName);
                provider.RegisterForScene(_sceneName, gameplayReset, allowOverride: false);

                DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                    $"IActorGroupGameplayResetOrchestrator registrado para a cena '{_sceneName}'.");
            }

            // Ponte de reset scoped -> ActorGroupGameplayReset
            var playersResetParticipant = new PlayerActorGroupGameplayResetWorldParticipant();
            provider.RegisterForScene<IActorGroupGameplayResetWorldParticipant>(
                _sceneName,
                playersResetParticipant,
                allowOverride: false);
            DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                $"IActorGroupGameplayResetWorldParticipant registrado para a cena '{_sceneName}'.");

            RegisterSceneLifecycleHooks(hookRegistry, worldRoot);
        }
    }
}



