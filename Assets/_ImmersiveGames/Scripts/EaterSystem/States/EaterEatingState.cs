using _ImmersiveGames.Scripts.PlanetSystems;
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

        public EaterEatingState(EaterBehavior behavior) : base(behavior)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _biteTimer = 0f;
            Behavior?.SetEating(true);
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
            PlanetsMaster target = Behavior != null ? Behavior.CurrentTarget : null;
            if (target != null)
            {
                Behavior?.Master.OnEventEaterBite(target);
            }
        }

        public override void OnExit()
        {
            DebugUtility.LogVerbose<EaterEatingState>("Saindo do estado Comendo.");
            bool changed = Behavior != null && Behavior.SetEating(false);
            PlanetsMaster target = Behavior != null ? Behavior.CurrentTarget : null;
            if (changed && target != null)
            {
                Behavior?.Master.OnEventEndEatPlanet(target);
            }
        }
    }
}
