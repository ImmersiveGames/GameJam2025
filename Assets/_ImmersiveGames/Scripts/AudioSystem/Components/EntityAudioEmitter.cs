using UnityEngine;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Core;
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

        [Inject] private IAudioSfxService _sfxService;

        /// <summary>
        /// Configuração padrão de áudio usada por esta entidade (spatial, distância, etc.).
        /// </summary>
        public AudioConfig Defaults => defaults;

        /// <summary>
        /// Indica se, por padrão, este emissor utiliza spatial blend.
        /// </summary>
        public bool UsesSpatialBlend => defaults?.useSpatialBlend ?? true;

        private void Awake()
        {
            // Garante que o sistema de áudio global foi inicializado antes da injeção.
            AudioSystemBootstrap.EnsureAudioSystemInitialized();

            if (DependencyManager.Provider != null)
            {
                DependencyManager.Provider.InjectDependencies(this);

                // Fallback adicional (caso a configuração de injeção não esteja completa)
                if (_sfxService == null &&
                    !DependencyManager.Provider.TryGetGlobal(out _sfxService))
                {
                    DebugUtility.LogWarning<EntityAudioEmitter>(
                        $"[{name}] IAudioSfxService não pôde ser resolvido via DependencyManager.",
                        this);
                }
            }
            else
            {
                DebugUtility.LogWarning<EntityAudioEmitter>(
                    $"[{name}] DependencyManager.Provider indisponível. SFX não poderão ser reproduzidos.",
                    this);
            }

            if (_sfxService == null)
            {
                DebugUtility.LogWarning<EntityAudioEmitter>(
                    $"[{name}] IAudioSfxService não encontrado — nenhum SFX será reproduzido.",
                    this);
            }
        }

        /// <summary>
        /// Reproduz um efeito sonoro usando o serviço global de SFX, com um contexto já construído.
        /// </summary>
        public void Play(SoundData soundData, AudioContext ctx, float fadeInSeconds = 0f)
        {
            if (soundData == null || soundData.clip == null)
            {
                DebugUtility.LogWarning<EntityAudioEmitter>(
                    $"[{name}] SoundData inválido ao tentar reproduzir SFX.",
                    this);
                return;
            }

            if (_sfxService == null)
            {
                DebugUtility.LogWarning<EntityAudioEmitter>(
                    $"[{name}] IAudioSfxService não disponível — som '{soundData.name}' não será reproduzido.",
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
        /// Sobrecarga para compatibilidade legada: usa UsesSpatialBlend como padrão.
        /// </summary>
        public void Play(SoundData soundData, float fadeInSeconds = 0f)
        {
            var ctx = AudioContext.Default(transform.position, UsesSpatialBlend);
            Play(soundData, ctx, fadeInSeconds);
        }
    }
}
