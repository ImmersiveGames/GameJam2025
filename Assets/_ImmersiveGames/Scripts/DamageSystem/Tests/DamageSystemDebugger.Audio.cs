// Path: _ImmersiveGames/Scripts/DamageSystem/Tests/DamageSystemDebugger.Audio.cs
using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
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
            if (!ValidateAudio()) return;
            _audio.PlaySound(_audio.AudioConfig?.hitSound, AudioContextMode.Auto, hitVolume);
            Debug.Log($"🔊 Hit sound executado ({_audio.AudioConfig?.hitSound?.clip?.name ?? "None"})");
        }

        [ContextMenu("Audio/Test Death Sound")]
        private void TestDeathSound()
        {
            if (!ValidateAudio()) return;
            _audio.PlaySound(_audio.AudioConfig?.deathSound, AudioContextMode.Auto, deathVolume);
            Debug.Log($"💀 Death sound executado ({_audio.AudioConfig?.deathSound?.clip?.name ?? "None"})");
        }

        [ContextMenu("Audio/Test Revive Sound")]
        private void TestReviveSound()
        {
            if (!ValidateAudio()) return;
            _audio.PlaySound(_audio.AudioConfig?.reviveSound, AudioContextMode.Auto, reviveVolume);
            Debug.Log($"✨ Revive sound executado ({_audio.AudioConfig?.reviveSound?.clip?.name ?? "None"})");
        }

        [ContextMenu("Audio/Print Audio Status")]
        private void PrintAudioStatus()
        {
            if (!ValidateAudio()) return;

            Debug.Log("=== AUDIO STATUS ===");
            Debug.Log($"Hit: {_audio.AudioConfig?.hitSound?.clip?.name ?? "None"}");
            Debug.Log($"Death: {_audio.AudioConfig?.deathSound?.clip?.name ?? "None"}");
            Debug.Log($"Revive: {_audio.AudioConfig?.reviveSound?.clip?.name ?? "None"}");
        }

        private bool ValidateAudio()
        {
            if (_audio == null)
            {
                Debug.LogWarning("🎧 Nenhum PlayerAudioController encontrado.");
                return false;
            }
            return true;
        }
    }
}
