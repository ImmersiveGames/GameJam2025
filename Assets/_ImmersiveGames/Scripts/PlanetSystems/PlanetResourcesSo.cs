using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [CreateAssetMenu(fileName = "PlanetResourcesData",menuName = "ImmersiveGames/PlanetResources")]
    public class PlanetResourcesSo : ScriptableObject
    {
        [SerializeField] private PlanetResources resourceType;
        [SerializeField] private Sprite resourceIcon;
        
        public Sprite ResourceIcon => resourceIcon;
        public PlanetResources ResourceType => resourceType;
    }
    public enum PlanetResources { Metal, Gas, Water, Rocks, Life }
}