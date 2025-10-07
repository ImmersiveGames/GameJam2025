using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.Scripts.AudioSystem.UI
{
    public class AudioSettingsUI : MonoBehaviour
    {
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;

        private IAudioService _audioService;

        private void Start()
        {
            if (!DependencyManager.Instance.TryGetGlobal(out _audioService))
            {
                Debug.LogError("AudioService não encontrado!");
                return;
            }

            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
            sfxSlider.onValueChanged.AddListener(SetSfxVolume);
        }

        public void SetBGMVolume(float value)
        {
            _audioService?.SetBGMVolume(value);
        }

        public void SetSfxVolume(float value)
        {
            _audioService?.SetSfxVolume(value);
        }
    }
}