using UnityEngine;
namespace _ImmersiveGames.Scripts.DetectionsSystems.Core
{
    [CreateAssetMenu(fileName = "DetectionType", menuName = "ImmersiveGames/Detection/DetectionType", order = 1)]
    public class DetectionType : ScriptableObject
    {
        [SerializeField] private string typeName = "NewDetection";
        public string TypeName => typeName;

        public override string ToString() => typeName;
    }
}