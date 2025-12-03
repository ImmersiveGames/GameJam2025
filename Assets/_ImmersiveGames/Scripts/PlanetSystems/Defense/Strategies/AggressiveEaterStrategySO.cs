using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense.Strategies
{
    /// <summary>
    /// Estratégia voltada para confrontar Eaters com minions rápidos e imprevisíveis.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlanetDefenseEaterAggressiveStrategy",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Strategies/Planet Defense Eater Aggressive Strategy")]
    public sealed class PlanetDefenseEaterAggressiveStrategySo : PlanetDefenseStrategySo
    {
        [Header("Profiles por alvo")]
        [SerializeField] private DefenseMinionBehaviorProfileSO eaterBehaviorProfile;

        [Tooltip("Profile usado quando o alvo não é um Eater; mantém compatibilidade com a estratégia base.")]
        [SerializeField] private DefenseMinionBehaviorProfileSO nonEaterBehaviorProfile;

        public override void OnEngaged(PlanetsMaster planet, DetectionType detectionType)
        {
            base.OnEngaged(planet, detectionType);
            DebugUtility.LogVerbose<PlanetDefenseEaterAggressiveStrategySo>(
                $"[Strategy] Aggressive/Eater engaged on {planet?.ActorName ?? "Unknown"} for {detectionType?.TypeName ?? "Unknown"}.");
        }

        public override DefenseMinionBehaviorProfileSO SelectMinionProfile(
            DefenseRole role,
            DefenseMinionBehaviorProfileSO waveProfile,
            DefenseMinionBehaviorProfileSO minionProfile)
        {
            if (role == DefenseRole.Eater && eaterBehaviorProfile != null)
            {
                return eaterBehaviorProfile;
            }

            if (role == DefenseRole.Unknown && TargetRole == DefenseRole.Eater && eaterBehaviorProfile != null)
            {
                return eaterBehaviorProfile;
            }

            var resolvedMinionProfile = nonEaterBehaviorProfile != null ? nonEaterBehaviorProfile : minionProfile;
            return base.SelectMinionProfile(role, waveProfile, resolvedMinionProfile);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            SetPreferredTargetRole(DefenseRole.Eater);
            SetUnmappedTargetRoleFallback(DefenseRole.Eater);
            EnsureRoleBehaviorBinding(DefenseRole.Eater, eaterBehaviorProfile);
        }
    }
}
