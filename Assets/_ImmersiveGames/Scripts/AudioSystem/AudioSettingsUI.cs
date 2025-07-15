using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.Scripts.AudioSystem
{
    public class AudioSettingsUI : MonoBehaviour
    {
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;

        private void Start()
        {
            // Inicializar os sliders com algum valor salvo, se desejar.
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        public void SetBGMVolume(float value)
        {
            SoundManager.Instance.SetBGMVolume(value);
        }

        public void SetSFXVolume(float value)
        {
            SoundManager.Instance.SetSFXVolume(value);
        }
    }
}
