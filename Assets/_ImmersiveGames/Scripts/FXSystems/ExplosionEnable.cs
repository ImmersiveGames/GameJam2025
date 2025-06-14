using UnityEngine;
namespace _ImmersiveGames.Scripts.FXSystems
{
    public class ExplosionEnable : MonoBehaviour
    {
        private ParticleSystem[] _particleSystem;

        private void OnEnable()
        {
            _particleSystem = GetComponentsInChildren<ParticleSystem>();
            
            foreach (var ps in _particleSystem)
            {
                ps?.gameObject.SetActive(true);
                ps?.Play();
            }
            
        }

        private void OnDisable()
        {
            if (_particleSystem == null) return;
            foreach (var ps in _particleSystem)
            {
                if (!ps || !ps.isPlaying) return;
                ps.Stop();
                ps.gameObject.SetActive(false);
            }
        }
    }
}