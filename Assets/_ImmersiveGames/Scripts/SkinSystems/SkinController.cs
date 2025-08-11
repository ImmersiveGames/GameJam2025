using System;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.SkinSystems
{
    /// <summary>
    /// Gerencia skins para objetos fora do sistema de pooling, com eventos de notificação.
    /// </summary>
    [DebugLevel(DebugLevel.Warning)]
    public class SkinController : SkinComponentBase
    {
        #region Fields
        public event Action<ISkinConfig> OnSkinChanged;
        public event Action<string> OnSkinChangeFailed;
        #endregion

        #region Initialization
        protected override void Awake()
        {
            base.Awake();

            if (skinCollectionData == null)
            {
                DebugUtility.LogError<SkinController>($"SkinCollectionData não atribuído em '{name}'.", this);
                OnSkinChangeFailed?.Invoke("SkinCollectionData não atribuído.");
                return;
            }

            Initialize();
        }
        #endregion

        #region Skin Management
        public override void ApplySkin(ISkinConfig newSkin)
        {
            if (newSkin == null)
            {
                DebugUtility.LogError<SkinController>($"SkinConfig inválido fornecido em '{name}'.", this);
                OnSkinChangeFailed?.Invoke("SkinConfig inválido fornecido.");
                return;
            }

            base.ApplySkin(newSkin);
            OnSkinChanged?.Invoke(newSkin);
        }
        #endregion
    }
}