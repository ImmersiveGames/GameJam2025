using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.EventsBus;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class PlanetHealth : HealthResource
    {
        public override void Deafeat(Vector3 position)
        {
            modelRoot.SetActive(false);
            // Dispara DeathEvent com a posição do objeto
            EventBus<DeathEvent>.Raise(new DeathEvent(position, gameObject));
            // Remove o planeta da lista de ativos e limpa targetToEater, se necessário
            Planets planet = GetComponent<Planets>();
            if (planet != null)
            {
                PlanetsManager.Instance.RemovePlanet(planet);
                Debug.Log($"Planeta {planet.name} destruído e removido de PlanetsManager.");
            }
            else
            {
                Debug.LogWarning($"Componente Planets não encontrado em {gameObject.name} ao tentar remover!", this);
            }
        }
    }
}