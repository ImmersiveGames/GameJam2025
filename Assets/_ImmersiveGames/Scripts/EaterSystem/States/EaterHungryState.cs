using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado "Com Fome" – o Eater procura por alvos enquanto sente fome.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    internal sealed class EaterHungryState : EaterMoveState
    {
        public EaterHungryState(EaterBehaviorContext context) : base(context)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Context.SetHungry(true);
            DebugUtility.LogVerbose<EaterHungryState>("Entrando no estado Com Fome.");
        }

        public override void OnExit()
        {
            DebugUtility.LogVerbose<EaterHungryState>("Saindo do estado Com Fome.");
        }

        protected override float EvaluateSpeed()
        {
            float baseSpeed = Mathf.Max(Config.MaxSpeed, Config.MinSpeed);
            return baseSpeed * 0.75f;
        }

        protected override float GetDirectionInterval()
        {
            return Mathf.Max(Config.DirectionChangeInterval * 0.5f, 0.1f);
        }
    }
}
