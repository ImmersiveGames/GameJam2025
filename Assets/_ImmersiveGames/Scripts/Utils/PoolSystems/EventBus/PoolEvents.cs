using _ImmersiveGames.Scripts.Utils.BusEventSystems;
namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public class PoolExhaustedEvent : IEvent
    {
        public string PoolKey { get; }
        public PoolExhaustedEvent(string poolKey) => PoolKey = poolKey;
    }
    
    public class PoolRestoredEvent : IEvent
    {
        public string PoolKey { get; }
        public PoolRestoredEvent(string poolKey) => PoolKey = poolKey;
    }
}