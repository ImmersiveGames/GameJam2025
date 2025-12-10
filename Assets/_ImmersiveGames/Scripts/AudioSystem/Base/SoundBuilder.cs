using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Core;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    /// <summary>
    /// SoundBuilder que delega reprodução para o serviço global de SFX.
    /// </summary>
    public class SoundBuilder
    {
        private readonly IAudioSfxService _sfxService;

        private SoundData _sound;
        private Vector3 _position = Vector3.zero;
        private bool _useSpatial = true;
        private bool _randomPitch;
        private float _volumeMultiplier = 1f;
        private float _fadeIn;

        public SoundBuilder() : this(ResolveSfxService())
        {
        }

        public SoundBuilder(IAudioSfxService sfxService)
        {
            _sfxService = sfxService;
        }

        public SoundBuilder WithSoundData(SoundData sd)
        {
            _sound = sd;
            return this;
        }

        public SoundBuilder AtPosition(Vector3 pos, bool spatial = true)
        {
            _position = pos;
            _useSpatial = spatial;
            return this;
        }

        public SoundBuilder WithRandomPitch(bool v = true)
        {
            _randomPitch = v;
            return this;
        }

        public SoundBuilder WithVolumeMultiplier(float m)
        {
            _volumeMultiplier = m;
            return this;
        }

        public SoundBuilder WithFadeIn(float seconds)
        {
            _fadeIn = seconds;
            return this;
        }

        public void Play()
        {
            if (_sound == null || _sound.clip == null)
            {
                DebugUtility.LogWarning(typeof(SoundBuilder), "SoundBuilder.Play chamado com SoundData inválido.");
                return;
            }

            if (_sfxService == null)
            {
                DebugUtility.LogWarning(typeof(SoundBuilder), "IAudioSfxService não encontrado. Som não será reproduzido.");
                return;
            }

            var context = AudioContext.Default(_position, _useSpatial, _volumeMultiplier);

            bool originalRandomPitch = _sound.randomPitch;
            if (_randomPitch && !_sound.randomPitch)
            {
                _sound.randomPitch = true;
            }

            _sfxService.PlayOneShot(_sound, context, _fadeIn);

            if (_sound.randomPitch == originalRandomPitch) return;
            if (_sound != null)
                _sound.randomPitch = originalRandomPitch;
        }

        private static IAudioSfxService ResolveSfxService()
        {
            AudioSystemBootstrap.EnsureAudioSystemInitialized();

            if (DependencyManager.Provider != null && DependencyManager.Provider.TryGetGlobal(out IAudioSfxService service))
            {
                return service;
            }

            DebugUtility.LogWarning(typeof(SoundBuilder), "IAudioSfxService não pôde ser resolvido via DependencyManager.");
            return null;
        }
    }
}
