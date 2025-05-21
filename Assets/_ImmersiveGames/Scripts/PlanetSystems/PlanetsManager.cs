using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.EnemySystem;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class PlanetsManager: MonoBehaviour
    {
        private readonly List<Planets> _activePlanets = new List<Planets>();
        [Tooltip("Prefab do planeta a ser instanciado"), SerializeField]
        private GameObject prefabPlanet;
        private int _numPlanets;
        private Vector2 _limitX;
        private Vector2 _limitZ;
        private float _minDistance;
        
        /// <summary>
        /// Spawna os planetas conforme a configuração do jogo
        /// </summary>
        private void PlanetSpawner(GameManager gameManager)
        {
            _numPlanets = gameManager.gameConfig.numPlanets;
            _limitX = gameManager.gameConfig.limiteX;
            _limitZ = gameManager.gameConfig.limiteZ;
            _minDistance = gameManager.gameConfig.distanciaMinima;
            
            
            // Limpa a lista de planetas ativos caso o jogo seja reiniciado
            foreach (var planeta in _activePlanets.Where(planeta => planeta))
            {
                Destroy(planeta.gameObject);
            }
            _activePlanets.Clear();
            
            // Posições já utilizadas para evitar sobreposição
            var usePositions = new List<Vector3>();
            
            // Spawna a quantidade de planetas configurada
            for (int i = 0; i < _numPlanets; i++)
            {
                // Tenta encontrar uma posição válida para o planets
                var validPosition = FindValidPosition(usePositions);
                usePositions.Add(validPosition);
                
                // Instancia o planets
                var objetoPlaneta = Instantiate(prefabPlanet, validPosition, Quaternion.identity);
                objetoPlaneta.name = $"Planeta_{i+1}";
                
                // Configura o componente Planets
                var planets = objetoPlaneta.GetComponent<Planets>();
                if (planets == null)
                {
                    planets = objetoPlaneta.AddComponent<Planets>();
                }
                
                // Inicializa o planets com as configurações do ScriptableObject
                planets.Initialize();
                
                // Adiciona à lista de planetas ativos
                _activePlanets.Add(planets);
                
                Debug.Log($"Planets {i+1} spawnado na posição: {validPosition}");
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
                    Random.Range(_limitX.x, _limitX.y),
                    alturaFixa,
                    Random.Range(_limitZ.x, _limitZ.y)
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
        
                    if (distanciaXZ < _minDistance)
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
                Random.Range(_limitX.x, _limitX.y),
                alturaFixa,
                Random.Range(_limitZ.x, _limitZ.y)
            );
        }
        
        /// <summary>
        /// Retorna a lista de planetas ativos no jogo
        /// </summary>
        public List<Planets> ObterPlanetasAtivos()
        {
            return _activePlanets;
        }
    }
}