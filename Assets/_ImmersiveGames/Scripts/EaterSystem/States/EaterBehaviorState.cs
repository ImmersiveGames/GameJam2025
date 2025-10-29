using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Classe base minimalista para todos os estados do Eater.
    /// Responsável apenas por registrar logs básicos de entrada e saída.
    /// </summary>
    internal abstract class EaterBehaviorState : IState
    {
        protected readonly EaterBehavior Behavior;
        protected readonly string StateName;

        protected EaterBehaviorState(EaterBehavior behavior, string stateName)
        {
            Behavior = behavior;
            StateName = stateName;
        }

        public virtual void Update()
        {
        }

        public virtual void FixedUpdate()
        {
        }

        public virtual void OnEnter()
        {
            DebugUtility.LogVerbose<EaterBehaviorState>($"Entrando no estado {StateName}.");
        }

        public virtual void OnExit()
        {
            DebugUtility.LogVerbose<EaterBehaviorState>($"Saindo do estado {StateName}.");
        }

        public virtual bool CanPerformAction(ActionType action)
        {
            return true;
        }

        public virtual bool IsGameActive()
        {
            return true;
        }

        public override string ToString()
        {
            return StateName;
        }
    }
}
