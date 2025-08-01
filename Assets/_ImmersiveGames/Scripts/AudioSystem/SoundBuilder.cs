﻿using System.Collections;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    public class SoundBuilder
    {
        readonly SoundManager soundManager;
        SoundData soundData;
        Vector3 position = Vector3.zero;
        bool randomPitch;

        public SoundBuilder(SoundManager soundManager) {
            this.soundManager = soundManager;
        }

        public SoundBuilder WithSoundData(SoundData soundData)
        {
            this.soundData = soundData;
            return this;
        }

        public SoundBuilder WithPosition(Vector3 position)
        {
            this.position = position;
            return this;
        }

        public SoundBuilder WithRandomPitch(bool randomPitch)
        {
            this.randomPitch = randomPitch;
            return this;
        }

        public void Play()
        {
            if (!soundManager.CanPlaySound(soundData)) return;

            SoundEmitter soundEmitter = soundManager.Get();
            soundEmitter.Initialize(soundData);
            soundEmitter.transform.position = position;
            soundEmitter.transform.parent = SoundManager.Instance.transform;

            if (randomPitch)
            {
                soundEmitter.WithRandomPitch();
            }

            if (soundData.frequentSound)
            {
                soundManager.FrequentSoundEmitters.Enqueue(soundEmitter);
            }

            soundEmitter.Play();
        }
    }
}