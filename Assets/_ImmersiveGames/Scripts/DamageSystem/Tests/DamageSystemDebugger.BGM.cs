// Path: _ImmersiveGames/Scripts/DamageSystem/Tests/DamageSystemDebugger.BGM.cs
using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem.Tests
{
    public partial class DamageSystemDebugger
    {
        private IAudioService _audioService; // Obtido via initializer para testes globais

        private void InitializeAudioServiceForTest()
        {
            if (_audioService == null)
            {
                AudioSystemInitializer.EnsureAudioSystemInitialized();
                _audioService = AudioSystemInitializer.GetAudioService();
            }
        }

        [ContextMenu("BGM/Test Play BGM")]
        private void TestPlayBGM()
        {
            InitializeAudioServiceForTest();
            if (_audioService == null || testBgmData == null) 
            {
                Debug.LogWarning("🎵 Nenhum AudioService ou BGM Data encontrado para teste.");
                return;
            }
            _audioService.PlayBGM(testBgmData, true, fadeDuration);
            Debug.Log($"🎵 BGM iniciado ({testBgmData.clip?.name ?? "None"}) com fade: {fadeDuration}s");
        }

        [ContextMenu("BGM/Test Stop BGM")]
        private void TestStopBGM()
        {
            InitializeAudioServiceForTest();
            if (_audioService == null) return;
            _audioService.StopBGM(fadeDuration);
            Debug.Log($"🛑 BGM parado com fade: {fadeDuration}s");
        }

        [ContextMenu("BGM/Test Crossfade BGM")]
        private void TestCrossfadeBGM()
        {
            InitializeAudioServiceForTest();
            if (_audioService == null || testBgmData == null) return;
            _audioService.CrossfadeBGM(testBgmData, fadeDuration);
            Debug.Log($"🔄 Crossfade BGM para ({testBgmData.clip?.name ?? "None"}) com duração: {fadeDuration}s");
        }

        [ContextMenu("BGM/Set BGM Volume")]
        private void TestSetBGMVolume()
        {
            InitializeAudioServiceForTest();
            if (_audioService == null) return;
            _audioService.SetBGMVolume(bgmVolume);
            Debug.Log($"📶 Volume BGM definido para {bgmVolume}");
        }
        [ContextMenu("BGM/Test Play BGM No Fade")]
        private void TestPlayBGMNoFade()
        {
            InitializeAudioServiceForTest();
            if (_audioService == null || testBgmData == null) 
            {
                Debug.LogWarning("🎵 Nenhum AudioService ou BGM Data encontrado para teste.");
                return;
            }
            _audioService.PlayBGM(testBgmData, true, 0f); // Sem fade para isolar
            Debug.Log($"🎵 BGM iniciado sem fade ({testBgmData.clip?.name ?? "None"})");
        }

        [ContextMenu("BGM/Test Check BGM Source Status")]
        private void TestCheckBGMSourceStatus()
        {
            InitializeAudioServiceForTest();
            if (_audioService == null) return;

            var manager = _audioService as AudioManager; // Cast para acesso debug (apenas teste)
            if (manager == null) 
            {
                Debug.LogWarning("Não conseguiu cast para AudioManager.");
                return;
            }

            Debug.Log("=== BGM SOURCE STATUS ===");
            Debug.Log($"Clip: {manager.bgmAudioSource.clip?.name ?? "None"}");
            Debug.Log($"Playing: {manager.bgmAudioSource.isPlaying}");
            Debug.Log($"Volume: {manager.bgmAudioSource.volume}");
            Debug.Log($"Loop: {manager.bgmAudioSource.loop}");
            Debug.Log($"MixerGroup: {manager.bgmAudioSource.outputAudioMixerGroup?.name ?? "None"}");
        }
    }
}