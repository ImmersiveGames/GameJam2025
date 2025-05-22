using _ImmersiveGames.Scripts.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PoolSystem
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField, Tooltip("Velocidade do projétil")]
        private float speed = 15f;

        [SerializeField, Tooltip("Tempo de vida do projétil (segundos)")]
        private float lifetime = 3f;

        [SerializeField, Tooltip("Dano causado pelo projétil")]
        private float damage = 25f;

        [SerializeField, Tooltip("Se true, o projétil é retornado ao pool ao acertar um alvo")]
        private bool destroyOnHit = true;

        private Vector3 _direction;
        private float _timer;
        private ObjectPool _originPool;

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
            _timer += Time.deltaTime;
            if (_timer >= lifetime)
            {
                ReturnToPool();
                return;
            }

            if (_direction != Vector3.zero)
            {
                transform.position += _direction * speed * Time.deltaTime;
            }
        }

        public void Initialize(Vector3 direction, ObjectPool pool)
        {
            this._originPool = pool;
            this._direction = direction.normalized;
            transform.forward = this._direction;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Busca IDamageable no objeto colidido ou em seu pai
            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                damageable.TakeDamage(damage);
                if (destroyOnHit)
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
            PooledObject pooledObj = GetComponent<PooledObject>();
            if (pooledObj != null)
            {
                pooledObj.ReturnSelfToPool();
            }
            else if (_originPool != null)
            {
                _originPool.ReturnToPool(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}