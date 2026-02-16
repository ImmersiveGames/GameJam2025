using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Config
{
    [CreateAssetMenu(
        fileName = "NewScriptsBootstrapConfigManifestAsset",
        menuName = "ImmersiveGames/NewScripts/Infrastructure/Config/Configs/NewScriptsBootstrapConfigManifestAsset",
        order = 21)]
    public sealed class NewScriptsBootstrapConfigManifestAsset : ScriptableObject
    {
        [SerializeField] private NewScriptsBootstrapConfigAsset config;

        public NewScriptsBootstrapConfigAsset Config => config;
    }
}
