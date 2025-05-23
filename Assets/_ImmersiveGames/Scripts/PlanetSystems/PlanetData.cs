using System.Collections.Generic;
using _ImmersiveGames.Scripts.EnemySystem;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [CreateAssetMenu(fileName = "PlanetData", menuName = "ImmersiveGames/PlanetData")]
    public class PlanetData : DestructibleObjectSo
    {
        [SerializeField, Tooltip("Prefab do inimigo usado pelo spawner")]
        public GameObject enemyPrefab; // Novo campo

        [SerializeField, Tooltip("Prefab do modelo visual do planeta")]
        public GameObject modelPrefab;

        [SerializeField, Tooltip("Tamanho do planeta no plano XZ para cálculo de órbita (metros)")]
        public float size = 5f;

        [SerializeField, Tooltip("Raio de detecção do jogador para spawn de inimigos (metros)")]
        public float detectionRadius = 10f;

        [SerializeField, Tooltip("Lista de EnemyData para inimigos que podem ser spawnados pelo planeta")]
        public List<EnemyData> enemyDatas;
        
        [SerializeField, Tooltip("Tempo entre spawns de inimigos (segundos)")]
        public float spawnRate = 2f;
        
        [SerializeField, Tooltip("Número máximo de inimigos ativos spawnados pelo planeta")]
        public int maxEnemies = 5;

        [SerializeField, Tooltip("Velocidade mínima de órbita em torno do centro do universo (graus por segundo)")]
        public float minOrbitSpeed = 10f;

        [SerializeField, Tooltip("Velocidade máxima de órbita em torno do centro do universo (graus por segundo)")]
        public float maxOrbitSpeed = 20f;

        [SerializeField, Tooltip("Se ativado, a órbita é no sentido horário")]
        public bool orbitClockwise = true;

        [SerializeField, Tooltip("Multiplicador mínimo de escala para o modelo do planeta")]
        public float minScaleMultiplier = 0.8f;

        [SerializeField, Tooltip("Multiplicador máximo de escala para o modelo do planeta")]
        public float maxScaleMultiplier = 1.2f;

        [SerializeField, Tooltip("Ângulo mínimo de inclinação do modelo do planeta (graus)")]
        public float minTiltAngle = -15f;

        [SerializeField, Tooltip("Ângulo máximo de inclinação do modelo do planeta (graus)")]
        public float maxTiltAngle = 15f;

        [SerializeField, Tooltip("Velocidade mínima de rotação do planeta em torno de seu próprio eixo (graus por segundo)")]
        public float minRotationSpeed = 10f;

        [SerializeField, Tooltip("Velocidade máxima de rotação do planeta em torno de seu próprio eixo (graus por segundo)")]
        public float maxRotationSpeed = 30f;

        [SerializeField, Tooltip("Se ativado, a rotação é no sentido horário")]
        public bool rotateClockwise = true;
    }
}