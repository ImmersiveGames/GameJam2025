using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    internal sealed class EaterIdleState : EaterBehaviorState
    {
        public EaterIdleState(EaterBehavior behavior) : base(behavior, "EaterIdleState")
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            DebugUtility.LogVerbose<EaterIdleState>("Eater parado aguardando comandos.");
        }
    }
}
