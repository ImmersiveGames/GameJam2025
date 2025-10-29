using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    internal sealed class EaterWanderingState : EaterBehaviorState
    {
        public EaterWanderingState(EaterBehavior behavior) : base(behavior, "EaterWanderingState")
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            DebugUtility.LogVerbose<EaterWanderingState>("Eater está vagando pelo cenário.");
        }
    }
}
