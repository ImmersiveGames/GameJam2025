using _ImmersiveGames.NewScripts.SaveRuntime.Contracts;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.SaveRuntime.Authoring
{
    public abstract class SaveBackendAsset : ScriptableObject
    {
        public abstract string BackendId { get; }

        public abstract ISaveBackend CreateBackend();
    }
}

