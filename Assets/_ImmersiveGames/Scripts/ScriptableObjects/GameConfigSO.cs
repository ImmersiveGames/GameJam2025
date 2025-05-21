using UnityEngine;
        
        namespace _ImmersiveGames.Scripts.ScriptableObjects
        {
            [CreateAssetMenu(fileName = "GameConfig", menuName = "ImmersiveGames/GameConfig", order = 0)]
            public class GameConfigSo : ScriptableObject
            {
                public int timerGame = 60;
                [Header("Configurações de Spawn")]
                [Tooltip("Quantidade de planetas a serem gerados")]
                [Range(1, 20)]
                public int numPlanets = 5;
                
                
                [Header("Área de Spawn (Plano XZ)")]
                [Tooltip("Limites X da área de spawn (min, max)")]
                public Vector2 limiteX = new Vector2(-20f, 20f);
                
                [Tooltip("Limites Z da área de spawn (min, max)")]
                public Vector2 limiteZ = new Vector2(-20f, 20f);
                
                [Tooltip("Distância mínima entre os planetas")]
                public float distanciaMinima = 5f;
            }
        }