using _ImmersiveGames.Scripts.StateMachineSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Classe base para estados da máquina de estados do Eater.
    /// Fornece operações comuns como controle de tempo, acesso às dependências principais e métodos padrão.
    /// </summary>
    internal abstract class EaterBehaviorState : IState
    {
        protected readonly EaterBehavior Behavior;
        protected readonly Transform Transform;
        protected readonly EaterConfigSo Config;

        protected EaterBehaviorState(EaterBehavior behavior)
        {
            Behavior = behavior;
            Transform = behavior != null ? behavior.transform : null;
            Config = behavior != null ? behavior.Config : null;
        }

        public virtual void Update()
        {
            Behavior?.AdvanceStateTimer(Time.deltaTime);
        }

        public virtual void FixedUpdate()
        {
        }

        public virtual void OnEnter()
        {
            Behavior?.ResetStateTimer();
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
