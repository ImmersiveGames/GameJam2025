using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense.Strategies
{
    /// <summary>
    /// Estratégia defensiva contra Players: prioriza minions mais lentos e previsíveis.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DefensivePlayerStrategy",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Strategies/Defensive Player Strategy")]
    public sealed class DefensivePlayerStrategySO : DefenseStrategySO
    {
        [Header("Profiles por alvo")]
        [SerializeField] private DefenseMinionBehaviorProfileSO playerProfile;

        [Tooltip("Profile usado quando o alvo não é Player ou quando não há match direto.")]
        [SerializeField] private DefenseMinionBehaviorProfileSO fallbackProfile;

        public override void OnEngaged(PlanetsMaster planet, DetectionType detectionType)
        {
            base.OnEngaged(planet, detectionType);
            DebugUtility.LogVerbose<DefensivePlayerStrategySO>(
                $"[Strategy] Defensive/Player ativa em {planet?.ActorName ?? "Unknown"} para {detectionType?.TypeName ?? "Unknown"}.");
        }

        public override DefenseMinionBehaviorProfileSO SelectMinionProfile(
            DefenseRole role,
            DefenseMinionBehaviorProfileSO waveProfile,
            DefenseMinionBehaviorProfileSO minionProfile)
        {
            if (role == DefenseRole.Player && playerProfile != null)
            {
                return playerProfile;
            }

            if (waveProfile != null)
            {
                return waveProfile;
            }

            if (role == DefenseRole.Unknown && TargetRole == DefenseRole.Player && playerProfile != null)
            {
                return playerProfile;
            }

            return fallbackProfile != null ? fallbackProfile : minionProfile;
        }
    }
}
