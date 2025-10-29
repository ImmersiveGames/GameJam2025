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
            DebugUtility.Log<EaterBehaviorState>(
                $"[{GetBehaviorName()}] Entrando no estado {StateName}.",
                context: Behavior);
        }

        public virtual void OnExit()
        {
            DebugUtility.Log<EaterBehaviorState>(
                $"[{GetBehaviorName()}] Saindo do estado {StateName}.",
                context: Behavior);
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

        private string GetBehaviorName()
        {
            return Behavior != null ? Behavior.name : "Eater";
        }
    }
}
