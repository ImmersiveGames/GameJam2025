using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.SkinSystems.Configurable;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem.Skins
{
    public interface IActorSkinAudioProvider
    {
        bool TryGetSound(SkinAudioKey key, out SoundData sound);
    }

    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/Audio/Skin Audio Configurable")]
    public class SkinAudioConfigurable : SkinConfigurable, IActorSkinAudioProvider
    {
        private ISkinAudioConfig _currentAudioConfig;

        protected override void Awake()
        {
            // IMPORTANTÍSSIMO: o base usa 'targetModelType' (campo), não propriedade.
            targetModelType = ModelType.SoundRoot;

            base.Awake();
        }

        protected override void ConfigureSkin(ISkinConfig skinConfig)
        {
            if (skinConfig == null)
                return;

            if (skinConfig is not ISkinAudioConfig audioConfig)
            {
                DebugUtility.LogWarning<SkinAudioConfigurable>(
                    $"[{name}] SkinConfig recebida não implementa ISkinAudioConfig.",
                    this);
                return;
            }

            _currentAudioConfig = audioConfig;

            DebugUtility.Log<SkinAudioConfigurable>(
                $"[{name}] Skin de áudio aplicada (SoundRoot). Entradas: {_currentAudioConfig.AudioEntries?.Count ?? 0}",
                DebugUtility.Colors.CrucialInfo,
                this);
        }

        protected override void ApplyDynamicModifications()
        {
            // Nenhuma modificação dinâmica por enquanto
        }

        public bool TryGetSound(SkinAudioKey key, out SoundData sound)
        {
            if (_currentAudioConfig != null)
            {
                return _currentAudioConfig.TryGetSound(key, out sound);
            }

            sound = null;
            return false;
        }
    }
}

