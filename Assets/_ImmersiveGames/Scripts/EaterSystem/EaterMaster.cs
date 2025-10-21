using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
namespace _ImmersiveGames.Scripts.EaterSystem
{
    
    public sealed class EaterMaster: ActorMaster, IEaterActor
    {
        public IActor EaterActor => this;
    }
    public interface IEaterActor
    {
        IActor EaterActor { get; }
    }
}