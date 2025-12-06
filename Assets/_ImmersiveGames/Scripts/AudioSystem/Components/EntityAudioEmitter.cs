using UnityEngine;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    /// <summary>
    /// Emissor de áudio reutilizável por entidade. Encaminha a reprodução de SFX para o
    /// serviço global de áudio (IAudioSfxService), mantendo a configuração padrão
    /// definida via <see cref="AudioConfig"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class EntityAudioEmitter : MonoBehaviour
    {
        [Header("Defaults")]
        [SerializeField] private AudioConfig defaults;

        private IAudioSfxService _sfxService;

        public AudioConfig Defaults => defaults;
        public bool UsesSpatialBlend => defaults?.useSpatialBlend ?? true;

        private void Awake()
        {
            AudioSystemInitializer.EnsureAudioSystemInitialized();

            if (DependencyManager.Provider != null)
            {
                DependencyManager.Provider.TryGetGlobal(out _sfxService);
            }

            if (_sfxService == null)
            {
                DebugUtility.LogWarning<EntityAudioEmitter>(
                    $"[{name}] IAudioSfxService não encontrado — nenhum SFX será reproduzido.",
                    this);
            }
        }

        /// <summary>
        /// Reproduz um efeito sonoro local usando o serviço global de SFX.
        /// </summary>
        public void Play(SoundData soundData, AudioContext ctx, float fadeInSeconds = 0f)
        {
            if (soundData == null || soundData.clip == null)
            {
                DebugUtility.LogWarning<EntityAudioEmitter>(
                    $"[{name}] SoundData inválido ao reproduzir SFX.",
                    this);
                return;
            }

            if (_sfxService == null)
            {
                DebugUtility.LogWarning<EntityAudioEmitter>(
                    $"[{name}] IAudioSfxService não disponível — som não será reproduzido.",
                    this);
                return;
            }

            _sfxService.PlayOneShot(soundData, ctx, fadeInSeconds);
        }

        /// <summary>
        /// Helper para reproduzir um som na posição do emissor respeitando as configurações padrão.
        /// </summary>
        public void PlayAtSelf(SoundData soundData, float fadeInSeconds = 0f)
        {
            var useSpatial = defaults?.useSpatialBlend ?? true;
            var ctx = AudioContext.Default(transform.position, useSpatial);
            Play(soundData, ctx, fadeInSeconds);
        }

        /// <summary>
        /// Sobrecarga para compatibilidade legada.
        /// </summary>
        public void Play(SoundData soundData, float fadeInSeconds = 0f)
        {
            var ctx = AudioContext.Default(transform.position, UsesSpatialBlend);
            Play(soundData, ctx, fadeInSeconds);
        }
    }
}
