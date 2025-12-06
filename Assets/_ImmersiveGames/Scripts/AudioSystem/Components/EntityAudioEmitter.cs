using UnityEngine;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    /// <summary>
    /// Emissor de áudio reutilizável por entidade que delega a reprodução para o serviço global
    /// de SFX. Componentes de gameplay devem injetar seus próprios <see cref="SoundData"/> e
    /// chamar os métodos públicos para reproduzir efeitos sonoros locais sem acoplar a pooling
    /// ou a objetos temporários.
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
        }

        /// <summary>
        /// Reproduz um efeito sonoro local delegando ao serviço global de SFX.
        /// </summary>
        public void Play(SoundData soundData, AudioContext ctx, float fadeInSeconds = 0f)
        {
            if (soundData == null || soundData.clip == null) return;

            if (_sfxService == null)
            {
                DebugUtility.LogWarning<EntityAudioEmitter>(
                    $"[{name}] IAudioSfxService não encontrado — áudio não será reproduzido.",
                    this);
                return;
            }

            _sfxService.PlayOneShot(soundData, ctx, fadeInSeconds);
        }

        /// <summary>
        /// Helper para reproduzir um som na própria posição do emissor, respeitando o uso de spatial blend padrão.
        /// </summary>
        public void PlayAtSelf(SoundData soundData, float fadeInSeconds = 0f)
        {
            var ctx = AudioContext.Default(transform.position, UsesSpatialBlend);
            Play(soundData, ctx, fadeInSeconds);
        }

        /// <summary>
        /// Método legado mantido para compatibilidade, mas agora delegando ao serviço global.
        /// </summary>
        public void Play(SoundData soundData, float fadeInSeconds = 0f)
        {
            var ctx = AudioContext.Default(transform.position, UsesSpatialBlend);
            Play(soundData, ctx, fadeInSeconds);
        }
    }
}
