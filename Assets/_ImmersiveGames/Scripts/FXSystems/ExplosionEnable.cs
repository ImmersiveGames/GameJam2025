using System;
using UnityEngine;
namespace _ImmersiveGames.Scripts.FXSystems
{
    public class ExplosionEnable : MonoBehaviour
    {
        private ParticleSystem _particleSystem;

        private void Awake()
        {
           
        }

        private void OnEnable()
        {
            _particleSystem = GetComponentInChildren<ParticleSystem>();
    
            _particleSystem?.Play();
        }

        private void OnDisable()
        {
            if (_particleSystem && _particleSystem.isPlaying)
            {
                _particleSystem?.Stop();
            }
        }
    }
}