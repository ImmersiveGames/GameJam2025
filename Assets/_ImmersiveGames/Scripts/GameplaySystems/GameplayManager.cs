using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityUtils;

namespace _ImmersiveGames.Scripts.GameplaySystems
{
    public interface IGameplayManager
    {
        Transform WorldEater { get; }
    }

    public sealed class GameplayManager : Singleton<GameplayManager>, IGameplayManager
    {
        [SerializeField] private Transform worldEater;

        public Transform WorldEater => worldEater;

        protected override void Awake()
        {
            base.Awake();

            // Registro no DependencyManager para injeção via [Inject] IGameplayManager
            DependencyManager.Provider.RegisterGlobal<IGameplayManager>(this, allowOverride: true);

            DebugUtility.Log<GameplayManager>(
                "GameplayManager inicializado.",
                DebugUtility.Colors.Success);
        }

        private void OnDestroy()
        {
            // Se no futuro quiser tratar ciclo de vida:
            // - hoje não temos UnregisterGlobal, então só confiamos no ciclo de cena.
        }
    }
}