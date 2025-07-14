using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Mixer Parameters")]
    [SerializeField] private string bgmParameter = "BGM_Volume";
    [SerializeField] private string sfxParameter = "SFX_Volume";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Define o volume da música.
    /// </summary>
    /// <param name="volume">Valor de 0 a 1.</param>
    public void SetBGMVolume(float volume)
    {
        audioMixer.SetFloat(bgmParameter, LinearToDecibel(volume));
    }

    /// <summary>
    /// Define o volume dos efeitos sonoros.
    /// </summary>
    /// <param name="volume">Valor de 0 a 1.</param>
    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat(sfxParameter, LinearToDecibel(volume));
    }

    /// <summary>
    /// Converte de escala linear (0-1) para decibéis.
    /// </summary>
    private float LinearToDecibel(float linear)
    {
        if (linear <= 0.0001f)
            return -80f; // Silêncio total
        return Mathf.Log10(linear) * 20;
    }

    /// <summary>
    /// Converte de decibéis para escala linear (0-1).
    /// </summary>
    private float DecibelToLinear(float dB)
    {
        return Mathf.Pow(10f, dB / 20f);
    }
}
