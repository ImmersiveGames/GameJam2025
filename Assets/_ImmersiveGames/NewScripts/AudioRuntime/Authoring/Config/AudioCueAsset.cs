using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
namespace _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config
{
    /// <summary>
    /// Classe base abstrata para configura��o de cue de �udio.
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
        /// Tenta selecionar um clip aleat�rio da lista, priorizando clips n�o-nulos.
        /// </summary>
        /// <param name="clip">O clip selecionado aleatoriamente ou o primeiro v�lido encontrado.</param>
        /// <returns>Verdadeiro se um clip v�lido foi encontrado.</returns>
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
        /// Valida a configura��o do cue em tempo de execu��o.
        /// </summary>
        /// <param name="reason">String que descreve o motivo da valida��o falhar, se aplic�vel.</param>
        /// <returns>Verdadeiro se a configura��o � v�lida para execu��o.</returns>
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

