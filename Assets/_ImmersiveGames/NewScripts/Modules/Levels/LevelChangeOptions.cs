#nullable enable
using System;
using _ImmersiveGames.NewScripts.Modules.ContentSwap;
using _ImmersiveGames.NewScripts.Modules.ContentSwap.Runtime;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.Levels
{
    /// <summary>
    /// Opções para mudança de nível (opções de ContentSwap).
    /// </summary>
    [Serializable]
    public sealed class LevelChangeOptions
    {
        [SerializeField] private ContentSwapOptions? contentSwapOptions;

        public ContentSwapOptions? ContentSwapOptions
        {
            get => contentSwapOptions;
            set => contentSwapOptions = value;
        }

        public static LevelChangeOptions Default => new();

        public LevelChangeOptions Clone()
        {
            return new LevelChangeOptions
            {
                ContentSwapOptions = ContentSwapOptions?.Clone()
            };
        }
    }
}
