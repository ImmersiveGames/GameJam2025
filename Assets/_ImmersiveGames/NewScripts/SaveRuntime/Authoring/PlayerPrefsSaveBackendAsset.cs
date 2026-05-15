using _ImmersiveGames.NewScripts.SaveRuntime.Backends.PlayerPrefs;
using _ImmersiveGames.NewScripts.SaveRuntime.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SaveRuntime.Authoring
{
    [CreateAssetMenu(
        fileName = "PlayerPrefsSaveBackend",
        menuName = "ImmersiveGames/NewScripts/Save/PlayerPrefs Save Backend",
        order = 21)]
    public sealed class PlayerPrefsSaveBackendAsset : SaveBackendAsset
    {
        public override string BackendId => "PlayerPrefsSaveBackend";

        public override ISaveBackend CreateBackend()
        {
            return new PlayerPrefsSaveBackend();
        }
    }
}

