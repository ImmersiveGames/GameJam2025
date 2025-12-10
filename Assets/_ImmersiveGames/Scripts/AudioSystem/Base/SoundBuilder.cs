using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Core;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    /// <summary>
    /// Builder fluente para disparar SFX via serviço global de áudio (IAudioSfxService).
    /// Não é um MonoBehaviour; pode ser usado em qualquer contexto de código.
    /// </summary>
    public class SoundBuilder
    {
        private readonly IAudioSfxService _sfxService;
        private readonly SoundData _sound;

        private Vector3 _position = Vector3.zero;
        private bool _useSpatial = true;
        private float _volumeMultiplier = 1f;
        private float _volumeOverride = -1f;
        private float _fadeInSeconds = 0f;
        private bool _loop = false;

        private SoundBuilder(SoundData sound, IAudioSfxService sfxService)
        {
            _sound = sound;
            _sfxService = sfxService;
        }

        #region Factory

        /// <summary>
        /// Cria um SoundBuilder para o SoundData informado, resolvendo o serviço global
        /// de SFX (IAudioSfxService) via AudioSystemBootstrap/DependencyManager.
        /// </summary>
        public static SoundBuilder For(SoundData sound)
        {
            var service = ResolveSfxService();
            return new SoundBuilder(sound, service);
        }

        /// <summary>
        /// Cria um SoundBuilder usando explicitamente um IAudioSfxService fornecido.
        /// Útil para testes ou cenários onde você já tem o serviço em mãos.
        /// </summary>
        public static SoundBuilder For(SoundData sound, IAudioSfxService sfxService)
        {
            return new SoundBuilder(sound, sfxService);
        }

        #endregion

        #region Fluent configuration

        /// <summary>
        /// Define a posição 3D usada para tocar o som.
        /// </summary>
        public SoundBuilder AtPosition(Vector3 position)
        {
            _position = position;
            return this;
        }

        /// <summary>
        /// Define a posição usando o Transform informado.
        /// </summary>
        public SoundBuilder AtTransform(Transform transform)
        {
            if (transform != null)
            {
                _position = transform.position;
            }

            return this;
        }

        /// <summary>
        /// Define se o som deve usar spatialBlend (3D) ou não.
        /// </summary>
        public SoundBuilder WithSpatial(bool useSpatial)
        {
            _useSpatial = useSpatial;
            return this;
        }

        /// <summary>
        /// Marca o som como não espacial (2D).
        /// </summary>
        public SoundBuilder NonSpatial()
        {
            _useSpatial = false;
            return this;
        }

        /// <summary>
        /// Aplica um multiplicador de volume (ex.: 0.5f para metade, 2f para dobro).
        /// </summary>
        public SoundBuilder WithVolumeMultiplier(float multiplier)
        {
            _volumeMultiplier = Mathf.Max(0f, multiplier);
            return this;
        }

        /// <summary>
        /// Define um override de volume absoluto (0..1) para esta reprodução,
        /// ignorando o cálculo normal de camadas.
        /// </summary>
        public SoundBuilder WithVolumeOverride(float volume)
        {
            _volumeOverride = Mathf.Clamp01(volume);
            return this;
        }

        /// <summary>
        /// Define o tempo de fade-in em segundos.
        /// </summary>
        public SoundBuilder WithFadeIn(float seconds)
        {
            _fadeInSeconds = Mathf.Max(0f, seconds);
            return this;
        }

        /// <summary>
        /// Define se o som deve ser reproduzido em loop.
        /// </summary>
        public SoundBuilder AsLoop(bool loop = true)
        {
            _loop = loop;
            return this;
        }

        #endregion

        #region Execution

        /// <summary>
        /// Constrói o contexto e dispara a reprodução.
        /// Se AsLoop(true) foi chamado, usa PlayLoop; caso contrário, PlayOneShot.
        /// </summary>
        public IAudioHandle Play()
        {
            if (_sound == null || _sound.clip == null)
            {
                DebugUtility.LogWarning(
                    typeof(SoundBuilder),
                    "[SoundBuilder] SoundData inválido ao chamar Play().");
                return NullHandle.Instance;
            }

            if (_sfxService == null)
            {
                DebugUtility.LogWarning(
                    typeof(SoundBuilder),
                    $"[SoundBuilder] IAudioSfxService não disponível para tocar '{_sound.name}'.");
                return NullHandle.Instance;
            }

            var context = new AudioContext
            {
                position = _position,
                useSpatial = _useSpatial,
                volumeMultiplier = _volumeMultiplier,
                volumeOverride = _volumeOverride
            };

            return _loop
                ? _sfxService.PlayLoop(_sound, context, _fadeInSeconds)
                : _sfxService.PlayOneShot(_sound, context, _fadeInSeconds);
        }

        #endregion

        #region Service resolve

        private static IAudioSfxService ResolveSfxService()
        {
            AudioSystemBootstrap.EnsureAudioSystemInitialized();

            if (DependencyManager.Provider != null &&
                DependencyManager.Provider.TryGetGlobal(out IAudioSfxService service))
            {
                return service;
            }

            DebugUtility.LogWarning(
                typeof(SoundBuilder),
                "IAudioSfxService não pôde ser resolvido via DependencyManager.");
            return null;
        }

        #endregion

        #region NullHandle

        /// <summary>
        /// Handle nulo para chamadas de Play que não conseguem tocar som.
        /// Evita precisar checar null em todos os call sites.
        /// </summary>
        private sealed class NullHandle : IAudioHandle
        {
            public static readonly NullHandle Instance = new NullHandle();

            public bool IsPlaying => false;

            public void Stop(float fadeOutSeconds = 0f)
            {
                // Intencionalmente vazio.
            }
        }

        #endregion
    }
}
