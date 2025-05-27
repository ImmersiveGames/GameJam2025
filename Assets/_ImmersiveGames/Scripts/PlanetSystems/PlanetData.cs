using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [CreateAssetMenu(fileName = "Planet Data",menuName = "ImmersiveGames/PoolableObjectData/Planet")]
    public class PlanetData : PoolableObjectData
    {
        [SerializeField, Tooltip("Tamanho do planeta no plano XZ para cálculo de órbita (metros)"), Range(1,10)]
        public float size = 5f;
        [SerializeField, Tooltip("Multiplicador mínimo de escala para o modelo do planeta"), Range(0.1f,1f)]
        public float minScaleMultiplier = 0.8f;

        [SerializeField, Tooltip("Multiplicador máximo de escala para o modelo do planeta"), Range(0.1f,1f)]
        public float maxScaleMultiplier = 1.2f;
        [SerializeField, Tooltip("Ângulo mínimo de inclinação do modelo do planeta (graus)")]
        public float minTiltAngle = -15f;

        [SerializeField, Tooltip("Ângulo máximo de inclinação do modelo do planeta (graus)")]
        public float maxTiltAngle = 15f;
        
        [SerializeField, Tooltip("Velocidade mínima de órbita em torno do centro do universo (graus por segundo)")]
        public float minOrbitSpeed = 10f;

        [SerializeField, Tooltip("Velocidade máxima de órbita em torno do centro do universo (graus por segundo)")]
        public float maxOrbitSpeed = 20f;
        
        [SerializeField, Tooltip("Se ativado, a órbita é no sentido horário")]
        public bool orbitClockwise = true;
        
        [SerializeField, Tooltip("Velocidade mínima de rotação do planeta em torno de seu próprio eixo (graus por segundo)")]
        public float minRotationSpeed = 10f;

        [SerializeField, Tooltip("Velocidade máxima de rotação do planeta em torno de seu próprio eixo (graus por segundo)")]
        public float maxRotationSpeed = 30f;

        [SerializeField, Tooltip("Se ativado, a rotação é no sentido horário")]
        public bool rotateClockwise = true;
    }
}