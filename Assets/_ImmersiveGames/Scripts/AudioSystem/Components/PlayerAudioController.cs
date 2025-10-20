using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AudioSystem.Base;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    /// <summary>
    /// Controla os efeitos sonoros associados ao ator jogador (dano, morte, revive e tiros).
    /// Mantém o registro no EventBus filtrado para responder ao novo DamageSystem.
    /// </summary>
    public class PlayerAudioController : AudioControllerBase
    {
        private ActorMaster _actor;
        private EventBinding<DamageEvent> _damageBinding;
        private EventBinding<DeathEvent> _deathBinding;
        private EventBinding<ReviveEvent> _reviveBinding;
        private bool _bindingsInitialized;
        private bool _listenersRegistered;

        protected override void Awake()
        {
            base.Awake();
            TryGetComponent(out _actor);
            InitializeBindings();
        }

        private void OnEnable()
        {
            RegisterDamageListeners();
        }

        private void OnDisable()
        {
            UnregisterDamageListeners();
        }

        private void OnDestroy()
        {
            UnregisterDamageListeners();
        }

        /// <summary>
        /// Configura os bindings apenas uma vez por instância para evitar alocações adicionais.
        /// </summary>
        private void InitializeBindings()
        {
            if (_bindingsInitialized)
            {
                return;
            }

            _damageBinding = new EventBinding<DamageEvent>(OnDamageEvent);
            _deathBinding = new EventBinding<DeathEvent>(OnDeathEvent);
            _reviveBinding = new EventBinding<ReviveEvent>(OnReviveEvent);
            _bindingsInitialized = true;
        }

        /// <summary>
        /// Registra o controlador no EventBus filtrado usando o ActorId fornecido pelo ActorMaster local.
        /// </summary>
        private void RegisterDamageListeners()
        {
            if (_listenersRegistered)
            {
                return;
            }

            if (_actor == null || string.IsNullOrEmpty(_actor.ActorId))
            {
                DebugUtility.LogWarning<PlayerAudioController>(
                    "ActorId inválido. Eventos de áudio não serão registrados.");
                return;
            }

            if (!_bindingsInitialized)
            {
                InitializeBindings();
            }

            FilteredEventBus<DamageEvent>.Register(_damageBinding, _actor.ActorId);
            FilteredEventBus<DeathEvent>.Register(_deathBinding, _actor.ActorId);
            FilteredEventBus<ReviveEvent>.Register(_reviveBinding, _actor.ActorId);
            _listenersRegistered = true;
        }

        /// <summary>
        /// Remove os listeners registrados quando o componente é desabilitado ou destruído.
        /// </summary>
        private void UnregisterDamageListeners()
        {
            if (!_listenersRegistered || _actor == null || string.IsNullOrEmpty(_actor.ActorId))
            {
                _listenersRegistered = false;
                return;
            }

            if (_damageBinding != null)
            {
                FilteredEventBus<DamageEvent>.Unregister(_damageBinding, _actor.ActorId);
            }

            if (_deathBinding != null)
            {
                FilteredEventBus<DeathEvent>.Unregister(_deathBinding, _actor.ActorId);
            }

            if (_reviveBinding != null)
            {
                FilteredEventBus<ReviveEvent>.Unregister(_reviveBinding, _actor.ActorId);
            }

            _listenersRegistered = false;
        }

        private void OnDamageEvent(DamageEvent evt)
        {
            if (_actor == null || evt.TargetId != _actor.ActorId)
            {
                return;
            }

            var hitPosition = evt.HitPosition != Vector3.zero ? evt.HitPosition : transform.position;
            PlayConfiguredSound(audioConfig?.hitSound, hitPosition);
        }

        private void OnDeathEvent(DeathEvent evt)
        {
            if (_actor == null || evt.EntityId != _actor.ActorId)
            {
                return;
            }

            PlayConfiguredSound(audioConfig?.deathSound, transform.position);
        }

        private void OnReviveEvent(ReviveEvent evt)
        {
            if (_actor == null || evt.EntityId != _actor.ActorId)
            {
                return;
            }

            PlayConfiguredSound(audioConfig?.reviveSound, transform.position);
        }

        private void PlayConfiguredSound(SoundData sound, Vector3 position, float volumeMultiplier = 1f)
        {
            if (sound == null)
            {
                return;
            }

            bool useSpatial = audioConfig?.useSpatialBlend ?? true;
            var ctx = AudioContext.Default(position, useSpatial, volumeMultiplier);
            PlaySoundLocal(sound, ctx);
        }

        public void PlayShootSound(SoundData strategySound = null, float volumeMultiplier = 1f)
        {
            var soundToPlay = strategySound ?? audioConfig?.shootSound;
            if (soundToPlay == null) return;
            PlayConfiguredSound(soundToPlay, transform.position, volumeMultiplier);
        }

        // helpers para testes/debug
        public AudioConfig GetAudioConfig() => audioConfig;
        public SoundBuilder CreateSoundBuilderPublic() => CreateSoundBuilder();
        public void TestPlaySoundPublic(SoundData sound, float volMult = 1f)
        {
            PlayConfiguredSound(sound, transform.position, volMult);
        }
    }
}
