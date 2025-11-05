using _ImmersiveGames.Scripts.StateMachineSystems;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado base sem regras enquanto o comportamento completo é reimplementado.
    /// </summary>
    internal abstract class EaterBehaviorState : IState
    {
        public virtual void Update()
        {
        }

        public virtual void FixedUpdate()
        {
        }

        public virtual void OnEnter()
        {
        }

        public virtual void OnExit()
        {
        }

        public virtual bool CanPerformAction(ActionType action)
        {
            return true;
        }

        public virtual bool IsGameActive()
        {
            return true;
        }
    }
}
