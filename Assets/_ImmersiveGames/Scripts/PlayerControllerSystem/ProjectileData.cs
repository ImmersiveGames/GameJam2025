using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    [CreateAssetMenu(fileName = "ProjectileData", menuName = "ImmersiveGames/ProjectileData")]
    public class ProjectileData : ScriptableObject
    {
        [SerializeField, Tooltip("Prefab do modelo visual do projétil")]
        public GameObject modelPrefab;

        [SerializeField, Tooltip("Velocidade de movimento do projétil (metros por segundo)")]
        public float speed = 15f;

        [SerializeField, Tooltip("Tempo de vida do projétil antes de ser retornado ao pool (segundos)")]
        public float lifetime = 3f;

        [SerializeField, Tooltip("Dano causado pelo projétil ao atingir um alvo")]
        public float damage = 25f;

        [SerializeField, Tooltip("Se ativado, o projétil é retornado ao pool ao acertar um alvo")]
        public bool destroyOnHit = true;
    }
}