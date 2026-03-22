using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Config
{
    /// <summary>
    /// Classe base abstrata para configuração de cue de áudio.
    /// Define propriedades compartilhadas entre diferentes tipos de cues (BGM, SFX, etc).
    /// </summary>
    public abstract class AudioCueAsset : ScriptableObject
    {
        [SerializeField] private List<AudioClip> clips = new();
        [SerializeField] private AudioMixerGroup mixerGroup;
        [SerializeField] [Range(0f, 1f)] private float baseVolume = 1f;
        [SerializeField] private bool loop;
        [SerializeField] private float pitchMin = 1f;
        [SerializeField] private float pitchMax = 1f;
        [SerializeField] [Range(0f, 1f)] private float randomVolumeJitter;

        public IReadOnlyList<AudioClip> Clips => clips;
        public AudioMixerGroup MixerGroup => mixerGroup;
        public float BaseVolume => baseVolume;
        public bool Loop => loop;
        public float PitchMin => pitchMin;
        public float PitchMax => pitchMax;
        public float RandomVolumeJitter => randomVolumeJitter;

        /// <summary>
        /// Tenta selecionar um clip aleatório da lista, priorizando clips não-nulos.
        /// </summary>
        /// <param name="clip">O clip selecionado aleatoriamente ou o primeiro válido encontrado.</param>
        /// <returns>Verdadeiro se um clip válido foi encontrado.</returns>
        public bool TryPickClip(out AudioClip clip)
        {
            clip = null;

            if (clips == null || clips.Count == 0)
            {
                return false;
            }

            var validClips = clips.Where(t => t != null).ToList();
            if (validClips.Count == 0)
            {
                return false;
            }

            int index = Random.Range(0, validClips.Count);
            clip = validClips[index];
            return true;
        }


        /// <summary>
        /// Valida a configuração do cue em tempo de execução.
        /// </summary>
        /// <param name="reason">String que descreve o motivo da validação falhar, se aplicável.</param>
        /// <returns>Verdadeiro se a configuração é válida para execução.</returns>
        public bool ValidateRuntime(out string reason)
        {
            if (clips == null || clips.Count == 0)
            {
                reason = "missing_clips";
                return false;
            }

            if (baseVolume is < 0f or > 1f)
            {
                reason = "invalid_base_volume";
                return false;
            }

            if (pitchMin > pitchMax)
            {
                reason = "invalid_pitch_range";
                return false;
            }

            reason = null;
            return true;
        }
    }
}
