using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    [CreateAssetMenu(
        fileName = "DefenseRoleConfig",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Defense Role Config",
        order = 100)]
    public class DefenseRoleConfig : ScriptableObject
    {
        [SerializeField] private DefenseRole defaultRole = DefenseRole.Unknown;
        [SerializeField] private List<DefenseRoleBinding> bindings = new();

        public DefenseRole ResolveRole(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return defaultRole;
            }

            foreach (DefenseRoleBinding binding in bindings)
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

            return defaultRole;
        }

        [Serializable]
        private class DefenseRoleBinding
        {
            [SerializeField] private string identifier;
            [SerializeField] private DefenseRole role = DefenseRole.Unknown;

            public string Identifier => identifier;
            public DefenseRole Role => role;
        }
    }
}
