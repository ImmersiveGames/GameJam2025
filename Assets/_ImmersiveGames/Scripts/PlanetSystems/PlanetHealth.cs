using System;
using _ImmersiveGames.Scripts.GameManagerSystems.EventsBus;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PlanetHealth : HealthResource
    {
        [SerializeField]
        private GameObject[] planetsParts;
        
        private EventBinding<PlanetCreatedEvent> _planetCreateBinding;

        protected override void OnEnable()
        {
            base.OnEnable();
            _planetCreateBinding = new EventBinding<PlanetCreatedEvent>(OnPlanetCreated);
            EventBus<PlanetCreatedEvent>.Register(_planetCreateBinding);
        }
        

        private void OnDisable()
        {
            EventBus<PlanetCreatedEvent>.Unregister(_planetCreateBinding);
        }
        public override void Deafeat(Vector3 position)
        {
            modelRoot.SetActive(false);
            // Dispara DeathEvent com a posição do objeto
            EventBus<DeathEvent>.Raise(new DeathEvent(position, gameObject));
            // Remove o planeta da lista de ativos e limpa targetToEater, se necessário
            var planet = GetComponent<PlanetsMaster>();
            if (planet)
            {
                PlanetsManager.Instance.RemovePlanet(planet);
                DebugUtility.Log<PlanetHealth>($"Planeta {planet.name} destruído e removido de PlanetsManager.");
            }
            else
            {
                DebugUtility.LogWarning<PlanetHealth>($"Componente PlanetsMaster não encontrado em {gameObject.name} ao tentar remover!", this);
            }
        }
        
        private void OnPlanetCreated(PlanetCreatedEvent obj)
        {
            Reset();
        }
        // Cura o recurso
        public override void Heal(float amount)
        {
            base.Heal(amount);
            //DestroyPlanetPiece(currentValue, maxValue);
        }

        // Causa dano ao recurso
        public override void TakeDamage(float damage)
        {
            Decrease(damage);
            //DestroyPlanetPiece(currentValue, maxValue);
        }
        public void DestroyPlanetPiece(float actualHealth, float maxHealth)
        {
            int total = planetsParts.Length;
            int parts = Mathf.CeilToInt((actualHealth / maxHealth) * total);

            for (int i = 0; i < total; i++)
            {
                planetsParts[i].SetActive(i < parts);
            }
        }
    }
}