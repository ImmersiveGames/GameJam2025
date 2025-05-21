using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.EnemySystem;
using _ImmersiveGames.Scripts.ScriptableObjects;
using _ImmersiveGames.Scripts.StateMachine;
using _ImmersiveGames.Scripts.StateMachine.GameStates;
using UnityUtils;

namespace _ImmersiveGames.Scripts
{
    public class GameManager : Singleton<GameManager>
    {
        [SerializeField] private GameConfigSo gameConfig;
        
        private readonly List<Planets> _activePlanets = new List<Planets>();
        public string Score { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            // Inicializa o gerenciador de estados
            GameManagerStateMachine.Instance.InitializeStateMachine(this);
        }
        
       
        
        /// <summary>
        /// Inicia o jogo e faz o spawn dos planetas
        /// </summary>
        public void StartGame()
        {
            PlanetSpawner();
        }
        
        /// <summary>
        /// Spawna os planetas de acordo com a configuração do jogo
        /// </summary>
        private void PlanetSpawner()
        {
            // Limpa a lista de planetas ativos caso o jogo seja reiniciado
            foreach (var planeta in _activePlanets)
            {
                if (planeta != null)
                {
                    Destroy(planeta.gameObject);
                }
            }
            _activePlanets.Clear();
            
            if (!gameConfig)
            {
                Debug.LogError("Configurações de jogo ou de planeta não encontradas!");
                return;
            }
            
            // Posições já utilizadas para evitar sobreposição
            List<Vector3> usePositions = new List<Vector3>();
            
            // Spawna a quantidade de planetas configurada
            for (int i = 0; i < gameConfig.numPlanets; i++)
            {
                // Tenta encontrar uma posição válida para o planets
                Vector3 posicao = FindValidPosition(usePositions);
                usePositions.Add(posicao);
                
                // Instancia o planets
                GameObject objetoPlaneta = Instantiate(gameConfig.prefabPlaneta, posicao, Quaternion.identity);
                objetoPlaneta.name = $"Planeta_{i+1}";
                
                // Configura o componente Planets
                Planets planets = objetoPlaneta.GetComponent<Planets>();
                if (planets == null)
                {
                    planets = objetoPlaneta.AddComponent<Planets>();
                }
                
                // Inicializa o planets com as configurações do ScriptableObject
                planets.Initialize();
                
                // Adiciona à lista de planetas ativos
                _activePlanets.Add(planets);
                
                Debug.Log($"Planets {i+1} spawnado na posição: {posicao}");
            }
        }
        
        /// <summary>
        /// Encontra uma posição válida para o planeta, respeitando a distância mínima entre eles
        /// </summary>
        /// <param name="positionUsed">Lista de posições já utilizadas</param>
        /// <returns>Posição válida para o planeta</returns>
        private Vector3 FindValidPosition(List<Vector3> positionUsed)
        {
            const int maxTentativas = 100;
            const float alturaFixa = 1f; // Altura fixa para todos os planetas
        
            for (int tentativa = 0; tentativa < maxTentativas; tentativa++)
            {
                // Gera uma posição aleatória apenas nos eixos X e Z
                Vector3 validPosition = new Vector3(
                    Random.Range(gameConfig.limiteX.x, gameConfig.limiteX.y),
                    alturaFixa,
                    Random.Range(gameConfig.limiteZ.x, gameConfig.limiteZ.y)
                );
        
                // Verifica se a posição é válida (distante o suficiente de outras posições)
                bool positionValid = true;
                foreach (Vector3 inPosition in positionUsed)
                {
                    // Calcula a distância ignorando o eixo Y
                    float distanciaXZ = new Vector2(
                        validPosition.x - inPosition.x,
                        validPosition.z - inPosition.z
                    ).magnitude;
        
                    if (distanciaXZ < gameConfig.distanciaMinima)
                    {
                        positionValid = false;
                        break;
                    }
                }
        
                if (positionValid)
                {
                    return validPosition;
                }
            }
        
            // Fallback: retorna uma posição aleatória mantendo a altura fixa
            Debug.LogWarning("Não foi possível encontrar uma posição válida para o planeta. Usando posição aleatória.");
            return new Vector3(
                Random.Range(gameConfig.limiteX.x, gameConfig.limiteX.y),
                alturaFixa,
                Random.Range(gameConfig.limiteZ.x, gameConfig.limiteZ.y)
            );
        }
        
        /// <summary>
        /// Retorna a lista de planetas ativos no jogo
        /// </summary>
        public List<Planets> ObterPlanetasAtivos()
        {
            return _activePlanets;
        }

        internal bool CheckGameOver()
        {
            // Implementar lógica de game over
            return false;
        }

        internal bool CheckVictory()
        {
            // Implementar lógica de vitória
            return false;
        }
    }
}
