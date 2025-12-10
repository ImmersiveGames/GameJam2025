using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem.Configs
{
    /// <summary>
    /// Configurações globais do serviço de áudio:
    /// - Master: volume geral do jogo.
    /// - BGM / SFX: volumes por categoria, normalmente controlados pelo jogador na tela de opções.
    /// - Multiplicadores: ajustes de balance interno entre BGM e SFX, definidos por game design.
    ///
    /// Esta é a "fonte da verdade" para os volumes globais usados pelos serviços de áudio.
    /// </summary>
    [CreateAssetMenu(
        menuName = "ImmersiveGames/Audio/Audio Service Settings",
        fileName = "AudioServiceSettings")]
    public class AudioServiceSettings : ScriptableObject
    {
        [Header("Global / Master")]
        [Tooltip("Volume geral do jogo. Afeta todas as categorias de áudio (BGM e SFX).")]
        [Range(0f, 1f)]
        public float masterVolume = 1f;

        [Header("Category base volumes")]
        [Tooltip("Volume base de Música (BGM) controlado pelo jogador (UI de opções).")]
        [Range(0f, 1f)]
        public float bgmVolume = 1f;

        [Tooltip("Volume base de Efeitos Sonoros (SFX) controlado pelo jogador (UI de opções).")]
        [Range(0f, 1f)]
        public float sfxVolume = 1f;

        [Header("Category multipliers (balancing)")]
        [Tooltip("Multiplicador de balance para BGM. Use para ajustar o equilíbrio geral entre BGM e SFX sem mexer no valor do jogador.")]
        [Range(0.1f, 2f)]
        public float bgmMultiplier = 1f;

        [Tooltip("Multiplicador de balance para SFX. Use para ajustar o equilíbrio geral entre SFX e BGM sem mexer no valor do jogador.")]
        [Range(0.1f, 2f)]
        public float sfxMultiplier = 1f;
    }
}