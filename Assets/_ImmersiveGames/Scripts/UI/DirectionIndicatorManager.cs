using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

public class DirectionIndicatorManager : MonoBehaviour
{
    [SerializeField] Transform indicatorObject;
    [SerializeField] Sprite eaterIcon;

    private Transform eaterTransform;
    private bool hasSpawnedPlanets = false;

    private void Start()
    {
        eaterTransform = GameManager.Instance.WorldEater;
        SpawnIndicator(eaterTransform, eaterIcon, false);
        SpawnPlanetsIndicators();
    }

    void Update()
    {
        if (hasSpawnedPlanets == false)
            SpawnPlanetsIndicators();
    }

    private void SpawnIndicator(Transform targetIndicator, Sprite targetIcon, bool isHidden)
    {
        Transform indicator = Instantiate(indicatorObject, transform.position, Quaternion.identity);
        indicator.SetParent(this.transform);
        DirectionIndicatorObjectUI directionIndicatorObjectUI = indicator.GetComponent<DirectionIndicatorObjectUI>();
        directionIndicatorObjectUI.Setup(targetIndicator, targetIcon, isHidden);
    }

    private void SpawnPlanetsIndicators()
    {
        List<IDetectable> planetList = PlanetsManager.Instance.GetActivePlanets();

        if (planetList.Count == 0) Debug.Log("NÃO TEM PLANETA SPAWNADO");

        foreach (IDetectable planet in planetList)
        {
            SpawnIndicator(
                planet.GetPlanetsMaster().transform,
                planet.GetResource().ResourceIcon,
                false
            );
        }

        if (planetList.Count > 0) hasSpawnedPlanets = true;
    }
}
