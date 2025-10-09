using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AnimationSystems.Base;
using _ImmersiveGames.Scripts.AnimationSystems.Interfaces;
using _ImmersiveGames.Scripts.DamageSystem;

namespace _ImmersiveGames.Scripts.AnimationSystems.Components
{
    public class PlayerAnimationController : AnimationControllerBase, IActorAnimationController
    {
        private DamageReceiver _damageReceiver;
        
        // Agora as hashs vêm da AnimationConfig através das propriedades herdadas

        protected override void Start()
        {
            base.Start();

            _damageReceiver = GetComponent<DamageReceiver>();
            if (_damageReceiver != null)
            {
                _damageReceiver.EventDamageReceived += EventHit;
                _damageReceiver.EventDeath += EventDeath;
                _damageReceiver.EventRevive += EventRevive;
            }
        }

        protected override void OnDisable()
        {
            if (_damageReceiver != null)
            {
                _damageReceiver.EventDamageReceived -= EventHit;
                _damageReceiver.EventDeath -= EventDeath;
                _damageReceiver.EventRevive -= EventRevive;
            }
            base.OnDisable();
        }

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