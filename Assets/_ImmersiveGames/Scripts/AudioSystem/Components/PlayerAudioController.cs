using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.DamageSystem;
using System;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    /// <summary>
    /// Controller de áudio por entidade (Player/NPC). Centraliza reprodução de sons.
    /// Agora usa pool local definido em AudioControllerBase (via inspector).
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
                _onDamageReceived = (_, _) => PlaySound(audioConfig?.hitSound);
                _onDeath = _ => PlaySound(audioConfig?.deathSound);
                _onRevive = _ => PlaySound(audioConfig?.reviveSound);

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
        /// Método de conveniência para tocar som de tiro.
        /// Se uma estratégia passar SoundData, o controlador irá tocar respeitando seu pool local e configuração.
        /// </summary>
        public void PlayShootSound(SoundData strategySound = null, float volumeMultiplier = 1f)
        {
            var sound = strategySound ?? audioConfig?.shootSound;
            if (sound == null) return;
            // PlaySound do base já tentará usar pool local
            PlaySound(sound, AudioContextMode.Auto, volumeMultiplier);
        }
    }
}
