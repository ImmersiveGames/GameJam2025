using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [CreateAssetMenu(fileName = "PlanetResourcesData", menuName = "ImmersiveGames/PlanetResources")]
    public class PlanetResourcesSo : VisualResourceDefinition
    {
        [SerializeField] private PlanetResources resourceType;

        public Sprite ResourceIcon => GetIcon();
        public PlanetResources ResourceType => resourceType;

        private void OnValidate()
        {
            if (type != ResourceType.PlanetResource)
            {
                type = ResourceType.PlanetResource;
            }
        }
    }

    public enum PlanetResources
    {
        Metal,
        Gas,
        Water,
        Rocks,
        Life
    }
}
