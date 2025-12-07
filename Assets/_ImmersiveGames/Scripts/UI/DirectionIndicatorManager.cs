using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.GameplaySystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.UI
{
    public class DirectionIndicatorManager : MonoBehaviour
    {
        [SerializeField] Transform indicatorObject;
        [SerializeField] Sprite eaterIcon;

        [Inject] private IGameplayManager _gameplayManager;

        private Transform _eaterTransform;
        private bool _hasSpawnedPlanets;

        private void Awake()
        {
            DependencyManager.Provider.InjectDependencies(this);
        }

        private void Start()
        {
            _eaterTransform = (_gameplayManager ?? GameplayManager.Instance)?.WorldEater;
            SpawnIndicator(_eaterTransform, eaterIcon, false);
            SpawnPlanetsIndicators();
        }

        void Update()
        {
            if (_hasSpawnedPlanets == false)
                SpawnPlanetsIndicators();
        }

        private void SpawnIndicator(Transform targetIndicator, Sprite targetIcon, bool isHidden)
        {
            var indicator = Instantiate(indicatorObject, transform.position, Quaternion.identity);
            indicator.SetParent(transform);
            var directionIndicatorObjectUI = indicator.GetComponent<DirectionIndicatorObjectUI>();
            directionIndicatorObjectUI.Setup(targetIndicator, targetIcon, isHidden);
        }

        private void SpawnPlanetsIndicators()
        {
            List<IDetectable> planetList = PlanetsManager.Instance.GetActivePlanets();

            if (planetList.Count == 0) DebugUtility.LogVerbose<DirectionIndicatorManager>("NÃO TEM PLANETA SPAWNADO");

            if (planetList.Count > 0) _hasSpawnedPlanets = true;
        }
    }
}
