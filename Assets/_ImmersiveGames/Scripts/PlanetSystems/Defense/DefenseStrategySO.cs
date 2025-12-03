using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Estratégia base de defesa planetária.
    /// - Resolve o alvo prioritário (DefenseRole) de forma determinística.
    /// - Define perfil de comportamento de minions por role sem duplicar dados por estratégia concreta.
    /// - Falha cedo quando requisitos mínimos não estão configurados.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlanetDefenseStrategy",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Strategies/Planet Defense Strategy (Base)")]
    public class PlanetDefenseStrategySo : ScriptableObject, IDefenseStrategy
    {
        [Header("Identidade da estratégia")]
        [SerializeField]
        private string strategyId = "PlanetDefenseStrategy";

        [Header("Configuração do alvo preferencial")]
        [Tooltip("Role preferido pelo planeta ao engajar defesas quando não houver mapeamento explícito.")]
        [SerializeField]
        private DefenseRole preferredTargetRole = DefenseRole.Unknown;

        [Tooltip("Role aplicado quando nenhum mapeamento for resolvido; ajuda a evitar Unknown em runtime.")]
        [SerializeField]
        private DefenseRole unmappedTargetRoleFallback = DefenseRole.Unknown;

        [Header("Configuração externa de roles (opcional)")]
        [Tooltip("Config compartilhada para mapear identifiers em roles.")]
        [SerializeField]
        private DefenseRoleConfig sharedRoleConfig;

        [Header("Mapeamentos de role embutidos")]
        [Tooltip("Mapeamentos internos de identifier para role, evitando duplicidade com DefenseRoleConfig quando desnecessário.")]
        [SerializeField]
        private List<DefenseRoleBinding> embeddedRoleBindings = new();

        [Header("Profiles por role")]
        [Tooltip("Perfil de comportamento padrão quando não há correspondência por role ou wave.")]
        [SerializeField]
        private DefenseMinionBehaviorProfileSO defaultBehaviorProfile;

        [Tooltip("Associações explícitas entre role e perfil de comportamento, compartilhadas entre estratégias concretas.")]
        [SerializeField]
        private List<DefenseRoleBehaviorBinding> roleBehaviorBindings = new();

        public string StrategyId => string.IsNullOrWhiteSpace(strategyId) ? name : strategyId.Trim();

        public DefenseRole TargetRole => preferredTargetRole;

        public virtual void ConfigureContext(PlanetDefenseSetupContext context)
        {
            // Estratégias concretas podem ajustar pool, profile ou outros dados do contexto.
        }

        public virtual void OnEngaged(PlanetsMaster planet, DetectionType detectionType)
        {
            DebugUtility.LogVerbose<PlanetDefenseStrategySo>(
                $"[Strategy] {StrategyId} engaged for {planet?.ActorName ?? "Unknown"} ({detectionType?.TypeName ?? "Unknown"}).");
        }

        public virtual void OnDisengaged(PlanetsMaster planet, DetectionType detectionType)
        {
            DebugUtility.LogVerbose<PlanetDefenseStrategySo>(
                $"[Strategy] {StrategyId} disengaged for {planet?.ActorName ?? "Unknown"} ({detectionType?.TypeName ?? "Unknown"}).");
        }

        public virtual DefenseMinionBehaviorProfileSO SelectMinionProfile(
            DefenseRole role,
            DefenseMinionBehaviorProfileSO waveProfile,
            DefenseMinionBehaviorProfileSO minionProfile)
        {
            var resolvedRole = role != DefenseRole.Unknown ? role : preferredTargetRole;
            var roleProfile = ResolveBehaviorProfile(resolvedRole);

            if (roleProfile != null)
            {
                return roleProfile;
            }

            if (waveProfile != null)
            {
                return waveProfile;
            }

            if (defaultBehaviorProfile != null)
            {
                return defaultBehaviorProfile;
            }

            return minionProfile;
        }

        public virtual DefenseRole ResolveTargetRole(string targetIdentifier, DefenseRole requestedRole)
        {
            if (requestedRole != DefenseRole.Unknown)
            {
                return requestedRole;
            }

            var mappedRole = ResolveRoleFromBindings(targetIdentifier);
            if (mappedRole != DefenseRole.Unknown)
            {
                return mappedRole;
            }

            if (sharedRoleConfig != null)
            {
                var configRole = sharedRoleConfig.ResolveRole(targetIdentifier);
                if (configRole != DefenseRole.Unknown)
                {
                    return configRole;
                }
            }

            if (preferredTargetRole != DefenseRole.Unknown)
            {
                return preferredTargetRole;
            }

            return unmappedTargetRoleFallback;
        }

        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(strategyId))
            {
                DebugUtility.LogError<PlanetDefenseStrategySo>($"{name}: StrategyId is required; using asset name as fallback.");
                strategyId = name;
            }

            ValidateRoleBindings();
            ValidateBehaviorBindings();

            if (preferredTargetRole == DefenseRole.Unknown &&
                unmappedTargetRoleFallback == DefenseRole.Unknown &&
                !embeddedRoleBindings.Any() &&
                sharedRoleConfig == null)
            {
                DebugUtility.LogError<PlanetDefenseStrategySo>(
                    $"{name}: No role resolution configured. Set a preferred target, fallback role, role bindings or a shared role config.");
            }
        }

        protected void SetPreferredTargetRole(DefenseRole role)
        {
            preferredTargetRole = role;
        }

        protected void SetUnmappedTargetRoleFallback(DefenseRole role)
        {
            unmappedTargetRoleFallback = role;
        }

        protected void EnsureRoleBehaviorBinding(DefenseRole role, DefenseMinionBehaviorProfileSO profile)
        {
            if (role == DefenseRole.Unknown)
            {
                DebugUtility.LogError<PlanetDefenseStrategySo>($"{name}: Cannot bind Unknown role to a behavior profile.");
                return;
            }

            var existingBinding = roleBehaviorBindings.FirstOrDefault(binding => binding != null && binding.Role == role);
            if (existingBinding != null)
            {
                existingBinding.SetProfile(profile);
                return;
            }

            roleBehaviorBindings.Add(new DefenseRoleBehaviorBinding(role, profile));
        }

        private DefenseRole ResolveRoleFromBindings(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return unmappedTargetRoleFallback;
            }

            foreach (var binding in embeddedRoleBindings.Where(binding => binding != null && !string.IsNullOrWhiteSpace(binding.Identifier)))
            {
                if (binding.Identifier == identifier)
                {
                    return binding.Role;
                }
            }

            return unmappedTargetRoleFallback;
        }

        private DefenseMinionBehaviorProfileSO ResolveBehaviorProfile(DefenseRole role)
        {
            if (role == DefenseRole.Unknown)
            {
                return null;
            }

            foreach (var binding in roleBehaviorBindings.Where(binding => binding != null && binding.Role != DefenseRole.Unknown))
            {
                if (binding.Role == role)
                {
                    return binding.BehaviorProfile;
                }
            }

            return null;
        }

        private void ValidateRoleBindings()
        {
            var duplicatedKeys = embeddedRoleBindings
                .Where(binding => binding != null && !string.IsNullOrWhiteSpace(binding.Identifier))
                .GroupBy(binding => binding.Identifier)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            if (duplicatedKeys.Count > 0)
            {
                DebugUtility.LogError<PlanetDefenseStrategySo>(
                    $"{name}: Duplicate role identifiers detected: {string.Join(", ", duplicatedKeys)}");
            }
        }

        private void ValidateBehaviorBindings()
        {
            foreach (var binding in roleBehaviorBindings)
            {
                if (binding == null)
                {
                    DebugUtility.LogError<PlanetDefenseStrategySo>($"{name}: Null behavior binding detected.");
                    continue;
                }

                if (binding.Role == DefenseRole.Unknown)
                {
                    DebugUtility.LogError<PlanetDefenseStrategySo>($"{name}: Behavior binding with Unknown role is not allowed.");
                }

                if (binding.BehaviorProfile == null)
                {
                    DebugUtility.LogError<PlanetDefenseStrategySo>(
                        $"{name}: Behavior binding for role {binding.Role} has no profile assigned.");
                }
            }
        }

        [System.Serializable]
        private class DefenseRoleBinding
        {
            [Tooltip("Chave textual (ex.: ActorName) mapeada para um DefenseRole.")]
            [SerializeField]
            private string identifier;

            [Tooltip("Role que a estratégia deseja aplicar ao identifier informado.")]
            [SerializeField]
            private DefenseRole role = DefenseRole.Unknown;

            public string Identifier => identifier;
            public DefenseRole Role => role;
        }

        [System.Serializable]
        private class DefenseRoleBehaviorBinding
        {
            [Tooltip("Role alvo para associar ao perfil de comportamento do minion.")]
            [SerializeField]
            private DefenseRole role = DefenseRole.Unknown;

            [Tooltip("Perfil de comportamento escolhido para o role informado.")]
            [SerializeField]
            private DefenseMinionBehaviorProfileSO behaviorProfile;

            public DefenseRoleBehaviorBinding(DefenseRole role, DefenseMinionBehaviorProfileSO behaviorProfile)
            {
                this.role = role;
                this.behaviorProfile = behaviorProfile;
            }

            public DefenseRole Role => role;
            public DefenseMinionBehaviorProfileSO BehaviorProfile => behaviorProfile;

            public void SetProfile(DefenseMinionBehaviorProfileSO profile)
            {
                behaviorProfile = profile;
            }
        }
    }
}
