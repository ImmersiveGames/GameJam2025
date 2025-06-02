using UnityEngine;
namespace _ImmersiveGames.Scripts.FXSystems
{
    public class ExplosionEnable : MonoBehaviour
    {
        private ParticleSystem _particleSystem;

        private void OnEnable()
        {
            _particleSystem = GetComponentInChildren<ParticleSystem>();
            _particleSystem?.gameObject.SetActive(true);
            _particleSystem?.Play();
        }

        private void OnDisable()
        {
            if (!_particleSystem || !_particleSystem.isPlaying) return;
            _particleSystem?.Stop();
            _particleSystem?.gameObject.SetActive(false);
        }
    }
}