using _ImmersiveGames.NewScripts.SaveRuntime.Backends.InMemory;
using _ImmersiveGames.NewScripts.SaveRuntime.Contracts;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.SaveRuntime.Authoring
{
    [CreateAssetMenu(
        fileName = "InMemorySaveBackend",
        menuName = "ImmersiveGames/NewScripts/Save/InMemory Save Backend",
        order = 20)]
    public sealed class InMemorySaveBackendAsset : SaveBackendAsset
    {
        public override string BackendId => "InMemorySaveBackend";

        public override ISaveBackend CreateBackend()
        {
            return new InMemorySaveBackend();
        }
    }
}

