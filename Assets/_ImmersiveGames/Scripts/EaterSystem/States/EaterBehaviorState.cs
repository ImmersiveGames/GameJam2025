using _ImmersiveGames.Scripts.StatesMachines;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Classe base para estados da máquina de estados do Eater.
    /// Fornece operações comuns como controle de tempo, acesso ao contexto e métodos padrão.
    /// </summary>
    internal abstract class EaterBehaviorState : IState
    {
        protected readonly EaterBehaviorContext Context;
        protected readonly Transform Transform;
        protected readonly EaterConfigSo Config;

        protected EaterBehaviorState(EaterBehaviorContext context)
        {
            Context = context;
            Transform = context.Transform;
            Config = context.Config;
        }

        public virtual void Update()
        {
            Context.AdvanceStateTimer(Time.deltaTime);
        }

        public virtual void FixedUpdate()
        {
        }

        public virtual void OnEnter()
        {
            Context.ResetStateTimer();
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
