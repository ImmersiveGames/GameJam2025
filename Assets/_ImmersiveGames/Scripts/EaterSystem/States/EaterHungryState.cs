using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    internal sealed class EaterHungryState : EaterBehaviorState
    {
        public EaterHungryState(EaterBehavior behavior) : base(behavior, "EaterHungryState")
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            DebugUtility.LogVerbose<EaterHungryState>("Eater está com fome e aguardando um alvo.");
        }
    }
}
