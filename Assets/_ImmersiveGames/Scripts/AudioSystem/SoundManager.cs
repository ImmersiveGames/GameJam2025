using System.Collections.Generic;
using UnityEngine;
using UnityUtils;
using UnityEngine.Pool;
using UnityEngine.Audio;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    public class SoundManager : PersistentSingleton<SoundManager>
    {
        [Header("Audio Mixer Settings")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string bgmParameter = "BGM_Volume";
        [SerializeField] private string sfxParameter = "SFX_Volume";

        IObjectPool<SoundEmitter> soundEmitterPool;
        readonly List<SoundEmitter> activeSoundEmitters = new();
        public readonly Queue<SoundEmitter> FrequentSoundEmitters = new();

        [Header("Emitters Pool Settings")]
        [SerializeField] SoundEmitter soundEmitterPrefab;
        [SerializeField] bool collectionCheck = true;
        [SerializeField] int defaultCapacity = 10;
        [SerializeField] int maxPoolSize = 100;
        [SerializeField] int maxSoundInstances = 30;

        void Start()
        {
            InitializePool();
        }

        public SoundBuilder CreateSound() => new SoundBuilder(this);

        public bool CanPlaySound(SoundData data)
        {
            if (!data.frequentSound) return true;

            if (FrequentSoundEmitters.Count >= maxSoundInstances &&
                FrequentSoundEmitters.TryDequeue(out var soundEmitter))
            {
                try
                {
                    soundEmitter.Stop();
                    return true;
                }
                catch
                {
                    Debug.Log("SoundEmitter is already released");
                }
                return false;
            }
            return true;
        }

        public SoundEmitter Get() { return soundEmitterPool.Get(); }

        public void ReturnToPool(SoundEmitter soundEmitter)
        {
            soundEmitterPool.Release(soundEmitter);
        }

        void InitializePool()
        {
            soundEmitterPool = new ObjectPool<SoundEmitter>(
                CreateSoundEmitter,
                OnTakeFromPool,
                OnReturnedToPool,
                OnDestroyPoolObject,
                collectionCheck,
                defaultCapacity,
                maxPoolSize);
        }

        void OnTakeFromPool(SoundEmitter soundEmitter)
        {
            soundEmitter.gameObject.SetActive(true);
            activeSoundEmitters.Add(soundEmitter);
        }

        SoundEmitter CreateSoundEmitter()
        {
            var soundEmitter = Instantiate(soundEmitterPrefab);
            soundEmitter.gameObject.SetActive(false);
            return soundEmitter;
        }

        void OnReturnedToPool(SoundEmitter soundEmitter)
        {
            soundEmitter.gameObject.SetActive(false);
            activeSoundEmitters.Remove(soundEmitter);
        }

        void OnDestroyPoolObject(SoundEmitter soundEmitter)
        {
            Destroy(soundEmitter.gameObject);
        }

        public void SetBGMVolume(float volume)
        {
            audioMixer.SetFloat(bgmParameter, LinearToDecibel(volume));
        }

        /// Define o volume dos efeitos sonoros.
        public void SetSFXVolume(float volume)
        {
            audioMixer.SetFloat(sfxParameter, LinearToDecibel(volume));
        }

        /// Converte de escala linear (0-1) para decib�is.
        private float LinearToDecibel(float linear)
        {
            if (linear <= 0.0001f)
                return -80f; // Sil�ncio total
            return Mathf.Log10(linear) * 20;
        }

        /// Converte de decib�is para escala linear (0-1).
        private float DecibelToLinear(float dB)
        {
            return Mathf.Pow(10f, dB / 20f);
        }
    }
}