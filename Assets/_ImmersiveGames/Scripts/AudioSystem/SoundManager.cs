using System;
using System.Collections.Generic;
using UnityEngine;
using UnityUtils;
using UnityEngine.Pool;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    public class SoundManager : PersistentSingleton<SoundManager>
    {
        IObjectPool<SoundEmitter> soundEmitterPool;
        readonly List<SoundEmitter> activeSoundEmitters = new();
        public readonly Dictionary<SoundData, int> Counts = new();

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
            return !Counts.TryGetValue(data, out var count) || count < maxSoundInstances;
        }

        public SoundEmitter Get() {
            return soundEmitterPool.Get();
        }

        public void ReturnToPool(SoundEmitter soundEmitter) {
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
            if (Counts.TryGetValue(soundEmitter.Data, out var count))
            {
                Counts[soundEmitter.Data] -= count > 0 ? 1 : 0;
            }

            soundEmitter.gameObject.SetActive(false);
            activeSoundEmitters.Remove(soundEmitter);
        }

        void OnDestroyPoolObject(SoundEmitter soundEmitter)
        {
            Destroy(soundEmitter.gameObject);
        }
    }
}