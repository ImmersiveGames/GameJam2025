using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AudioSystem.Base;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.DamageSystem;
using System;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    /// <summary>
    /// Controller de áudio por entidade (Player/NPC). Centraliza reprodução de sons.
    /// </summary>
    public class PlayerAudioController : AudioControllerBase
    {
        private DamageReceiver _damageReceiver;

        // Handlers armazenados para unsubscribe correto (importante)
        private Action<float, IActor> _onDamageReceived;
        private Action<IActor> _onDeath;
        private Action<IActor> _onRevive;

        protected override void Awake()
        {
            base.Awake();
            TryGetComponent(out _damageReceiver);

            if (_damageReceiver != null)
            {
                _onDamageReceived = (damage, source) => PlaySound(audioConfig?.hitSound);
                _onDeath = actor => PlaySound(audioConfig?.deathSound);
                _onRevive = actor => PlaySound(audioConfig?.reviveSound);

                _damageReceiver.EventDamageReceived += _onDamageReceived;
                _damageReceiver.EventDeath += _onDeath;
                _damageReceiver.EventRevive += _onRevive;
            }
        }

        private void OnDestroy()
        {
            if (_damageReceiver == null) return;
            _damageReceiver.EventDamageReceived -= _onDamageReceived;
            _damageReceiver.EventDeath -= _onDeath;
            _damageReceiver.EventRevive -= _onRevive;
        }

        /// <summary>
        /// Método específico de conveniência para tiros: usa SoundData vindo da estratégia (se tiver) ou do config.
        /// </summary>
        public void PlayShootSound(SoundData strategySound = null, float volumeMultiplier = 1f)
        {
            var soundToPlay = strategySound ?? audioConfig?.shootSound;
            if (soundToPlay == null) return;
            PlaySound(soundToPlay, AudioContextMode.Auto, volumeMultiplier);
        }
    }
}
