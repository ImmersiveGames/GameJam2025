using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SkinSystems.Data
{
    /// <summary>
    /// Chaves padrão de áudio que podem ser associadas a uma skin.
    /// </summary>
    public enum SkinAudioKey
    {
        Shoot,
        Hit,
        EngineLoop,
        Death,
        Revive,
        Custom1,
        Custom2,
    }

    [Serializable]
    public struct SkinAudioEntry
    {
        public SkinAudioKey key;
        public SoundData sound;
    }

    /// <summary>
    /// Contrato para configs de skin que também expõem uma coleção de áudios.
    /// </summary>
    public interface ISkinAudioConfig : ISkinConfig
    {
        IReadOnlyList<SkinAudioEntry> AudioEntries { get; }

        bool TryGetSound(SkinAudioKey key, out SoundData sound);
    }

    /// <summary>
    /// Extensão de SkinConfigData que adiciona uma coleção genérica de
    /// pares (chave de áudio, SoundData) para uso como "skin de áudio".
    /// Normalmente usada com ModelType.SoundRoot.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SkinAudioConfigData",
        menuName = "ImmersiveGames/Skin/Skin Audio Config",
        order = 3)]
    public class SkinAudioConfigData : SkinConfigData, ISkinAudioConfig
    {
        [Header("Audio Entries (Key -> SoundData)")]
        [SerializeField] private List<SkinAudioEntry> audioEntries = new List<SkinAudioEntry>();

        private Dictionary<SkinAudioKey, SoundData> _audioMap;

        public IReadOnlyList<SkinAudioEntry> AudioEntries => audioEntries;

        public bool TryGetSound(SkinAudioKey key, out SoundData sound)
        {
            EnsureMap();
            return _audioMap.TryGetValue(key, out sound);
        }

        private void EnsureMap()
        {
            if (_audioMap != null)
                return;

            _audioMap = new Dictionary<SkinAudioKey, SoundData>();
            if (audioEntries == null)
                return;

            foreach (var entry in audioEntries)
            {
                if (entry.sound == null)
                    continue;

                _audioMap[entry.key] = entry.sound;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Rebuild map in editor para manter consistente
            _audioMap = null;
            EnsureMap();

            // Valida se está sendo usada com o ModelType esperado
            if (ModelType != ModelType.SoundRoot)
            {
                Debug.LogWarning(
                    $"[SkinAudioConfigData] Asset '{name}' está com ModelType={ModelType}, " +
                    "mas é recomendado usar ModelType.SoundRoot para configs de áudio por skin.",
                    this);
            }

            // Valida se há pelo menos algum áudio configurado
            bool hasAnySound = false;
            if (audioEntries != null)
            {
                foreach (var entry in audioEntries)
                {
                    if (entry.sound != null)
                    {
                        hasAnySound = true;
                        break;
                    }
                }
            }

            if (!hasAnySound)
            {
                Debug.LogWarning(
                    $"[SkinAudioConfigData] Asset '{name}' não possui nenhum SoundData configurado " +
                    "na lista de Audio Entries. Verifique se isso é intencional.",
                    this);
            }
        }
#endif
    }
}
