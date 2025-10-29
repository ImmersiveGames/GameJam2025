using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    internal sealed class EaterEatingState : EaterBehaviorState
    {
        public EaterEatingState(EaterBehavior behavior) : base(behavior, "EaterEatingState")
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            DebugUtility.LogVerbose<EaterEatingState>("Eater iniciou a fase de consumo do planeta.");
        }
    }
}
