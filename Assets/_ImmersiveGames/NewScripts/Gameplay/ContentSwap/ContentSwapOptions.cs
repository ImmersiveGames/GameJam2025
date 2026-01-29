#nullable enable
using System;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.ContentSwap
{
    /// <summary>
    /// Opções de execução para troca de conteúdo (fade, HUD e timeout).
    /// </summary>
    [Serializable]
    public sealed class ContentSwapOptions
    {
        public const int DefaultTimeoutMs = 20000;

        [SerializeField] private bool useFade;
        [SerializeField] private bool useLoadingHud;
        [SerializeField] private int timeoutMs = DefaultTimeoutMs;

        public bool UseFade
        {
            get => useFade;
            set => useFade = value;
        }

        public bool UseLoadingHud
        {
            get => useLoadingHud;
            set => useLoadingHud = value;
        }

        public int TimeoutMs
        {
            get => timeoutMs;
            set => timeoutMs = value;
        }

        public static ContentSwapOptions Default => new ContentSwapOptions();

        public ContentSwapOptions Clone()
        {
            return new ContentSwapOptions
            {
                UseFade = UseFade,
                UseLoadingHud = UseLoadingHud,
                TimeoutMs = TimeoutMs
            };
        }
    }
}
