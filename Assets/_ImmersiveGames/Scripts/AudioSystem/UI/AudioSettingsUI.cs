using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.AudioSystem.Services;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Core.Composition;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.Scripts.AudioSystem.UI
{
    /// <summary>
    /// UI de configurações de áudio (Opções).
    /// Responsável por controlar os sliders de BGM e SFX e encaminhar as
    /// alterações para os serviços globais de áudio / configurações.
    /// </summary>
    public class AudioSettingsUI : MonoBehaviour
    {
        [Header("Sliders de Volume")]
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;

        [Inject] private IBgmAudioService _bgmAudioService;
        [Inject] private AudioServiceSettings _audioSettings;

        private void Awake()
        {
            // Garante que o sistema de áudio foi inicializado antes de resolver dependências.
            Core.AudioSystemBootstrap.EnsureAudioSystemInitialized();

            if (DependencyManager.Provider != null)
            {
                DependencyManager.Provider.InjectDependencies(this);

                // Fallback explícito caso a injeção não esteja devidamente configurada no container.
                if (_bgmAudioService == null &&
                    !DependencyManager.Provider.TryGetGlobal(out _bgmAudioService))
                {
                    DebugUtility.LogWarning<AudioSettingsUI>(
                        "IBgmAudioService não pôde ser resolvido via DependencyManager.",
                        this);
                }

                if (_audioSettings == null &&
                    !DependencyManager.Provider.TryGetGlobal(out _audioSettings))
                {
                    DebugUtility.LogWarning<AudioSettingsUI>(
                        "AudioServiceSettings não pôde ser resolvido via DependencyManager.",
                        this);
                }
            }
            else
            {
                DebugUtility.LogWarning<AudioSettingsUI>(
                    "DependencyManager.Provider indisponível. Sliders não terão efeito nos serviços globais.",
                    this);
            }

            if (_bgmAudioService == null)
            {
                DebugUtility.LogWarning<AudioSettingsUI>(
                    "IBgmAudioService não encontrado — o slider de BGM não terá efeito sonoro.",
                    this);
            }

            if (_audioSettings == null)
            {
                DebugUtility.LogWarning<AudioSettingsUI>(
                    "AudioServiceSettings não encontrado — os sliders não poderão persistir o volume global.",
                    this);
            }
        }

        private void OnEnable()
        {
            // Vincula listeners
            if (bgmSlider != null)
            {
                bgmSlider.onValueChanged.RemoveListener(SetBGMVolume);
                bgmSlider.onValueChanged.AddListener(SetBGMVolume);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveListener(SetSfxVolume);
                sfxSlider.onValueChanged.AddListener(SetSfxVolume);
            }

            // Sincroniza valores iniciais com as configurações globais
            SyncSlidersFromSettings();
        }

        private void OnDisable()
        {
            if (bgmSlider != null)
            {
                bgmSlider.onValueChanged.RemoveListener(SetBGMVolume);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveListener(SetSfxVolume);
            }
        }

        /// <summary>
        /// Atualiza o valor inicial dos sliders com base nas configurações globais.
        /// Útil para quando o jogador abre o menu de opções no meio do jogo.
        /// </summary>
        private void SyncSlidersFromSettings()
        {
            if (_audioSettings == null)
                return;

            if (bgmSlider != null)
            {
                bgmSlider.value = _audioSettings.bgmVolume;
            }

            if (sfxSlider != null)
            {
                sfxSlider.value = _audioSettings.sfxVolume;
            }
        }

        /// <summary>
        /// Chamado pelo slider de BGM. Atualiza o volume de música (BGM) no serviço
        /// global e persiste o valor em <see cref="AudioServiceSettings"/>.
        /// </summary>
        public void SetBGMVolume(float value)
        {
            // Persiste nas configurações globais (usadas no cálculo de volume).
            if (_audioSettings != null)
            {
                _audioSettings.bgmVolume = value;
            }

            // Encaminha para o serviço de BGM ajustar o AudioSource / mixer.
            if (_bgmAudioService != null)
            {
                _bgmAudioService.SetBGMVolume(value);
            }
            else
            {
                DebugUtility.LogWarning<AudioSettingsUI>(
                    $"SetBGMVolume chamado com valor {value}, mas IBgmAudioService é nulo.",
                    this);
            }
        }

        /// <summary>
        /// Chamado pelo slider de SFX. Atualiza o volume de efeitos em
        /// <see cref="AudioServiceSettings"/>; o <see cref="AudioSfxService"/> utiliza
        /// este valor ao calcular o volume final dos sons.
        /// </summary>
        public void SetSfxVolume(float value)
        {
            if (_audioSettings == null)
            {
                DebugUtility.LogWarning<AudioSettingsUI>(
                    $"SetSfxVolume chamado com valor {value}, mas AudioServiceSettings é nulo.",
                    this);
                return;
            }

            _audioSettings.sfxVolume = value;
            // Não é necessário chamar nenhum serviço diretamente:
            // o AudioSfxService lê AudioServiceSettings a cada reprodução de SFX
            // via AudioVolumeService / AudioMathService, aplicando o novo valor.
        }
    }
}

