using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    internal sealed class EaterChasingState : EaterBehaviorState
    {
        public EaterChasingState(EaterBehavior behavior) : base(behavior, "EaterChasingState")
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            DebugUtility.LogVerbose<EaterChasingState>("Eater está perseguindo seu alvo atual.");
        }
    }
}
