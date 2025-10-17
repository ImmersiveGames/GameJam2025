using UnityEngine;
using UnityEngine.Serialization;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [CreateAssetMenu(fileName = "PlanetResourcesData", menuName = "ImmersiveGames/PlanetResources")]
    public class PlanetResourcesSo : ScriptableObject
    {
        [SerializeField] private PlanetResources resourceId = PlanetResources.Metal;
        [FormerlySerializedAs("visualIcon")]
        [FormerlySerializedAs("icon")]
        [SerializeField] private Sprite resourceIcon;

        public PlanetResources ResourceId => resourceId;
        public Sprite ResourceIcon => resourceIcon;
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
