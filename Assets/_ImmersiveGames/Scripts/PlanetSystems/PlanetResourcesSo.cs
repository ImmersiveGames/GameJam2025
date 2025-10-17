using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using UnityEngine;
using UnityEngine.Serialization;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [CreateAssetMenu(fileName = "PlanetResourcesData", menuName = "ImmersiveGames/PlanetResources")]
    public class PlanetResourcesSo : VisualResourceDefinition
    {
        [FormerlySerializedAs("resourceType")]
        [SerializeField] private PlanetResources resourceId;

        public Sprite ResourceIcon => GetIcon();
        public PlanetResources ResourceId => resourceId;

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
