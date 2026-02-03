using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.GameplaySystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Core.Composition;
using UnityEngine;
namespace _ImmersiveGames.Scripts.UISystems.DirectIndicator
{
    /// <summary>
    /// LEGADO / OBSOLETO:
    /// Sistema antigo de "localização por indicador". Não evoluir.
    /// Mantido apenas para compatibilidade temporária com cenas existentes.
    /// </summary>
    [Obsolete("Sistema legado/obsoleto. Não evoluir. Planejar remoção após migração para o sistema atual de navegação/UX.")]
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
            // LEGADO: mantém a tentativa original, mas sem depender exclusivamente de Instance.
            TrySpawnEaterIndicator();
            SpawnPlanetsIndicators();
        }

        private void Update()
        {
            // LEGADO: tenta novamente se o eater ainda não existia no Start (spawn tardio).
            if (_hasSpawnedEaterIndicator == false)
            {
                TrySpawnEaterIndicator();
            }

            if (_hasSpawnedPlanets == false)
            {
                SpawnPlanetsIndicators();
            }
        }

        private void TrySpawnEaterIndicator()
        {
            if (_hasSpawnedEaterIndicator)
                return;

            if (_gameplayManager == null)
            {
                // Mantém fallback para não quebrar cenas antigas, mas evita acoplamento como evolução.
                var legacy = GameplayManager.Instance;
                if (legacy == null)
                    return;

                _eaterTransform = legacy.WorldEater;
            }
            else
            {
                _eaterTransform = _gameplayManager.WorldEater;
            }

            if (_eaterTransform == null)
            {
                return;
            }

            SpawnIndicator(_eaterTransform, eaterIcon, false);
            _hasSpawnedEaterIndicator = true;
        }

        private void SpawnIndicator(Transform targetIndicator, Sprite targetIcon, bool isHidden)
        {
            if (indicatorObject == null || targetIndicator == null)
                return;

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

