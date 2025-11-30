using System;
using System.Collections.Generic;
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
        [Tooltip("Role de fallback aplicado quando nenhum mapeamento corresponde ao identifier.")]
        [SerializeField]
        private DefenseRole fallbackRole = DefenseRole.Unknown;

        [Tooltip("Mapeamentos de um identifier (ex.: ActorName) para um DefenseRole específico.")]
        [SerializeField]
        private List<DefenseRoleBinding> roleMappings = new();

        public DefenseRole ResolveRole(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return fallbackRole;
            }

            foreach (DefenseRoleBinding binding in roleMappings)
            {
                if (binding == null || string.IsNullOrWhiteSpace(binding.Identifier))
                {
                    continue;
                }

                if (string.Equals(binding.Identifier, identifier, StringComparison.Ordinal))
                {
                    return binding.Role;
                }
            }

            return fallbackRole;
        }

        [Serializable]
        private class DefenseRoleBinding
        {
            [Tooltip("Chave string que identifica o ator ou detector (ex.: ActorName).")]
            [SerializeField]
            private string identifier;

            [Tooltip("DefenseRole associado ao identifier informado.")]
            [SerializeField]
            private DefenseRole role = DefenseRole.Unknown;

            public string Identifier => identifier;
            public DefenseRole Role => role;
        }
    }
}
