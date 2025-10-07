using _ImmersiveGames.Scripts.AudioSystem;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem.Tests
{
    public partial class DamageSystemDebugger
    {
        [Header("Audio Test Volumes")]
        [SerializeField] private float hitVolume = 1f;
        [SerializeField] private float deathVolume = 1f;
        [SerializeField] private float reviveVolume = 1f;

        [ContextMenu("Audio/Test Hit Sound")]
        private void TestHitSound()
        {
            if (_audio == null)
            {
                Debug.LogWarning("🎧 Nenhum PlayerAudioController encontrado.");
                return;
            }

            _audio.PlayCustomShootSound(_audio.AudioConfig?.hitSound, hitVolume);
            Debug.Log($"🔊 Hit sound executado ({_audio.AudioConfig?.hitSound?.clip?.name ?? "None"})");
        }

        [ContextMenu("Audio/Test Death Sound")]
        private void TestDeathSound()
        {
            if (_audio == null)
            {
                Debug.LogWarning("🎧 Nenhum PlayerAudioController encontrado.");
                return;
            }

            _audio.PlayCustomShootSound(_audio.AudioConfig?.deathSound, deathVolume);
            Debug.Log($"💀 Death sound executado ({_audio.AudioConfig?.deathSound?.clip?.name ?? "None"})");
        }

        [ContextMenu("Audio/Test Revive Sound")]
        private void TestReviveSound()
        {
            if (_audio == null)
            {
                Debug.LogWarning("🎧 Nenhum PlayerAudioController encontrado.");
                return;
            }

            _audio.PlayCustomShootSound(_audio.AudioConfig?.reviveSound, reviveVolume);
            Debug.Log($"✨ Revive sound executado ({_audio.AudioConfig?.reviveSound?.clip?.name ?? "None"})");
        }

        [ContextMenu("Audio/Print Audio Status")]
        private void PrintAudioStatus()
        {
            if (_audio == null)
            {
                Debug.LogWarning("🎧 Nenhum PlayerAudioController encontrado.");
                return;
            }

            Debug.Log("=== AUDIO STATUS ===");
            Debug.Log($"Hit Sound Enabled: {_audio.IsHitSoundEnabled}");
            Debug.Log($"Death Sound Enabled: {_audio.IsDeathSoundEnabled}");
            Debug.Log($"Revive Sound Enabled: {_audio.IsReviveSoundEnabled}");
        }
    }
}
