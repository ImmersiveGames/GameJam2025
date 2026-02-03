using _ImmersiveGames.Scripts.GameplaySystems.Domain;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Core.Composition;
using UnityEngine;

namespace _ImmersiveGames.Scripts.GameplaySystems.Bootstrap
{
    [DefaultExecutionOrder(-200)]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplayDomainBootstrapper : MonoBehaviour
    {
        [Tooltip("Quando verdadeiro, permite sobrescrever serviços de cena já registrados (útil em testes).")]
        [SerializeField] private bool allowOverride;

        private void Awake()
        {
            var sceneName = gameObject.scene.name;

            var registry = new OldActorRegistry();
            var playerDomain = new PlayerDomain();
            var eaterDomain = new EaterDomain();

            DependencyManager.Provider.RegisterForScene<IOldActorRegistry>(sceneName, registry, allowOverride);
            DependencyManager.Provider.RegisterForScene<IPlayerDomain>(sceneName, playerDomain, allowOverride);
            DependencyManager.Provider.RegisterForScene<IEaterDomain>(sceneName, eaterDomain, allowOverride);

            DebugUtility.Log<GameplayDomainBootstrapper>(
                $"GameplayDomainBootstrapper registrou serviços de domínio para a cena '{sceneName}'.",
                DebugUtility.Colors.Success);
        }
    }
}
