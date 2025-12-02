using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Descreve modos de seleção de alvo para as defesas planetárias sem depender
    /// de ScriptableObjects adicionais. Mantém a semântica de targeting próxima
    /// do código, reduzindo ambiguidade entre Player/Eater em setups de multiplayer local.
    /// </summary>
    public enum DefenseTargetMode
    {
        [Tooltip("Sempre mirar no jogador, ignorando o Eater mesmo que detectado.")]
        PlayerOnly,

        [Tooltip("Sempre mirar no Eater, ignorando o jogador mesmo que detectado.")]
        EaterOnly,

        [Tooltip("Aceita ambos os alvos e escolhe o primeiro disponível no contexto.")]
        PlayerOrEater,

        [Tooltip("Prefere o jogador quando ambos estão presentes; cai para Eater como fallback.")]
        PreferPlayer,

        [Tooltip("Prefere o Eater quando ambos estão presentes; cai para jogador como fallback.")]
        PreferEater
    }
}
