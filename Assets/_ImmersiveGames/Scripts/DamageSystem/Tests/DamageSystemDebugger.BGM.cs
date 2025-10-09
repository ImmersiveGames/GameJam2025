using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem.Tests
{
    public partial class DamageSystemDebugger
    {
        private IAudioService _audioService;

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
            DebugUtility.LogVerbose<DamageSystemDebugger>($"🎵 BGM iniciado ({testBgmData.clip?.name ?? "None"}) com fade: {fadeDuration}s");
        }

        [ContextMenu("BGM/Test Stop BGM")]
        private void TestStopBGM()
        {
            InitializeAudioServiceForTest();
            if (_audioService == null) return;
            _audioService.StopBGM(fadeDuration);
            DebugUtility.LogVerbose<DamageSystemDebugger>($"🛑 BGM parado com fade: {fadeDuration}s");
        }

        [ContextMenu("BGM/Test Stop BGM No Fade")] // Novo: Teste específico sem fade
        private void TestStopBGMNoFade()
        {
            InitializeAudioServiceForTest();
            if (_audioService == null) return;
            _audioService.StopBGMImmediate();
            DebugUtility.LogVerbose<DamageSystemDebugger>($"🛑 BGM parado imediatamente (sem fade)");
        }

        [ContextMenu("BGM/Test Crossfade BGM")]
        private void TestCrossfadeBGM()
        {
            InitializeAudioServiceForTest();
            if (_audioService == null || testBgmData == null) return;
            _audioService.CrossfadeBGM(testBgmData, fadeDuration);
            DebugUtility.LogVerbose<DamageSystemDebugger>($"🔄 Crossfade BGM para ({testBgmData.clip?.name ?? "None"}) com duração: {fadeDuration}s");
        }

        [ContextMenu("BGM/Set BGM Volume")]
        private void TestSetBGMVolume()
        {
            InitializeAudioServiceForTest();
            if (_audioService == null || testBgmData == null) return;
            _audioService.SetBGMVolume(testBgmData.volume); // Usa volume do Data para convergência (remove duplicação)
            DebugUtility.LogVerbose<DamageSystemDebugger>($"📶 Volume BGM definido para {testBgmData.volume} (do SoundData)");
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
            _audioService.PlayBGM(testBgmData);
            DebugUtility.LogVerbose<DamageSystemDebugger>($"🎵 BGM iniciado sem fade ({testBgmData.clip?.name ?? "None"})");
        }

        [ContextMenu("BGM/Test Check BGM Status")]
        private void TestCheckBGMStatus()
        {
            InitializeAudioServiceForTest();
            if (_audioService == null) return;
            DebugUtility.LogVerbose<DamageSystemDebugger>("=== BGM STATUS (via serviço) ===");
            DebugUtility.LogVerbose<DamageSystemDebugger>("Use AudioManager inspector para detalhes internos; aqui validamos inicialização.");
            DebugUtility.LogVerbose<DamageSystemDebugger>($"Serviço inicializado: {AudioSystemInitializer.IsInitialized()}");
        }
    }
}