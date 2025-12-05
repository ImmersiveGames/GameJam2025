using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    [CreateAssetMenu(
        fileName = "RadialEvenSpawnPattern",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Spawn Patterns/Radial Even")]
    public class RadialEvenSpawnPatternSo : DefenseSpawnPatternSo
    {
        public override Vector3 GetSpawnOffset(int index, int total, float radius, float heightOffset)
        {
            if (total <= 0 || radius <= 0f)
                return Vector3.zero;

            float angle = (Mathf.PI * 2f) * (index / (float)total);
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            return new Vector3(x, heightOffset, z);
        }
    }
}