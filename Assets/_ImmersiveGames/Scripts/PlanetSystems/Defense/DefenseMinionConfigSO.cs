// LEGACY: DefenseMinionConfigSO não é mais usado pelo fluxo de defesa v2.
// Mantido apenas para compatibilidade temporária com assets antigos.
using System;
using _ImmersiveGames.Scripts.PlanetSystems.Defense;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Descreve um tipo lógico de minion de defesa, contendo apenas parâmetros
    /// de comportamento, lifetime e ajustes opcionais de movimento para editor.
    /// </summary>
    [Obsolete("LEGACY: não usado pelo fluxo de defesa v2. Evite criar novas instâncias.", false)]
    [CreateAssetMenu(
        fileName = "DefenseMinionConfig",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Minions/Minion Config")]
    public sealed class DefenseMinionConfigSO : ScriptableObject
    {
        [Header("Identidade do minion")]
        [Tooltip("Nome lógico deste minion para debugging e organização.")]
        [SerializeField]
        private string objectName;

        [Tooltip("Tempo de vida em segundos antes do retorno automático ao pool ou reaproveitamento.")]
        [SerializeField, Min(0f)]
        private float lifetime = 5f;

        [Header("Comportamento")]
        [Tooltip("Perfil de comportamento aplicado a todos os minions deste tipo.")]
        [SerializeField]
        private DefenseMinionBehaviorProfileSO behaviorProfile;

        [Header("Ajustes de movimento e fases")]
        [Tooltip("Velocidade base de movimento do minion.")]
        [SerializeField, Min(0f)]
        private float movementSpeed = 1f;

        [Tooltip("Multiplicador aplicado na fase de entrada do minion.")]
        [SerializeField, Min(0f)]
        private float entrySpeedMultiplier = 1f;

        [Tooltip("Raio de órbita desejado enquanto o minion aguarda para perseguir.")]
        [SerializeField, Min(0f)]
        private float orbitRadius = 1f;

        [Tooltip("Tempo máximo aguardando antes de reavaliar a perseguição após perder o alvo.")]
        [SerializeField, Min(0f)]
        private float chaseReacquireDelay = 0.5f;

        public string ObjectName => objectName;

        public float Lifetime => lifetime;

        public DefenseMinionBehaviorProfileSO BehaviorProfile => behaviorProfile;

        public float MovementSpeed => movementSpeed;

        public float EntrySpeedMultiplier => entrySpeedMultiplier;

        public float OrbitRadius => orbitRadius;

        public float ChaseReacquireDelay => chaseReacquireDelay;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(objectName))
            {
                DebugUtility.LogWarning<DefenseMinionConfigSO>($"ObjectName não configurado em {name}.", this);
            }

            if (lifetime < 0f)
            {
                DebugUtility.LogWarning<DefenseMinionConfigSO>($"Lifetime não pode ser negativo em {name}. Definindo como 0.", this);
                lifetime = 0f;
            }

            if (behaviorProfile == null)
            {
                DebugUtility.LogWarning<DefenseMinionConfigSO>($"BehaviorProfile não configurado em {name}.", this);
            }

            if (movementSpeed < 0f)
            {
                DebugUtility.LogWarning<DefenseMinionConfigSO>($"MovementSpeed não pode ser negativo em {name}. Definindo como 0.", this);
                movementSpeed = 0f;
            }

            if (entrySpeedMultiplier < 0f)
            {
                DebugUtility.LogWarning<DefenseMinionConfigSO>($"EntrySpeedMultiplier não pode ser negativo em {name}. Definindo como 0.", this);
                entrySpeedMultiplier = 0f;
            }

            if (orbitRadius < 0f)
            {
                DebugUtility.LogWarning<DefenseMinionConfigSO>($"OrbitRadius não pode ser negativo em {name}. Definindo como 0.", this);
                orbitRadius = 0f;
            }

            if (chaseReacquireDelay < 0f)
            {
                DebugUtility.LogWarning<DefenseMinionConfigSO>($"ChaseReacquireDelay não pode ser negativo em {name}. Definindo como 0.", this);
                chaseReacquireDelay = 0f;
            }
        }
#endif
    }
}
