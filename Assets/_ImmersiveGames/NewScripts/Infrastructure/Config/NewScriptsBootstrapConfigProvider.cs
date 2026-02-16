using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Config
{
    /// <summary>
    /// Provider de referência direta para o bootstrap config no scene de boot.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NewScriptsBootstrapConfigProvider : MonoBehaviour
    {
        [SerializeField] private NewScriptsBootstrapConfigAsset config;

        public NewScriptsBootstrapConfigAsset Config => config;
    }
}
