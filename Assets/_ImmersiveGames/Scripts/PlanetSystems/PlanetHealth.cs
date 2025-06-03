using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.EventsBus;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PlanetHealth : HealthResource
    {
        public override void Deafeat(Vector3 position)
        {
            modelRoot.SetActive(false);
            // Dispara DeathEvent com a posição do objeto
            EventBus<DeathEvent>.Raise(new DeathEvent(position, gameObject));
            // Remove o planeta da lista de ativos e limpa targetToEater, se necessário
            var planet = GetComponent<Planets>();
            if (planet)
            {
                PlanetsManager.Instance.RemovePlanet(planet);
                DebugUtility.Log<PlanetHealth>($"Planeta {planet.name} destruído e removido de PlanetsManager.");
            }
            else
            {
                DebugUtility.LogWarning<PlanetHealth>($"Componente Planets não encontrado em {gameObject.name} ao tentar remover!", this);
            }
        }
    }
}