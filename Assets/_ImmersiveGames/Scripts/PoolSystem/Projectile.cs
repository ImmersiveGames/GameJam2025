using UnityEngine;
using _ImmersiveGames.Scripts.EnemySystem;
using _ImmersiveGames.Scripts.PlayerControllerSystem;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PoolSystem
{
    public class Projectile : MonoBehaviour
    {
        private Vector3 _direction;
        private float _timer;
        private ProjectileData _data;

        private void Awake()
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        private void OnEnable()
        {
            _timer = 0f;
            _direction = Vector3.zero;
        }

        private void Update()
        {
            if (_data == null) return;

            _timer += Time.deltaTime;
            if (_timer >= _data.lifetime)
            {
                ReturnToPool();
                return;
            }

            if (_direction != Vector3.zero)
            {
                transform.position += _direction * _data.speed * Time.deltaTime;
            }
        }

        public void SetProjectileData(ProjectileData data)
        {
            _data = data;
        }

        public void Initialize(Vector3 direction)
        {
            _direction = direction.normalized;
            transform.forward = _direction;
        }

        private void OnTriggerEnter(Collider other)
        {
            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                damageable.TakeDamage(_data.damage);
                if (_data.destroyOnHit)
                {
                    ReturnToPool();
                }
            }
            else
            {
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            ProjectilePooledObject pooledObj = GetComponent<ProjectilePooledObject>();
            if (pooledObj != null)
            {
                pooledObj.ReturnSelfToPool();
            }
            else
            {
                gameObject.SetActive(false);
                DebugUtility.LogWarning<Projectile>($"Projectile {gameObject.name} não tem ProjectilePooledObject.", this);
            }
        }
    }
}