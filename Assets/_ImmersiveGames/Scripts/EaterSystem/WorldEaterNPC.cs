using _ImmersiveGames.Scripts.PlanetSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    public class WorldEaterNpc : MonoBehaviour
    {
        private static readonly int _animisDead = Animator.StringToHash("isDead");
        private static readonly int _animisEating = Animator.StringToHash("isEating");
        [Header("Target Planet")]
        [SerializeField]
        private Transform planetsTransform;
        [SerializeField]
        private Transform planetTarget;

        [Header("Movement")]
        [SerializeField]
        private float speed = 5f;
        [SerializeField]
        private float stoppingDistance = 2f;

        [Header("Animation")]
        [SerializeField]
        private Animator animator;

        [Header("Attack")]
        [SerializeField]
        private float biteDamage = 5f;

        [Header("Hungry")]
        [SerializeField]
        private float maxHungry = 100f;
        [SerializeField]
        private float hungryRate = 5f;
        private float _currentHungry;

        [Header("Health")]
        [SerializeField]
        private float maxHealth = 100f;
        private float _currentHealth;

        private Planets _planet;

        private bool _isEating;
        private bool _isDead;

        private void Awake()
        {
            ResetEater();
        }

        private void Update()
        {
            if (_isDead) return;
        
            if (Input.GetKeyDown(KeyCode.T))
                GetATarget();

            DepleteHungry();

            if (planetTarget == null || _isEating)
                return;

            // Calcula a posi??o do alvo mantendo o mesmo Y (altura atual)
            Vector3 targetPosition = new Vector3(planetTarget.position.x, transform.position.y, planetTarget.position.z);

            // Distância at? o alvo no plano XZ
            float distance = Vector3.Distance(transform.position, targetPosition);

            if (distance > stoppingDistance)
            {
                MoveTowards(targetPosition);
                animator.SetBool(_animisEating, false);
            }
            else
            {
                StartEating();
            }
        }

        private void MoveTowards(Vector3 targetPosition)
        {
            // Dire??o
            Vector3 direction = (targetPosition - transform.position).normalized;

            // Movimento
            transform.position += direction * (speed * Time.deltaTime);

            // Rotaciona para olhar na dire??o
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
            }
        }

        private void StartEating()
        {
            transform.SetParent(planetTarget.transform);        
            _isEating = true;
            animator.SetBool(_animisEating, true);
            transform.LookAt(planetTarget);

            // Aqui você pode chamar outro efeito, destruir o planeta, etc.
            // Exemplo:
            // Destroy(planetTarget.gameObject, 3f); // Destrói após 3 segundos de animação
        }

        public void Bite() 
        {       
            _planet.TakeDamage(biteDamage);
        }

        // Método público para setar o alvo dinamicamente
        public void SetTarget(Transform newTarget)
        {            
            planetTarget = newTarget;
            if (planetTarget != null) 
            {
                _planet = planetTarget.GetComponent<Planets>();
                _planet.OnDeath += PlanetEaten;
            }

            _isEating = false;
        }

        public void PlanetEaten(DestructibleObject destructible)
        {
            _planet.OnDeath -= PlanetEaten;
            ResetEater();
            ReleaseTarget();

        }

        private void ReleaseTarget()
        {
            _isEating = false;
            animator.SetBool(_animisEating, false);
            transform.SetParent(null);
            SetTarget(null);
        }

        public void GetATarget() 
        {
            if (planetTarget == null) 
            {
                foreach(Transform planet1 in planetsTransform) 
                {
                    if (planet1.GetComponent<Planets>().IsActive) 
                    {
                        planetTarget = planet1.transform;
                        break;
                    }
                }
            
                SetTarget(planetTarget);
            }
        }


        private void ResetEater() 
        { 
            _currentHungry = maxHungry;
            _currentHealth = maxHealth;
        }
        private void DepleteHungry() 
        {
            _currentHungry -= hungryRate * Time.deltaTime;
            _currentHungry = Mathf.Clamp(_currentHungry, 0, maxHungry);

            if (_currentHungry <= 0) 
            {
                TakeDamage(hungryRate * Time.deltaTime);
            }
        }


        public void TakeDamage(float damage) 
        {
            _currentHealth -= damage;
            _currentHealth = Mathf.Clamp(_currentHealth, 0, maxHealth);

            if (_currentHealth == 0) Die();
        }

        private void Die() 
        {
            ReleaseTarget();
            animator.SetTrigger(_animisDead);
            _isDead = true;
        
        }


        public float GetHungry() { return _currentHungry / maxHungry; }
        public float GetHealth() { return _currentHealth / maxHealth; }

    }
}
