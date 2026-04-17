using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.Scripts.GameplaySystems.Domain;
using UnityEngine;

namespace _ImmersiveGames.Scripts.GameplaySystems.Bootstrap
{
    [DefaultExecutionOrder(-200)]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplayDomainBootstrapper : MonoBehaviour
    {
        [Tooltip("Quando verdadeiro, permite sobrescrever servi�os de cena j� registrados (�til em testes).")]
        [SerializeField] private bool allowOverride;

        private void Awake()
        {
            string sceneName = gameObject.scene.name;

            var registry = new OldActorRegistry();
            var playerDomain = new PlayerDomain();
            var eaterDomain = new EaterDomain();

            DependencyManager.Provider.RegisterForScene<IOldActorRegistry>(sceneName, registry, allowOverride);
            DependencyManager.Provider.RegisterForScene<IPlayerDomain>(sceneName, playerDomain, allowOverride);
            DependencyManager.Provider.RegisterForScene<IEaterDomain>(sceneName, eaterDomain, allowOverride);

            DebugUtility.Log<GameplayDomainBootstrapper>(
                $"GameplayDomainBootstrapper registrou servi�os de dom�nio para a cena '{sceneName}'.",
                DebugUtility.Colors.Success);
        }
    }
}
