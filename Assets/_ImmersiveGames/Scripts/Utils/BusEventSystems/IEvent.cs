using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.BusEventSystems {
    public interface IEvent { }
    public interface ISpawnEvent : IEvent
    {
        Vector3 Position { get; }
        GameObject GameObject { get; }
    }
}