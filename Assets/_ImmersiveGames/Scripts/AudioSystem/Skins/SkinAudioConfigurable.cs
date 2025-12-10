// Path: _ImmersiveGames/Scripts/AudioSystem/Skins/SkinAudioConfigurable.cs

using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.SkinSystems.Configurable;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem.Skins
{
    /// <summary>
    /// Contrato para qualquer componente que exponha sons da skin atual por chave.
    /// Permite que controllers (como PlayerShootController) peçam som via SkinAudioKey,
    /// sem conhecer detalhes do sistema de skin.
    /// </summary>
    public interface IActorSkinAudioProvider
    {
        bool TryGetSound(SkinAudioKey key, out SoundData sound);
    }

    /// <summary>
    /// Integra SkinSystem (SkinAudioConfigData / SoundRoot) com o sistema de áudio.
    ///
    /// - targetModelType é fixado em SoundRoot.
    /// - Quando a skin de SoundRoot é aplicada, guarda o ISkinAudioConfig atual.
    /// - Implementa IActorSkinAudioProvider para fornecer SoundData por SkinAudioKey.
    ///
    /// Não instancia nada, não mexe em prefab: trabalha apenas com dados de áudio da skin.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/Audio/Skin Audio Configurable")]
    public class SkinAudioConfigurable : SkinConfigurable, IActorSkinAudioProvider
    {
        private ISkinAudioConfig _currentAudioConfig;

        #region Unity Lifecycle

        protected override void Awake()
        {
            // Garante que este configurável sempre escute a skin de áudio.
            targetModelType = ModelType.SoundRoot;

            base.Awake();
        }

        #endregion

        #region SkinConfigurable Overrides

        /// <summary>
        /// Chamado quando a SkinConfig de ModelType.SoundRoot é aplicada.
        /// Esperamos receber um SkinAudioConfigData (ISkinAudioConfig).
        /// </summary>
        protected override void ConfigureSkin(ISkinConfig skinConfig)
        {
            if (skinConfig == null)
                return;

            if (skinConfig is not ISkinAudioConfig audioConfig)
            {
                DebugUtility.LogWarning<SkinAudioConfigurable>(
                    $"[{name}] ConfigureSkin recebeu uma ISkinConfig que não implementa ISkinAudioConfig. Ignorando.");
                return;
            }

            if (audioConfig.ModelType != targetModelType)
            {
                DebugUtility.LogWarning<SkinAudioConfigurable>(
                    $"[{name}] ISkinAudioConfig com ModelType={audioConfig.ModelType}, " +
                    $"mas este SkinAudioConfigurable espera {targetModelType}.");
                return;
            }

            _currentAudioConfig = audioConfig;

            DebugUtility.LogVerbose<SkinAudioConfigurable>(
                $"[{name}] Skin de áudio aplicada. Entradas disponíveis: " +
                $"{_currentAudioConfig.AudioEntries?.Count ?? 0}");
        }

        /// <summary>
        /// Ponto de extensão para modificações dinâmicas sem troca de skin.
        /// No momento não há necessidade de ajustes adicionais.
        /// </summary>
        protected override void ApplyDynamicModifications()
        {
            // No-op por enquanto.
        }

        #endregion

        #region IActorSkinAudioProvider

        public bool TryGetSound(SkinAudioKey key, out SoundData sound)
        {
            if (_currentAudioConfig != null)
            {
                return _currentAudioConfig.TryGetSound(key, out sound);
            }

            sound = null;
            return false;
        }

        #endregion
    }
}
