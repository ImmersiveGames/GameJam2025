using UnityEngine;
namespace _ImmersiveGames.Scripts.ScriptableObjects
{
    [CreateAssetMenu(fileName = "NovaPlanetaConfig", menuName = "ImmersiveGames/PlanetaConfig", order = 1)]
    public class PlanetData : DestructibleObjectSo
    {
        [Header("Modelo Visual")]
        [SerializeField, Tooltip("Tamanho do planeta (em unidades, usado para espaçamento de órbita)")]
        public float size = 2f;

        [Header("Órbita")]
        [SerializeField, Tooltip("Velocidade mínima de órbita (graus por segundo)")]
        public float minOrbitSpeed = 20f;
        [SerializeField, Tooltip("Velocidade máxima de órbita (graus por segundo)")]
        public float maxOrbitSpeed = 40f;
        [SerializeField, Tooltip("Se true, orbita no sentido horário; se false, anti-horário")]
        public bool orbitClockwise = true;

        [Header("Escala do Modelo")]
        [SerializeField, Tooltip("Multiplicador mínimo para a escala do modelo")]
        public float minScaleMultiplier = 0.8f;
        [SerializeField, Tooltip("Multiplicador máximo para a escala do modelo")]
        public float maxScaleMultiplier = 1.2f;

        [Header("Inclinação do Modelo")]
        [SerializeField, Tooltip("Ângulo mínimo de inclinação (graus, eixos X e Z)")]
        public float minTiltAngle = -30f;
        [SerializeField, Tooltip("Ângulo máximo de inclinação (graus, eixos X e Z)")]
        public float maxTiltAngle = 30f;

        [Header("Translação (Rotação Própria)")]
        [SerializeField, Tooltip("Velocidade mínima de rotação (graus por segundo)")]
        public float minRotationSpeed = 10f;
        [SerializeField, Tooltip("Velocidade máxima de rotação (graus por segundo)")]
        public float maxRotationSpeed = 20f;
        [SerializeField, Tooltip("Se true, rotaciona no sentido horário; se false, anti-horário")]
        public bool rotateClockwise = true;
    }
}