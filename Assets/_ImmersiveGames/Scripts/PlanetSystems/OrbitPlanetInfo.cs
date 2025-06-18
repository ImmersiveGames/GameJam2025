using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [System.Serializable]
    public struct OrbitPlanetInfo
    {
        public Vector3 orbitPosition;
        public float planetRadius;
        public float initialAngle;
        public float orbitSpeed;
        public Bounds planetBounds; // Novo: tamanho real do planeta

        public OrbitPlanetInfo(Vector3 position, float radius, float angle, float speed, Bounds bounds)
        {
            orbitPosition = position;
            planetRadius = radius;
            initialAngle = angle;
            orbitSpeed = speed;
            planetBounds = bounds;
        }
    }
}