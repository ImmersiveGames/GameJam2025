using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AnimationSystems.Base;
using _ImmersiveGames.Scripts.AnimationSystems.Config;
using _ImmersiveGames.Scripts.AnimationSystems.Interfaces;
using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.EaterSystem.Configs;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem.Animations
{
    public class EaterAnimationController : AnimationControllerBase, IActorAnimationController
    {
        private DamageReceiver _damageReceiver;
        protected new EaterAnimationConfig animationConfig;
        
        // Agora as hashs vêm da AnimationConfig através das propriedades herdadas

        protected override void Start()
        {
            base.Start();

            _damageReceiver = GetComponent<DamageReceiver>();
            if (_damageReceiver != null)
            {
                /*_damageReceiver.EventDamageReceived += EventHit;
                _damageReceiver.EventDeath += EventDeath;
                _damageReceiver.EventRevive += EventRevive;*/
            }
        }

        protected override void OnDisable()
        {
            if (_damageReceiver != null)
            {
                /*_damageReceiver.EventDamageReceived -= EventHit;
                _damageReceiver.EventDeath -= EventDeath;
                _damageReceiver.EventRevive -= EventRevive;*/
            }
            base.OnDisable();
        }
        protected new int DeathHash => animationConfig?.DeathHash ?? Animator.StringToHash("Dead");
        protected int EatingHash => animationConfig?.EatingHash ?? Animator.StringToHash("isEating");
        protected int HappyHash => animationConfig?.HappyHash ?? Animator.StringToHash("Happy");
        protected int MadHash => animationConfig?.MadHash ?? Animator.StringToHash("Mad");
        
        

        private void EventHit(float dmg, IActor src) => PlayHit();
        private void EventDeath(IActor _) => PlayDeath();
        private void EventRevive(IActor _) => PlayRevive();

        // Implementação da interface usando as hashs da config
        public void PlayHit() => PlayHash(HitHash);
        public void PlayDeath() => PlayHash(DeathHash);
        public void PlayRevive() => PlayHash(ReviveHash);
        public void PlayIdle() => PlayHash(IdleHash);
    }
}