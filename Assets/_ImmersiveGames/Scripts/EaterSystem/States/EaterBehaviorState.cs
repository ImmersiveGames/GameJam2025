using _ImmersiveGames.Scripts.EaterSystem.Configs;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado base sem regras enquanto o comportamento completo é reimplementado.
    /// </summary>
    internal abstract class EaterBehaviorState : OldIState
    {
        protected EaterBehaviorState(string stateName)
        {
            StateName = stateName;
        }

        public string StateName { get; }

        protected Behavior.EaterBehavior Behavior { get; private set; }

        protected Transform Transform => Behavior != null ? Behavior.transform : null;

        protected EaterMaster Master => Behavior != null ? Behavior.Master : null;

        protected EaterConfigSo Config => Behavior != null ? Behavior.Config : null;

        internal void Attach(Behavior.EaterBehavior behavior)
        {
            Behavior = behavior;
        }

        public virtual void Update()
        {
        }

        public virtual void FixedUpdate()
        {
        }

        public virtual void OnEnter()
        {
            LogStateEvent("Entrou");
        }

        public virtual void OnExit()
        {
            LogStateEvent("Saiu");
        }

        public virtual bool CanPerformAction(OldActionType action)
        {
            return true;
        }

        public virtual bool IsGameActive()
        {
            return true;
        }

        private void LogStateEvent(string description)
        {
            if (Behavior == null || !Behavior.ShouldLogStateTransitions)
            {
                return;
            }

            string message = $"{description} no estado {StateName}.";
            DebugUtility.LogVerbose(
                message,
                DebugUtility.Colors.CrucialInfo,
                Behavior,
                Behavior);
        }
    }
}



