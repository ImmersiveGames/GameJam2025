using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.GameplaySystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.UI.DirectIndicator
{
    [DebugLevel(DebugLevel.Verbose)]
    public class DirectionIndicatorManager : MonoBehaviour
    {
        [SerializeField] private Transform indicatorObject;
        [SerializeField] private Sprite eaterIcon;

        [Inject] private IGameplayManager _gameplayManager;

        private Transform _eaterTransform;
        private bool _hasSpawnedPlanets;
        private bool _hasSpawnedEaterIndicator;

        private void Awake()
        {
            DependencyManager.Provider.InjectDependencies(this);
        }

        private void Start()
        {
            TrySpawnEaterIndicatorIfReady();
            SpawnPlanetsIndicators();
        }

        private void Update()
        {
            if (_hasSpawnedEaterIndicator == false)
            {
                TrySpawnEaterIndicatorIfReady();
            }

            if (_hasSpawnedPlanets == false)
            {
                SpawnPlanetsIndicators();
            }
        }

        private void TrySpawnEaterIndicatorIfReady()
        {
            if (_hasSpawnedEaterIndicator)
                return;

            if (_gameplayManager == null)
            {
                DebugUtility.LogWarning<DirectionIndicatorManager>(
                    "IGameplayManager não foi injetado. Verifique se GameplayManager está ativo e registrado no DI.",
                    this);
                return;
            }

            _eaterTransform = _gameplayManager.WorldEater;

            if (_eaterTransform == null)
            {
                // Eater ainda não existe / ainda não registrou no domínio.
                return;
            }

            SpawnIndicator(_eaterTransform, eaterIcon, isHidden: false);
            _hasSpawnedEaterIndicator = true;

            DebugUtility.LogVerbose<DirectionIndicatorManager>(
                $"Indicador do Eater criado com sucesso. Target='{_eaterTransform.name}'.");
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

            if (planetList.Count == 0)
            {
                DebugUtility.LogVerbose<DirectionIndicatorManager>("NÃO TEM PLANETA SPAWNADO");
                return;
            }

            _hasSpawnedPlanets = true;
        }
    }
}
