using UnityEngine;
namespace _ImmersiveGames.Scripts.EnemySystem
{
    public abstract class DestructibleObjectSo : ScriptableObject
    {
        // Campos herdados de DestructibleObjectSo
        [SerializeField, Tooltip("Vida máxima do planeta")]
        public float maxHealth = 100f;

        [SerializeField, Tooltip("Defesa do planeta (redução de dano recebido)")]
        public float defense = 0f;

        [SerializeField, Tooltip("Se ativado, o planeta pode ser destruído")]
        public bool canDestroy = true;

        [SerializeField, Tooltip("Atraso antes de o planeta ser destruído após atingir 0 de vida (segundos)")]
        public float deathDelay = 0f;
    }
}
