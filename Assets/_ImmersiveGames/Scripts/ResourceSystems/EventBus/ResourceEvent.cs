using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems.EventBus
{
    // Evento genérico para recursos (cheio, esgotado, etc.)
    public class ResourceEvent: IEvent
    {
        public GameObject Source { get; } // Objeto que disparou o evento
        public ResourceType Type { get; } // Tipo do recurso
        public float Percentage { get; } // Porcentagem atual do recurso

        public ResourceEvent(GameObject source, ResourceType type, float percentage)
        {
            Source = source;
            Type = type;
            Percentage = percentage;
        }
    }
}