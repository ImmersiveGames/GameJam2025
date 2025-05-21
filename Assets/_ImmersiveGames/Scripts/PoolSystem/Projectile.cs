using UnityEngine;

namespace _ImmersiveGames.Scripts.PoolSystem
{
    using _ImmersiveGames.Scripts.EnemySystem;
    
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float _speed = 15f;
        [SerializeField] private float _lifetime = 3f;
        [SerializeField] private float _damage = 25f;
        [SerializeField] private bool _destroyOnEnemyHit = true;
        [SerializeField] private string[] _targetTags = { "Enemy" }; // Tags que o projétil pode atingir
        
        private Vector3 _direction;
        private float _timer;
        private ObjectPool _originPool;
        private Rigidbody _rb;
    
        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            
            // Desabilita a física para o projétil
            if (_rb != null)
            {
                _rb.isKinematic = true;
                _rb.useGravity = false;
            }
        }
    
        private void OnEnable()
        {
            _timer = 0f;
            _direction = Vector3.zero;
        }
    
        private void Update()
        {
            // Incrementa o timer de vida
            _timer += Time.deltaTime;
            
            if (_timer >= _lifetime)
            {
                ReturnToPool();
                return;
            }
            
            // Move o projétil em linha reta com base na direção inicial
            if (_direction != Vector3.zero)
            {
                transform.position += _direction * _speed * Time.deltaTime;
            }
        }
    
        public void Initialize(Vector3 direction, ObjectPool pool)
        {
            _originPool = pool;
            
            // Armazena a direção normalizada para uso no Update
            _direction = direction.normalized;
            
            // Opcionalmente, define a rotação do projétil para alinhar com a direção
            transform.forward = _direction;
        }
    
        private void OnTriggerEnter(Collider other)
        {
            bool hitTarget = false;
            
            // Verificar se atingiu um objeto com uma das tags alvo
            if (_targetTags.Length > 0)
            {
                foreach (string tag in _targetTags)
                {
                    if (other.CompareTag(tag))
                    {
                        hitTarget = true;
                        break;
                    }
                }
            }
            
            // Se atingiu um alvo válido
            if (hitTarget || _targetTags.Length == 0)
            {
                // Verificar se é um inimigo e causar dano
                Enemy enemy = other.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(_damage);
                    
                    // Retornar ao pool apenas se configurado para destruir ao atingir inimigos
                    if (_destroyOnEnemyHit)
                    {
                        ReturnToPool();
                        return;
                    }
                }
                else
                {
                    // Se colidiu com algo que não é um inimigo (como uma parede)
                    ReturnToPool();
                    return;
                }
            }
            
            // Se chegou aqui, não retorna ao pool ainda (por exemplo, para projéteis que atravessam inimigos)
        }

        private void ReturnToPool()
        {
            // Usar o componente PooledObject para retornar ao pool correto
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
