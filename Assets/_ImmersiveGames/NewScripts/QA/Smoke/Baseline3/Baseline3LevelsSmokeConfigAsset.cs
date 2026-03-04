using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Smoke.Baseline3
{
    /// <summary>
    /// Configuração canônica dos levels usados no smoke Baseline 3.0 (D/E).
    /// </summary>
    [CreateAssetMenu(
        fileName = "Baseline3LevelsSmokeConfig",
        menuName = "QA/Smoke/Baseline 3.0/Levels Smoke Config",
        order = 3000)]
    public sealed class Baseline3LevelsSmokeConfigAsset : ScriptableObject
    {
        [Header("Baseline levels")]
        [SerializeField] private LevelId initialLevel = new("level.1");
        [SerializeField] private LevelId swapTargetLevel = new("level.2");

        public LevelId InitialLevel => initialLevel;
        public LevelId SwapTargetLevel => swapTargetLevel;

        public bool IsValid => initialLevel.IsValid && swapTargetLevel.IsValid;
    }
}
