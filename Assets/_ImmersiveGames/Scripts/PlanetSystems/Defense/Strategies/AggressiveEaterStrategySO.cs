using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense.Strategies
{
    /// <summary>
    /// Estratégia voltada para confrontar Eaters com minions rápidos e imprevisíveis.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AggressiveEaterStrategy",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Strategies/Aggressive Eater Strategy")]
    public sealed class AggressiveEaterStrategySo : DefenseStrategySO
    {
        [Header("Profiles por alvo")]
        [SerializeField] private DefenseMinionBehaviorProfileSO eaterProfile;

        [Tooltip("Profile usado quando o alvo não é um Eater; mantém compatibilidade com a estratégia base.")]
        [SerializeField] private DefenseMinionBehaviorProfileSO fallbackProfile;

        public override void OnEngaged(PlanetsMaster planet, DetectionType detectionType)
        {
            base.OnEngaged(planet, detectionType);
            DebugUtility.LogVerbose<AggressiveEaterStrategySo>(
                $"[Strategy] Aggressive/Eater ativa em {planet?.ActorName ?? "Unknown"} para {detectionType?.TypeName ?? "Unknown"}.");
        }

        public override DefenseMinionBehaviorProfileSO SelectMinionProfile(
            DefenseRole role,
            DefenseMinionBehaviorProfileSO waveProfile,
            DefenseMinionBehaviorProfileSO minionProfile)
        {
            if (role == DefenseRole.Eater && eaterProfile != null)
            {
                return eaterProfile;
            }

            if (waveProfile != null)
            {
                return waveProfile;
            }

            if (role == DefenseRole.Unknown && TargetRole == DefenseRole.Eater && eaterProfile != null)
            {
                return eaterProfile;
            }

            return fallbackProfile != null ? fallbackProfile : minionProfile;
        }
    }
}
