using System.Collections.Generic;
using _ImmersiveGames.Scripts.EnemySystem;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystemsOLD
{
    public class PlanetData : DestructibleObjectSo
    {
        [SerializeField, Tooltip("Prefab do inimigo usado pelo spawner")]
        public GameObject enemyPrefab; // Novo campo

        [SerializeField, Tooltip("Prefab do modelo visual do planeta")]
        public GameObject modelPrefab;

       

        [SerializeField, Tooltip("Raio de detecção do jogador para spawn de inimigos (metros)")]
        public float detectionRadius = 10f;

        [SerializeField, Tooltip("Lista de EnemyData para inimigos que podem ser spawnados pelo planeta")]
        public List<EnemyData> enemyDatas;
        
        [SerializeField, Tooltip("Tempo entre spawns de inimigos (segundos)")]
        public float spawnRate = 2f;
        
        [SerializeField, Tooltip("Número máximo de inimigos ativos spawnados pelo planeta")]
        public int maxEnemies = 5;

        

        

        

        

        
    }
}