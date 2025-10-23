using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado "Comendo" – o Eater consome o alvo atual e aplica mordidas periódicas.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    internal sealed class EaterEatingState : EaterBehaviorState
    {
        private const float BiteInterval = 1.2f;
        private float _biteTimer;

        public EaterEatingState(EaterBehaviorContext context) : base(context)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _biteTimer = 0f;
            Context.SetEating(true);
            DebugUtility.LogVerbose<EaterEatingState>("Entrando no estado Comendo.");
        }

        public override void Update()
        {
            base.Update();

            _biteTimer += Time.deltaTime;
            if (_biteTimer < BiteInterval)
            {
                return;
            }

            _biteTimer = 0f;
            if (Context.Target != null)
            {
                Context.Master.OnEventEaterBite(Context.Target);
            }
        }

        public override void OnExit()
        {
            DebugUtility.LogVerbose<EaterEatingState>("Saindo do estado Comendo.");
            bool changed = Context.SetEating(false);
            if (changed && Context.Target != null)
            {
                Context.Master.OnEventEndEatPlanet(Context.Target);
            }
        }
    }
}
