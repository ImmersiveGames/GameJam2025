// Path: _ImmersiveGames/Scripts/DamageSystem/Tests/DamageSystemDebugger.Audio.cs

using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem.Tests
{
    public partial class DamageSystemDebugger
    {
        [ContextMenu("Audio/Test Shoot Sound (Simple)")]
        private void TestShootSoundSimple()
        {
            if (!ValidateAudio()) return;
            _audio.PlayShootSound(null, shootVolume); // Usa método público; valida convergência de volume
            DebugUtility.LogVerbose<DamageSystemDebugger>($"🔫 Shoot sound simples executado ({_audio.GetAudioConfig()?.shootSound?.clip?.name ?? "None"})");
        }

        [ContextMenu("Audio/Test Shoot Sound (Advanced with Builder)")]
        private void TestShootSoundAdvanced()
        {
            if (!ValidateAudio()) return;
            var builder = _audio.CreateSoundBuilderPublic(); // Wrapper público para factory protected
            if (builder == null) 
            {
                Debug.LogWarning("Builder não disponível — verifique pool.");
                return;
            }
            builder.WithSoundData(_audio.GetAudioConfig()?.shootSound)
                   .AtPosition(transform.position)
                   .WithRandomPitch()
                   .WithVolumeMultiplier(shootVolume)
                   .Play(); // Valida builder com configs convergentes
            DebugUtility.LogVerbose<DamageSystemDebugger>($"🔫 Shoot sound avançado executado com builder ({_audio.GetAudioConfig()?.shootSound?.clip?.name ?? "None"})");
        }

        [ContextMenu("Audio/Test Hit Sound")]
        private void TestHitSound()
        {
            if (!ValidateAudio()) return;
            var config = _audio.GetAudioConfig();
            if (config?.hitSound == null) return;
            _audio.TestPlaySoundPublic(config.hitSound, hitVolume); // Wrapper público para PlaySoundLocal
            DebugUtility.LogVerbose<DamageSystemDebugger>($"🔊 Hit sound executado ({config.hitSound.clip?.name ?? "None"})");
        }

        [ContextMenu("Audio/Test Death Sound")]
        private void TestDeathSound()
        {
            if (!ValidateAudio()) return;
            var config = _audio.GetAudioConfig();
            if (config?.deathSound == null) return;
            _audio.TestPlaySoundPublic(config.deathSound, deathVolume);
            DebugUtility.LogVerbose<DamageSystemDebugger>($"💀 Death sound executado ({config.deathSound.clip?.name ?? "None"})");
        }

        [ContextMenu("Audio/Test Revive Sound")]
        private void TestReviveSound()
        {
            if (!ValidateAudio()) return;
            var config = _audio.GetAudioConfig();
            if (config?.reviveSound == null) return;
            _audio.TestPlaySoundPublic(config.reviveSound, reviveVolume);
            DebugUtility.LogVerbose<DamageSystemDebugger>($"✨ Revive sound executado ({config.reviveSound.clip?.name ?? "None"})");
        }

        [ContextMenu("Audio/Print Audio Status")]
        private void PrintAudioStatus()
        {
            if (!ValidateAudio()) return;

            var config = _audio.GetAudioConfig();
            DebugUtility.LogVerbose<DamageSystemDebugger>("=== AUDIO STATUS ===");
            DebugUtility.LogVerbose<DamageSystemDebugger>($"Shoot: {config?.shootSound?.clip?.name ?? "None"} (Volume Mult: {shootVolume})");
            DebugUtility.LogVerbose<DamageSystemDebugger>($"Hit: {config?.hitSound?.clip?.name ?? "None"} (Volume Mult: {hitVolume})");
            DebugUtility.LogVerbose<DamageSystemDebugger>($"Death: {config?.deathSound?.clip?.name ?? "None"} (Volume Mult: {deathVolume})");
            DebugUtility.LogVerbose<DamageSystemDebugger>($"Revive: {config?.reviveSound?.clip?.name ?? "None"} (Volume Mult: {reviveVolume})");
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