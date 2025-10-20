using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.Scripts.AudioSystem.UI
{
    public class AudioSettingsUI : MonoBehaviour
    {
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;

        [Inject] private IAudioService _audioService;

        private void Awake()
        {
            // Garante que o AudioManager exista antes de solicitar dependências.
            AudioSystemInitializer.EnsureAudioSystemInitialized();
            DependencyManager.Instance.InjectDependencies(this);
        }

        private void Start()
        {
            if (_audioService == null)
            {
                Debug.LogError("IAudioService não encontrado após injeção!");
                return;
            }

            if (bgmSlider != null)
            {
                bgmSlider.onValueChanged.AddListener(SetBGMVolume);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.AddListener(SetSfxVolume);
            }
        }

        public void SetBGMVolume(float value)
        {
            _audioService?.SetBGMVolume(value);
        }

        public void SetSfxVolume(float value)
        {
           // _audioService?.SetSfxVolume(value);
        }
    }
}
