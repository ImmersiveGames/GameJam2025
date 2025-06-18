using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.BusEventSystems {
    public interface IEvent { }
    public interface ILocationEvent : IEvent
    {
        Vector3 Position { get; }
        GameObject GameObject { get; }
    }
}