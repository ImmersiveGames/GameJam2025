using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundEmitter: MonoBehaviour
    {
        public SoundData Data { get; private set; }
        AudioSource audioSource;
        Coroutine playingCoroutine;

        void Awake()
        {
            audioSource = gameObject.GetOrAdd<AudioSource>();
        }

        public void Play()
        {
            if(playingCoroutine != null) {
                StopCoroutine(playingCoroutine);
            }

            audioSource.Play();
            playingCoroutine = StartCoroutine(WaitForSoundToEnd());
        }

        IEnumerator WaitForSoundToEnd()
        {
            yield return new WaitWhile(() => audioSource.isPlaying);
            SoundManager.Instance.ReturnToPool(this);
        }

        public void Stop() 
        {
            if (playingCoroutine != null) 
            {
                StopCoroutine(playingCoroutine);
                playingCoroutine = null;
            }

            audioSource.Stop();
            SoundManager.Instance.ReturnToPool(this);
        }


        public void Initialize(SoundData data) 
        {
            Data = data;
            audioSource.clip = data.clip;
            audioSource.outputAudioMixerGroup = data.mixerGroup;
            audioSource.volume = data.volume;
            audioSource.priority = data.priority;
            audioSource.loop = data.loop;
            audioSource.playOnAwake = data.playOnAwake;
        }

        public void WithRandomPitch(float min = -0.05f, float max = 0.05f)
        {
            audioSource.pitch += Random.Range(min, max);
        }
    }
}