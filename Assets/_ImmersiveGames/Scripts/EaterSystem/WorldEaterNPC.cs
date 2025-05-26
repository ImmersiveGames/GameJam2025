using UnityEngine;
using UnityEngine.AI;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.EnemySystem;

public class WorldEaterNPC : MonoBehaviour
{
    [Header("Target Planet")]
    [SerializeField] Transform planetTarget;

    [Header("Movement")]
    [SerializeField] float speed = 5f;
    [SerializeField] float stoppingDistance = 2f;

    [Header("Animation")]
    [SerializeField] Animator animator;

    [Header("Attack")]
    [SerializeField] float biteDamage = 5f;
    [SerializeField] Transform planetsTransform;
    Planets planet;

    private bool isEating = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
            GetATarget();

        if (planetTarget == null || isEating)
            return;

        // Calcula a posição do alvo mantendo o mesmo Y (altura atual)
        Vector3 targetPosition = new Vector3(planetTarget.position.x, transform.position.y, planetTarget.position.z);

        // Distância até o alvo no plano XZ
        float distance = Vector3.Distance(transform.position, targetPosition);

        if (distance > stoppingDistance)
        {
            MoveTowards(targetPosition);
            animator.SetBool("isEating", false);
        }
        else
        {
            StartEating();
        }
    }

    private void MoveTowards(Vector3 targetPosition)
    {
        // Direção
        Vector3 direction = (targetPosition - transform.position).normalized;

        // Movimento
        transform.position += direction * speed * Time.deltaTime;

        // Rotaciona para olhar na direção
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    private void StartEating()
    {
        transform.SetParent(planetTarget.transform);        
        isEating = true;
        animator.SetBool("isEating", true);
        transform.LookAt(planetTarget);

        // Aqui você pode chamar outro efeito, destruir o planeta, etc.
        // Exemplo:
        // Destroy(planetTarget.gameObject, 3f); // Destrói após 3 segundos de animação
    }

    public void Bite() 
    {       
        planet.TakeDamage(biteDamage);
    }

    // Método público para setar o alvo dinamicamente
    public void SetTarget(Transform newTarget)
    {            
        planetTarget = newTarget;
        if (planetTarget != null) 
        {
            planet = planetTarget.GetComponent<Planets>();
            planet.OnDeath += RemoveTarget;
        }

        isEating = false;
    }

    public void RemoveTarget(DestructibleObject Do) 
    {
        planet.OnDeath -= RemoveTarget;
        isEating = false;
        animator.SetBool("isEating", false);
        transform.SetParent(null);
        SetTarget(null);
        
    }

    public void GetATarget() 
    {
        if (planetTarget == null) 
        {
            foreach(Transform planet in planetsTransform) 
            {
                if (planet.GetComponent<Planets>().IsActive) 
                {
                    planetTarget = planet.transform;
                    break;
                }
            }
            
            SetTarget(planetTarget);
        }
    }
}
