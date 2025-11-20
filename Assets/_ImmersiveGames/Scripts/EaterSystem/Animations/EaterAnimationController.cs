using _ImmersiveGames.Scripts.AnimationSystems.Base;
using _ImmersiveGames.Scripts.AnimationSystems.Interfaces;
using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.EaterSystem.Configs;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem.Animations
{
    
    public class EaterAnimationController : AnimationControllerBase, IActorAnimationController
    {
        private EaterAnimationConfig EaterAnimationConfig => animationConfig as EaterAnimationConfig;
        private EventBinding<DamageEvent> _damageBinding;
        private EventBinding<DeathEvent> _deathBinding;
        private EventBinding<ReviveEvent> _reviveBinding;
        private EaterMaster _eaterMaster;
        private bool _listenersRegistered;
        private bool _biteListenerRegistered;

        protected override void Awake()
        {
            base.Awake();

            if (!enabled || Actor == null)
            {
                return;
            }

            TryResolveEaterMaster();
            _damageBinding = new EventBinding<DamageEvent>(OnDamageEvent);
            _deathBinding = new EventBinding<DeathEvent>(OnDeathEvent);
            _reviveBinding = new EventBinding<ReviveEvent>(OnReviveEvent);
        }

        protected void OnEnable()
        {
            RegisterDamageListeners();
            RegisterBiteListener();
        }

        protected override void OnDisable()
        {
            UnregisterBiteListener();
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
                DebugUtility.LogWarning<EaterAnimationController>(
                    "ActorId inválido. Eventos de animação não serão registrados.");
                return;
            }

            if (_damageBinding == null || _deathBinding == null || _reviveBinding == null)
            {
                DebugUtility.LogWarning<EaterAnimationController>(
                    "Bindings de animação não foram inicializados corretamente.");
                return;
            }

            FilteredEventBus<DamageEvent>.Register(_damageBinding, Actor.ActorId);
            FilteredEventBus<DeathEvent>.Register(_deathBinding, Actor.ActorId);
            FilteredEventBus<ReviveEvent>.Register(_reviveBinding, Actor.ActorId);
            _listenersRegistered = true;

            DebugUtility.Log<EaterAnimationController>(
                $"Eventos de dano registrados para {Actor.ActorId}.",
                DebugUtility.Colors.CrucialInfo);
        }
        protected new int DeathHash => EaterAnimationConfig?.DeathHash ?? Animator.StringToHash("Dead");
        protected int EatingHash => EaterAnimationConfig?.EatingHash ?? Animator.StringToHash("isEating");
        protected int HappyHash => EaterAnimationConfig?.HappyHash ?? Animator.StringToHash("Happy");
        protected int MadHash => EaterAnimationConfig?.MadHash ?? Animator.StringToHash("Mad");
        
        

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
            if (evt.TargetId != Actor?.ActorId)
            {
                return;
            }

            DebugUtility.LogVerbose<EaterAnimationController>("Recebido DamageEvent. Executando animação de Hit.");
            PlayHit();
        }

        private void RegisterBiteListener()
        {
            if (_biteListenerRegistered)
            {
                return;
            }

            if (!TryResolveEaterMaster())
            {
                return;
            }

            _eaterMaster.EventEaterBite += OnEaterBite;
            _biteListenerRegistered = true;
        }

        private void UnregisterBiteListener()
        {
            if (!_biteListenerRegistered || _eaterMaster == null)
            {
                _biteListenerRegistered = false;
                return;
            }

            _eaterMaster.EventEaterBite -= OnEaterBite;
            _biteListenerRegistered = false;
        }

        private void OnDeathEvent(DeathEvent evt)
        {
            if (evt.EntityId != Actor?.ActorId)
            {
                return;
            }

            DebugUtility.LogVerbose<EaterAnimationController>("Recebido DeathEvent. Executando animação de Death.");
            PlayDeath();
        }

        private void OnReviveEvent(ReviveEvent evt)
        {
            if (evt.EntityId != Actor?.ActorId)
            {
                return;
            }

            DebugUtility.LogVerbose<EaterAnimationController>("Recebido ReviveEvent. Executando animação de Revive.");
            PlayRevive();
        }

        public void PlayHit() => PlayHash(HitHash);
        public void PlayDeath() => PlayHash(DeathHash);
        public void PlayRevive() => PlayHash(ReviveHash);
        public void PlayIdle() => PlayHash(IdleHash);

        public void SetEating(bool isEating)
        {
            if (animator == null || !gameObject.activeInHierarchy)
            {
                return;
            }

            animator.SetBool(EatingHash, isEating);
        }

        private void OnEaterBite(PlanetsMaster planet)
        {
            DebugUtility.LogVerbose<EaterAnimationController>(
                "Recebido evento de mordida. Mantendo animação de Eating ativa.");
            SetEating(true);
        }

        private bool TryResolveEaterMaster()
        {
            if (_eaterMaster != null)
            {
                return true;
            }

            if (Actor is EaterMaster eaterActor)
            {
                _eaterMaster = eaterActor;
                return true;
            }

            if (Actor != null
                && DependencyManager.Provider.TryGetForObject(Actor.ActorId, out EaterMaster resolvedMaster))
            {
                _eaterMaster = resolvedMaster;
                return true;
            }

            if (TryGetComponent(out resolvedMaster))
            {
                _eaterMaster = resolvedMaster;
                return true;
            }

            DebugUtility.LogWarning<EaterAnimationController>(
                "EaterMaster não encontrado. Evento de mordida não será ouvido.");
            return false;
        }
    }
}
