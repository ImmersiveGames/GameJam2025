using _ImmersiveGames.Scripts.Tags;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ActorSystems
{
    public abstract class DeathExplosionEffect : MonoBehaviour
    {
        [SerializeField]
        private GameObject explosionPrefab;
        private ParticleSystem[] _particleSystem;
        private ActorMaster _actorMaster;
        private FxRoot _fxRoot;
        
        private GameObject _explosionInstance;

        protected virtual void Awake()
        {
            _actorMaster = GetComponent<ActorMaster>();
            _fxRoot = _actorMaster.GetFxRoot();
            _explosionInstance = Instantiate(explosionPrefab, _fxRoot.transform.position, Quaternion.identity);
            _explosionInstance.SetActive(false);
            _explosionInstance.transform.SetParent(_fxRoot.transform);
            _particleSystem = _explosionInstance.GetComponentsInChildren<ParticleSystem>(true);
        }
        
        protected virtual void EnableParticles()
        {
            _explosionInstance.SetActive(true);
            foreach (var ps in _particleSystem)
            {
                ps?.gameObject.SetActive(true);
                ps?.Play();
            }
        }

        protected virtual void DisableParticles()
        {
            if (_particleSystem == null) return;
            _explosionInstance.SetActive(false);
            foreach (var ps in _particleSystem)
            {
                if (!ps || !ps.isPlaying) return;
                ps.Stop();
            }
        }
        
    }
}