// Path: _ImmersiveGames/Scripts/AudioSystem/PlayerAudioController.cs
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AudioSystem.Base;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.PlayerControllerSystem;
using _ImmersiveGames.Scripts.PlayerControllerSystem.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    /// <summary>
    /// Controlador de áudio específico para o Player.
    /// Escuta eventos do DamageReceiver (hit, death, revive) e expõe métodos públicos
    /// para tocar sons específicos (ex: tiros vindos de InputSpawnerComponent).
    /// </summary>
    public class PlayerAudioController : AudioControllerBase
    {
        [Header("Damage System Audio Settings")]
        [SerializeField] private bool enableHitSound = true;
        [SerializeField] private bool enableDeathSound = true;
        [SerializeField] private bool enableReviveSound = true;

        [Header("Audio Customization")]
        [SerializeField] private float hitSoundVolumeMultiplier = 1f;
        [SerializeField] private float deathSoundVolumeMultiplier = 1f;
        [SerializeField] private float reviveSoundVolumeMultiplier = 1f;
        [SerializeField] private bool useSpatialAudio = true;

        private ActorMaster _actorMaster;
        private DamageReceiver _damageReceiver;
        private EventBinding<PlayerDiedEvent> _playerDiedEvent;

        protected override void Awake()
        {
            base.Awake();
            TryGetComponent(out _actorMaster);
            TryGetComponent(out _damageReceiver);

            // Registrar eventos do DamageReceiver local — se houver
            TryBindDamageReceiverEvents();
        }

        private void TryBindDamageReceiverEvents()
        {
            if (_damageReceiver == null) return;

            _damageReceiver.EventDamageReceived += OnDamageReceived;
            _damageReceiver.EventDeath += OnDeath;
            _damageReceiver.EventRevive += OnRevive;
        }

        private void OnDestroy()
        {
            if (_damageReceiver != null)
            {
                _damageReceiver.EventDamageReceived -= OnDamageReceived;
                _damageReceiver.EventDeath -= OnDeath;
                _damageReceiver.EventRevive -= OnRevive;
            }
        }

        private void OnEnable()
        {
            // Registrar eventos globais do player (legacy)
            _playerDiedEvent = new EventBinding<PlayerDiedEvent>(OnPlayerDiedEvent);
            EventBus<PlayerDiedEvent>.Register(_playerDiedEvent);
        }

        private void OnDisable()
        {
            EventBus<PlayerDiedEvent>.Unregister(_playerDiedEvent);
        }

        #region Damage System Audio Handlers
        private void OnDamageReceived(float damage, IActor source)
        {
            if (!enableHitSound || audioConfig?.hitSound == null) return;
            PlayDamageSound(audioConfig.hitSound, hitSoundVolumeMultiplier, "hit");
        }

        private void OnDeath(IActor actor)
        {
            if (!enableDeathSound || audioConfig?.deathSound == null) return;
            PlayDamageSound(audioConfig.deathSound, deathSoundVolumeMultiplier, "death");
        }

        private void OnRevive(IActor actor)
        {
            if (!enableReviveSound || audioConfig?.reviveSound == null) return;
            PlayDamageSound(audioConfig.reviveSound, reviveSoundVolumeMultiplier, "revive");
        }

        private void PlayDamageSound(SoundData soundData, float volumeMultiplier, string soundType)
        {
            if (soundData == null || !isInitialized) return;

            AudioContext ctx;
            if (useSpatialAudio || (audioConfig != null && audioConfig.useSpatialBlend))
            {
                ctx = new AudioContext
                {
                    Position = transform.position,
                    UseSpatial = true,
                    VolumeMultiplier = volumeMultiplier
                };
            }
            else
            {
                ctx = AudioContext.NonSpatial(volumeMultiplier);
            }

            // Usa o método unificado do AudioControllerBase
            Play(soundData, ctx);

            Debug.Log($"🔊 [AUDIO] {_actorMaster?.ActorName ?? gameObject.name} - {soundType} sound: {soundData.clip?.name}");
        }
        #endregion

        #region Shoot Sound: integração com InputSpawnerComponent
        /// <summary>
        /// Método público para tocar som de tiro — usado por InputSpawnerComponent.
        /// Sempre respeita o AudioConfig do Player e o contexto (spatial ou non-spatial).
        /// </summary>
        public void PlayCustomShootSound(SoundData customSound, float volumeMultiplier = 1f)
        {
            if (customSound == null || !isInitialized) return;

            AudioContext ctx;
            if (audioConfig != null ? audioConfig.useSpatialBlend : useSpatialAudio)
            {
                ctx = new AudioContext
                {
                    Position = transform.position,
                    UseSpatial = true,
                    VolumeMultiplier = volumeMultiplier
                };
            }
            else
            {
                ctx = AudioContext.NonSpatial(volumeMultiplier);
            }

            Play(customSound, ctx);

            Debug.Log($"🔫 [AUDIO] {_actorMaster?.ActorName ?? gameObject.name} - shoot sound: {customSound.clip?.name}");
        }
        #endregion

        #region Public Setters/Getters
        public void SetHitSoundEnabled(bool enabled) => enableHitSound = enabled;
        public void SetDeathSoundEnabled(bool enabled) => enableDeathSound = enabled;
        public void SetReviveSoundEnabled(bool enabled) => enableReviveSound = enabled;

        public void SetHitVolumeMultiplier(float multiplier) => hitSoundVolumeMultiplier = multiplier;
        public void SetDeathVolumeMultiplier(float multiplier) => deathSoundVolumeMultiplier = multiplier;
        public void SetReviveVolumeMultiplier(float multiplier) => reviveSoundVolumeMultiplier = multiplier;

        public bool IsHitSoundEnabled => enableHitSound;
        public bool IsDeathSoundEnabled => enableDeathSound;
        public bool IsReviveSoundEnabled => enableReviveSound;
        public bool IsAudioInitialized => isInitialized;
        #endregion

        #region Legacy global player event
        protected void OnPlayerDiedEvent(PlayerDiedEvent evt)
        {
            // Caso de compatibilidade — apenas repassa para PlayDamageSound padrão
            if (audioConfig?.deathSound != null)
                PlayDamageSound(audioConfig.deathSound, deathSoundVolumeMultiplier, "legacy death");
        }
        #endregion
    }
}
