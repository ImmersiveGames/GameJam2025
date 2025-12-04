using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Estratégia simples baseada em <see cref="DefenseTargetMode"/>, evitando ScriptableObjects
    /// adicionais e mantendo a seleção de alvo próxima ao preset.
    /// Comentários em português, código em inglês conforme convenção do projeto.
    /// </summary>
    public sealed class SimplePlanetDefenseStrategy : IDefenseStrategy
    {
        private const string DefaultStrategyId = "SimplePresetStrategy";

        private readonly string strategyId;
        private readonly DefenseTargetMode targetMode;
        private readonly DefenseRole preferredRole;
        private readonly Dictionary<string, DefenseRole> cachedRoles;

        public SimplePlanetDefenseStrategy(
            DefenseTargetMode targetMode = DefenseTargetMode.PreferPlayer,
            string customStrategyId = DefaultStrategyId)
        {
            this.targetMode = targetMode;
            preferredRole = ResolvePreferredRole(targetMode);
            strategyId = string.IsNullOrWhiteSpace(customStrategyId) ? DefaultStrategyId : customStrategyId;
            cachedRoles = new Dictionary<string, DefenseRole>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Identificador amigável para logs e debugging.
        /// </summary>
        public string StrategyId => strategyId;

        /// <summary>
        /// Role preferido com base no modo selecionado.
        /// </summary>
        public DefenseRole TargetRole => preferredRole;

        /// <summary>
        /// Não altera contexto; mantém neutralidade com presets existentes.
        /// </summary>
        public void ConfigureContext(PlanetDefenseSetupContext context)
        {
            // Mantém SRP: preset fornece dados, estratégia apenas expressa preferência de alvo.
        }

        public void OnEngaged(PlanetsMaster planet, DetectionType detectionType)
        {
            DebugUtility.LogVerbose<SimplePlanetDefenseStrategy>(
                $"[Strategy] {StrategyId} engaged for {planet?.ActorName ?? "Unknown"} ({detectionType?.TypeName ?? "Unknown"}). Mode={targetMode}.");
        }

        public void OnDisengaged(PlanetsMaster planet, DetectionType detectionType)
        {
            DebugUtility.LogVerbose<SimplePlanetDefenseStrategy>(
                $"[Strategy] {StrategyId} disengaged for {planet?.ActorName ?? "Unknown"} ({detectionType?.TypeName ?? "Unknown"}). Mode={targetMode}.");
        }

        public DefenseMinionBehaviorProfileSO SelectMinionProfile(
            DefenseRole targetRole,
            DefenseMinionBehaviorProfileSO waveProfile,
            DefenseMinionBehaviorProfileSO minionProfile)
        {
            // Respeita prioridade existente: wave profile > minion profile do pool.
            return waveProfile != null ? waveProfile : minionProfile;
        }

        /// <summary>
        /// Resolve role com cache para identifiers repetidos, usando o <see cref="DefenseTargetMode"/>.
        /// </summary>
        public DefenseRole ResolveTargetRole(string targetIdentifier, DefenseRole requestedRole)
        {
            if (requestedRole != DefenseRole.Unknown)
            {
                return requestedRole;
            }

            var cacheKey = targetIdentifier ?? string.Empty;
            if (cachedRoles.TryGetValue(cacheKey, out var cachedRole))
            {
                return cachedRole;
            }

            var resolvedRole = ResolveRoleByMode(targetIdentifier);
            cachedRoles[cacheKey] = resolvedRole;
            return resolvedRole;
        }

        private DefenseRole ResolveRoleByMode(string targetIdentifier)
        {
            var identifierRole = TryResolveIdentifier(targetIdentifier);

            switch (targetMode)
            {
                case DefenseTargetMode.PlayerOnly:
                    return DefenseRole.Player;
                case DefenseTargetMode.EaterOnly:
                    return DefenseRole.Eater;
                case DefenseTargetMode.PlayerOrEater:
                    return identifierRole != DefenseRole.Unknown ? identifierRole : DefenseRole.Player;
                case DefenseTargetMode.PreferPlayer:
                    return identifierRole != DefenseRole.Unknown ? identifierRole : DefenseRole.Player;
                case DefenseTargetMode.PreferEater:
                    return identifierRole != DefenseRole.Unknown ? identifierRole : DefenseRole.Eater;
                default:
                    return preferredRole;
            }
        }

        private static DefenseRole ResolvePreferredRole(DefenseTargetMode mode)
        {
            return mode switch
            {
                DefenseTargetMode.PlayerOnly => DefenseRole.Player,
                DefenseTargetMode.EaterOnly => DefenseRole.Eater,
                DefenseTargetMode.PlayerOrEater => DefenseRole.Unknown,
                DefenseTargetMode.PreferPlayer => DefenseRole.Player,
                DefenseTargetMode.PreferEater => DefenseRole.Eater,
                _ => DefenseRole.Unknown,
            };
        }

        private static DefenseRole TryResolveIdentifier(string targetIdentifier)
        {
            if (string.IsNullOrWhiteSpace(targetIdentifier))
            {
                return DefenseRole.Unknown;
            }

            var normalized = targetIdentifier.Trim();
            if (normalized.Equals("player", StringComparison.OrdinalIgnoreCase))
            {
                return DefenseRole.Player;
            }

            if (normalized.Equals("eater", StringComparison.OrdinalIgnoreCase))
            {
                return DefenseRole.Eater;
            }

            return DefenseRole.Unknown;
        }
    }
}
