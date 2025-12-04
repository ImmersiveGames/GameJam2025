using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    [CreateAssetMenu(
        fileName = "DefenseRoleConfig",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Config/Role Config",
        order = 100)]
    public class DefenseRoleConfig : ScriptableObject
    {
        [Tooltip("Role do alvo detectado usado como fallback quando nenhum mapeamento corresponde ao identifier.")]
        [SerializeField]
        private DefenseRole fallbackRole = DefenseRole.Unknown;

        [Tooltip("Mapeamentos de um identifier (ex.: ActorName) para o DefenseRole do alvo detectado.")]
        [SerializeField]
        private List<DefenseRoleBinding> roleMappings = new();

        public DefenseRole ResolveRole(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return fallbackRole;
            }

            foreach (var binding in roleMappings.Where(binding => binding != null && !string.IsNullOrWhiteSpace(binding.Identifier)).Where(binding => string.Equals(binding.Identifier, identifier, StringComparison.Ordinal)))
            {
                return binding.Role;
            }

            return fallbackRole;
        }

        [Serializable]
        private class DefenseRoleBinding
        {
            [Tooltip("Chave string que identifica o ator ou detector (ex.: ActorName).")]
            [SerializeField]
            private string identifier;

            [Tooltip("DefenseRole aplicado ao alvo detectado que corresponde ao identifier informado.")]
            [SerializeField]
            private DefenseRole role = DefenseRole.Unknown;

            public string Identifier => identifier;
            public DefenseRole Role => role;
        }
    }
}
