using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils.Extensions;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ActorSystems
{
    public abstract class DeathExplosionEffect : MonoBehaviour
    {
        [SerializeField]
        private GameObject explosionPrefab;
        private ParticleSystem[] _particleSystem;

        private FxRoot _fxRoot;
        public FxRoot FxRoot => _fxRoot ??= this.GetOrCreateComponentInChild<FxRoot>("FxRoot");
        public Transform FxTransform => FxRoot.transform;
        public void SetFxActive(bool active)
        {
            if (_fxRoot != null)
            {
                _fxRoot.gameObject.SetActive(active);
            }
        }

        protected virtual void Awake()
        {
            explosionPrefab = Instantiate(explosionPrefab, FxTransform.position, Quaternion.identity);
            explosionPrefab.SetActive(false);
            explosionPrefab.transform.SetParent(FxTransform);
            _particleSystem = explosionPrefab.GetComponentsInChildren<ParticleSystem>(true);
        }

        public virtual void EnableParticles()
        {
            explosionPrefab.SetActive(true);
            foreach (var ps in _particleSystem)
            {
                ps?.gameObject.SetActive(true);
                ps?.Play();
            }
        }

        protected virtual void DisableParticles()
        {
            if (_particleSystem == null) return;
            explosionPrefab.SetActive(false);
            foreach (var ps in _particleSystem)
            {
                if (!ps || !ps.isPlaying) return;
                ps.Stop();
            }
        }
        
    }
}