using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.UI
{
    public sealed class LoadedSceneActorAttributeUiBindingRequestProvider : IActorAttributeUiBindingRequestProvider
    {
        public IReadOnlyList<ActorAttributeUiBindingRequestEntry> GetRequests()
        {
            List<ActorAttributeUiBindingRequestEntry> requests = new();
            int providerCount = 0;
            int loadedSceneCount = 0;
            int collectedSceneCount = 0;

            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                var scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                loadedSceneCount += 1;
                GameObject[] rootObjects = scene.GetRootGameObjects();
                bool sceneHadProvider = false;
                for (int rootIndex = 0; rootIndex < rootObjects.Length; rootIndex++)
                {
                    var root = rootObjects[rootIndex];
                    if (root == null)
                    {
                        continue;
                    }

                    SceneActorAttributeUiBindingRequestProvider[] providers = root.GetComponentsInChildren<SceneActorAttributeUiBindingRequestProvider>(true);
                    if (providers == null || providers.Length == 0)
                    {
                        continue;
                    }

                    sceneHadProvider = true;
                    for (int providerIndex = 0; providerIndex < providers.Length; providerIndex++)
                    {
                        var provider = providers[providerIndex];
                        if (provider == null)
                        {
                            continue;
                        }

                        providerCount += 1;
                        IReadOnlyList<ActorAttributeUiBindingRequestEntry> providerRequests = provider.GetRequests();
                        if (providerRequests == null || providerRequests.Count == 0)
                        {
                            continue;
                        }

                        for (int requestIndex = 0; requestIndex < providerRequests.Count; requestIndex++)
                        {
                            requests.Add(providerRequests[requestIndex]);
                        }
                    }
                }

                if (sceneHadProvider)
                {
                    collectedSceneCount += 1;
                }
            }

            if (providerCount == 0)
            {
                DebugUtility.Log(
                    typeof(LoadedSceneActorAttributeUiBindingRequestProvider),
                    $"event='ActorAttributeUiSceneRequestProvidersCollected' sceneCount='{loadedSceneCount}' providerCount='0' requestCount='0'",
                    DebugUtility.Colors.Info);
                DebugUtility.LogVerbose(
                    typeof(LoadedSceneActorAttributeUiBindingRequestProvider),
                    "event='ActorAttributeUiSceneRequestProviderSkipped' reason='no_binding_request_providers'",
                    DebugUtility.Colors.Info);
                return new EmptyActorAttributeUiBindingRequestProvider().GetRequests();
            }

            DebugUtility.Log(
                typeof(LoadedSceneActorAttributeUiBindingRequestProvider),
                $"event='ActorAttributeUiSceneRequestProvidersCollected' sceneCount='{loadedSceneCount}' collectedSceneCount='{collectedSceneCount}' providerCount='{providerCount}' requestCount='{requests.Count}'",
                requests.Count > 0 ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);

            return requests;
        }
    }
}
