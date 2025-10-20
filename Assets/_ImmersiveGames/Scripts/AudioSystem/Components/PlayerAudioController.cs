using System;
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
    /// Exponde <see cref="SoundData"/> serializáveis para permitir variação por entidade.
    /// </summary>
    public class PlayerAudioController : AudioControllerBase
    {
        [Serializable]
        private struct DamageTypeSound
        {
            public DamageType damageType;
            public SoundData sound;

            public bool Matches(DamageEvent evt)
            {
                return sound != null && damageType == evt.DamageType;
            }
        }

        [Header("Shoot Sound")]
        [SerializeField] private SoundData shootSoundOverride;

        [Header("Damage Feedback Sounds")]
        [SerializeField] private SoundData defaultHitSound;
        [SerializeField] private SoundData defaultDeathSound;
        [SerializeField] private SoundData defaultReviveSound;
        [SerializeField] private DamageTypeSound[] damageTypeOverrides = Array.Empty<DamageTypeSound>();
        [SerializeField] private bool useHitWorldPosition = true;
        [SerializeField, Range(0.1f, 5f)] private float hitVolumeMultiplier = 1f;

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

        public void PlayShootSound(SoundData strategySound = null, float volumeMultiplier = 1f)
        {
            var soundToPlay = ResolveShootSound(strategySound);
            if (soundToPlay == null)
            {
                DebugUtility.LogVerbose<PlayerAudioController>("Nenhum som de tiro configurado.", "yellow");
                return;
            }

            var context = AudioContext.Default(transform.position, ResolveSpatialUsage(), volumeMultiplier);
            PlaySoundLocal(soundToPlay, context);
        }

        // helpers para testes/debug
        public AudioConfig GetAudioConfig() => audioConfig;
        public SoundBuilder CreateSoundBuilderPublic() => CreateSoundBuilder();
        public void TestPlaySoundPublic(SoundData sound, float volMult = 1f)
        {
            if (sound == null) return;
            var context = AudioContext.Default(transform.position, ResolveSpatialUsage(), volMult);
            PlaySoundLocal(sound, context);
        }

        private void OnDamageEvent(DamageEvent evt)
        {
            if (_actor == null || evt.TargetId != _actor.ActorId)
            {
                return;
            }

            var sound = ResolveDamageSound(evt);
            if (sound == null)
            {
                return;
            }

            var position = ResolveHitPosition(evt);
            var context = AudioContext.Default(position, ResolveSpatialUsage(), hitVolumeMultiplier);
            PlaySoundLocal(sound, context);
        }

        private void OnDeathEvent(DeathEvent evt)
        {
            if (_actor == null || evt.EntityId != _actor.ActorId)
            {
                return;
            }

            var sound = ResolveDeathSound();
            if (sound == null)
            {
                return;
            }

            var context = AudioContext.Default(transform.position, ResolveSpatialUsage());
            PlaySoundLocal(sound, context);
        }

        private void OnReviveEvent(ReviveEvent evt)
        {
            if (_actor == null || evt.EntityId != _actor.ActorId)
            {
                return;
            }

            var sound = ResolveReviveSound();
            if (sound == null)
            {
                return;
            }

            var context = AudioContext.Default(transform.position, ResolveSpatialUsage());
            PlaySoundLocal(sound, context);
        }

        private SoundData ResolveShootSound(SoundData strategySound)
        {
            if (strategySound != null)
            {
                return strategySound;
            }

            if (shootSoundOverride != null)
            {
                return shootSoundOverride;
            }

            return audioConfig?.shootSound;
        }

        private SoundData ResolveDamageSound(DamageEvent evt)
        {
            foreach (var entry in damageTypeOverrides)
            {
                if (entry.Matches(evt))
                {
                    return entry.sound;
                }
            }

            if (defaultHitSound != null)
            {
                return defaultHitSound;
            }

            return audioConfig?.hitSound;
        }

        private SoundData ResolveDeathSound()
        {
            if (defaultDeathSound != null)
            {
                return defaultDeathSound;
            }

            return audioConfig?.deathSound;
        }

        private SoundData ResolveReviveSound()
        {
            if (defaultReviveSound != null)
            {
                return defaultReviveSound;
            }

            return audioConfig?.reviveSound;
        }

        private Vector3 ResolveHitPosition(DamageEvent evt)
        {
            if (!useHitWorldPosition)
            {
                return transform.position;
            }

            if (evt.HasHitPosition)
            {
                return evt.HitPosition;
            }

            return transform.position;
        }

        private bool ResolveSpatialUsage()
        {
            return audioConfig?.useSpatialBlend ?? true;
        }
    }
}
