using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Traduz o novo preset simplificado para o contexto usado pelo runtime atual.
    /// </summary>
    public static class PlanetDefensePresetAdapter
    {
        public static PlanetDefenseSetupContext BuildContext(
            PlanetsMaster planet,
            DetectionType detectionType,
            PlanetResourcesSo resource,
            PlanetDefenseLoadoutSo loadout,
            PlanetDefensePresetSo preset,
            PoolData fallbackPool,
            DefenseWaveProfileSo fallbackWaveProfile,
            IDefenseStrategy fallbackStrategy)
        {
            var waveProfile = preset?.GetWaveProfile() ?? fallbackWaveProfile;
            var poolData = preset?.ResolvePoolData(fallbackPool) ?? fallbackPool;
            var strategy = preset?.ResolveStrategy(fallbackStrategy) ?? fallbackStrategy;

            var context = new PlanetDefenseSetupContext(
                planet,
                detectionType,
                resource,
                strategy,
                poolData,
                waveProfile,
                loadout);

            strategy?.ConfigureContext(context);

            DebugUtility.LogVerbose<PlanetDefensePresetAdapter>(
                $"[PresetAdapter] Context para {planet?.ActorName ?? "Unknown"}: " +
                $"Preset='{preset?.name ?? "null"}', Pool='{poolData?.name ?? "null"}', " +
                $"WaveProfile='{waveProfile?.name ?? "runtime"}', Strategy='{strategy?.StrategyId ?? "null"}'.");

            return context;
        }
    }
}
