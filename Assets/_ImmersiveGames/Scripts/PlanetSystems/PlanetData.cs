﻿using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [CreateAssetMenu(fileName = "PlanetMaster Data",menuName = "ImmersiveGames/PoolableObjectData/PlanetMaster")]
    public class PlanetData : PoolableObjectData
    {
        [SerializeField, Tooltip("Tamanho do planeta no plano XZ para cálculo de órbita (metros)"), Range(1,10)]
        public float size = 5f;
        [SerializeField, Tooltip("Multiplicador mínimo de escala para o modelo do planeta"), Range(1,10)]
        public int minScale = 1;

        [SerializeField, Tooltip("Multiplicador máximo de escala para o modelo do planeta"), Range(10,20)]
        public int maxScale = 4;
        [SerializeField, Tooltip("Ângulo mínimo de inclinação do modelo do planeta (graus)")]
        public float minTiltAngle = -15f;

        [SerializeField, Tooltip("Ângulo máximo de inclinação do modelo do planeta (graus)")]
        public float maxTiltAngle = 15f;
        
        [SerializeField, Tooltip("Velocidade mínima de órbita em torno do centro do universo (graus por segundo)")]
        public float minOrbitSpeed = 10f;

        [SerializeField, Tooltip("Velocidade máxima de órbita em torno do centro do universo (graus por segundo)")]
        public float maxOrbitSpeed = 20f;
        
        [SerializeField, Tooltip("Velocidade mínima de rotação do planeta em torno de seu próprio eixo (graus por segundo)")]
        public float minRotationSpeed = 10f;

        [SerializeField, Tooltip("Velocidade máxima de rotação do planeta em torno de seu próprio eixo (graus por segundo)")]
        public float maxRotationSpeed = 30f;
        [Tooltip("Centro da órbita (opcional, sobrescreve PlanetSpawnData.orbitCenter se definido)")]
        public Vector3? orbitCenter = null; // Opcional, para maior flexibilidade
    }
}