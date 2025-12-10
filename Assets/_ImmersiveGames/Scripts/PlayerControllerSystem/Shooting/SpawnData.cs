using System.Collections.Generic;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem.Shooting
{
    public struct SpawnData
    {
        public Vector3 position;
        public Vector3 direction;
    }

    public enum SpawnStrategyType
    {
        Single,
        MultipleLinear,
        Circular
    }

    /// <summary>
    /// Contrato para estratégias de spawn.
    /// Define:
    /// - Como gerar posições/direções de spawn.
    /// - Qual chave de áudio a estratégia usa na SkinAudioConfigData.
    /// </summary>
    public interface ISpawnStrategy
    {
        List<SpawnData> GetSpawnData(Vector3 basePosition, Vector3 baseDirection);

        /// <summary>
        /// Chave de áudio associada à estratégia.
        /// O PlayerShootController usa esta chave para buscar o SoundData
        /// na SkinAudioConfigData da skin atual.
        /// </summary>
        SkinAudioKey ShootAudioKey { get; }
    }
}