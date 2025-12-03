using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense.Strategies
{
    /// <summary>
    /// Estratégia defensiva contra Players: prioriza minions mais lentos e previsíveis.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlanetDefensePlayerDefensiveStrategy",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Strategies/Planet Defense Player Defensive Strategy")]
    public sealed class PlanetDefensePlayerDefensiveStrategySo : PlanetDefenseStrategySo
    {
        [Header("Profiles por alvo")]
        [SerializeField] private DefenseMinionBehaviorProfileSO playerBehaviorProfile;

        [Tooltip("Profile usado quando o alvo não é Player ou quando não há match direto.")]
        [SerializeField] private DefenseMinionBehaviorProfileSO nonPlayerBehaviorProfile;

        public override void OnEngaged(PlanetsMaster planet, DetectionType detectionType)
        {
            base.OnEngaged(planet, detectionType);
            DebugUtility.LogVerbose<PlanetDefensePlayerDefensiveStrategySo>(
                $"[Strategy] Defensive/Player engaged on {planet?.ActorName ?? "Unknown"} for {detectionType?.TypeName ?? "Unknown"}.");
        }

        public override DefenseMinionBehaviorProfileSO SelectMinionProfile(
            DefenseRole role,
            DefenseMinionBehaviorProfileSO waveProfile,
            DefenseMinionBehaviorProfileSO minionProfile)
        {
            if (role == DefenseRole.Player && playerBehaviorProfile != null)
            {
                return playerBehaviorProfile;
            }

            if (role == DefenseRole.Unknown && TargetRole == DefenseRole.Player && playerBehaviorProfile != null)
            {
                return playerBehaviorProfile;
            }

            var resolvedMinionProfile = nonPlayerBehaviorProfile != null ? nonPlayerBehaviorProfile : minionProfile;
            return base.SelectMinionProfile(role, waveProfile, resolvedMinionProfile);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            SetPreferredTargetRole(DefenseRole.Player);
            SetUnmappedTargetRoleFallback(DefenseRole.Player);
            EnsureRoleBehaviorBinding(DefenseRole.Player, playerBehaviorProfile);
        }
    }
}
