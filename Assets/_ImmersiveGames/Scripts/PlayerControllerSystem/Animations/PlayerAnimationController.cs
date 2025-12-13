using _ImmersiveGames.Scripts.AnimationSystems.Base;
using _ImmersiveGames.Scripts.AnimationSystems.Interfaces;
using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.GameplaySystems.Execution;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem.Animations
{
    
    public class PlayerAnimationController : AnimationControllerBase, IActorAnimationController,IExecutionToggleIgnored
    {
        private EventBinding<DamageEvent> _damageBinding;
        private EventBinding<DeathEvent> _deathBinding;
        private EventBinding<ReviveEvent> _reviveBinding;
        private bool _listenersRegistered;

        protected override void Awake()
        {
            base.Awake();

            if (!enabled || Actor == null)
            {
                return;
            }

            _damageBinding = new EventBinding<DamageEvent>(OnDamageEvent);
            _deathBinding = new EventBinding<DeathEvent>(OnDeathEvent);
            _reviveBinding = new EventBinding<ReviveEvent>(OnReviveEvent);
        }

        protected void OnEnable()
        {
            RegisterDamageListeners();
        }

        protected override void OnDisable()
        {
            UnregisterDamageListeners();
            base.OnDisable();
        }

        private void RegisterDamageListeners()
        {
            if (_listenersRegistered)
            {
                return;
            }

            if (Actor == null || string.IsNullOrEmpty(Actor.ActorId))
            {
                DebugUtility.LogWarning<PlayerAnimationController>(
                    "ActorId inválido. Eventos de animação não serão registrados.");
                return;
            }

            if (_damageBinding == null || _deathBinding == null || _reviveBinding == null)
            {
                DebugUtility.LogWarning<PlayerAnimationController>(
                    "Bindings de animação não foram inicializados corretamente.");
                return;
            }

            FilteredEventBus<DamageEvent>.Register(_damageBinding, Actor.ActorId);
            FilteredEventBus<DeathEvent>.Register(_deathBinding, Actor.ActorId);
            FilteredEventBus<ReviveEvent>.Register(_reviveBinding, Actor.ActorId);
            _listenersRegistered = true;

            DebugUtility.LogVerbose<PlayerAnimationController>(
                $"Eventos de dano registrados para {Actor.ActorId}.",
                DebugUtility.Colors.CrucialInfo);
        }

        private void UnregisterDamageListeners()
        {
            if (!_listenersRegistered || Actor == null || string.IsNullOrEmpty(Actor.ActorId))
            {
                _listenersRegistered = false;
                return;
            }

            if (_damageBinding != null)
            {
                FilteredEventBus<DamageEvent>.Unregister(_damageBinding, Actor.ActorId);
            }

            if (_deathBinding != null)
            {
                FilteredEventBus<DeathEvent>.Unregister(_deathBinding, Actor.ActorId);
            }

            if (_reviveBinding != null)
            {
                FilteredEventBus<ReviveEvent>.Unregister(_reviveBinding, Actor.ActorId);
            }
            _listenersRegistered = false;
        }

        private void OnDamageEvent(DamageEvent evt)
        {
            if (evt.targetId != Actor?.ActorId)
            {
                return;
            }

            DebugUtility.LogVerbose<PlayerAnimationController>("Recebido DamageEvent. Executando animação de Hit.");
            PlayHit();
        }

        private void OnDeathEvent(DeathEvent evt)
        {
            if (evt.entityId != Actor?.ActorId)
            {
                return;
            }

            DebugUtility.LogVerbose<PlayerAnimationController>("Recebido DeathEvent. Executando animação de Death.");
            PlayDeath();
        }

        private void OnReviveEvent(ReviveEvent evt)
        {
            if (evt.entityId != Actor?.ActorId)
            {
                return;
            }

            DebugUtility.LogVerbose<PlayerAnimationController>("Recebido ReviveEvent. Executando animação de Revive.");
            PlayRevive();
        }

        public void PlayHit() => PlayHash(HitHash);
        public void PlayDeath() => PlayHash(DeathHash);
        public void PlayRevive() => PlayHash(ReviveHash);
        public void PlayIdle() => PlayHash(IdleHash);
    }
}
