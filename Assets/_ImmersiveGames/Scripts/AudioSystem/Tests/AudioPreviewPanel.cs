using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Core;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.Scripts.AudioSystem.System;
using UnityEngine;
namespace _ImmersiveGames.Scripts.AudioSystem.Tests
{
    /// <summary>
    /// Painel simples de preview de �udio em runtime.
    /// 
    /// - Use arrays de SoundData (SFX e BGM) configurados no Inspector.
    /// - Permite selecionar e tocar SFX via IAudioSfxService.
    /// - Permite selecionar e tocar BGM via IBgmAudioService.
    /// 
    /// Controles:
    /// - Tecla F10: liga/desliga o painel.
    /// - Bot�es na UI: Anterior / Play / Pr�ximo.
    /// </summary>
    public class AudioPreviewPanel : MonoBehaviour
    {
        [Header("Ativa��o")]
        public bool panelVisible = true;
        public KeyCode toggleKey = KeyCode.F10;

        [Header("SFX Preview")]
        public SoundData[] sfxClips;

        [Tooltip("Fade-in em segundos para preview de SFX.")]
        [Range(0f, 2f)]
        public float sfxFadeIn = 0.1f;

        [Tooltip("Volume multiplier para preview de SFX (multiplicador de contexto).")]
        [Range(0.1f, 2f)]
        public float sfxVolumeMultiplier = 1f;

        [Header("BGM Preview")]
        public SoundData[] bgmClips;

        [Tooltip("Fade-in em segundos para preview de BGM.")]
        [Range(0f, 3f)]
        public float bgmFadeIn = 1f;

        [Tooltip("Fade-out em segundos ao parar BGM.")]
        [Range(0f, 3f)]
        public float bgmFadeOut = 1f;

        private int _sfxIndex;
        private int _bgmIndex;

        private IAudioSfxService _sfxService;
        private IBgmAudioService _bgmAudioService;

        private void Awake()
        {
            AudioSystemBootstrap.EnsureAudioSystemInitialized();

            if (DependencyManager.Provider != null)
            {
                DependencyManager.Provider.TryGetGlobal(out _sfxService);
                DependencyManager.Provider.TryGetGlobal(out _bgmAudioService);
            }

            if (_sfxService == null)
            {
                DebugUtility.LogWarning<AudioPreviewPanel>(
                    "[AudioPreview] IAudioSfxService n�o encontrado. Preview de SFX indispon�vel.");
            }

            if (_bgmAudioService == null)
            {
                DebugUtility.LogWarning<AudioPreviewPanel>(
                    "[AudioPreview] IBgmAudioService (BGM) n�o encontrado. Preview de BGM indispon�vel.");
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                panelVisible = !panelVisible;
            }
        }

        private void OnGUI()
        {
            if (!panelVisible) return;

            const float width = 420f;
            const float height = 200f;
            var rect = new Rect(Screen.width - width - 10f, 10f, width, height);

            GUI.Box(rect, GUIContent.none);

            GUILayout.BeginArea(rect);
            GUILayout.BeginVertical();

            GUILayout.Label("<b><size=14>Audio Preview Panel</size></b>");

            GUILayout.Space(4);

            DrawSfxPreviewSection();
            GUILayout.Space(8);
            DrawBgmPreviewSection();

            GUILayout.FlexibleSpace();
            GUILayout.Label($"Toggle: {toggleKey}");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        #region SFX

        private void DrawSfxPreviewSection()
        {
            GUILayout.Label("<b>SFX</b>");

            if (sfxClips == null || sfxClips.Length == 0)
            {
                GUILayout.Label("Nenhum SoundData de SFX configurado.");
                return;
            }

            _sfxIndex = Mathf.Clamp(_sfxIndex, 0, sfxClips.Length - 1);
            var current = sfxClips[_sfxIndex];
            string currentName = current != null ? current.name : "(null)";

            GUILayout.Label($"Selecionado: [{_sfxIndex}] {currentName}");

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("<<", GUILayout.Width(50)))
            {
                _sfxIndex = (_sfxIndex - 1 + sfxClips.Length) % sfxClips.Length;
            }

            GUI.enabled = _sfxService != null && current != null;
            if (GUILayout.Button("Play", GUILayout.Width(80)))
            {
                PlaySfx(current);
            }
            GUI.enabled = true;

            if (GUILayout.Button(">>", GUILayout.Width(50)))
            {
                _sfxIndex = (_sfxIndex + 1) % sfxClips.Length;
            }

            GUILayout.EndHorizontal();
        }

        private void PlaySfx(SoundData sd)
        {
            if (_sfxService == null || sd == null || sd.clip == null)
            {
                DebugUtility.LogWarning<AudioPreviewPanel>(
                    "[AudioPreview][SFX] N�o � poss�vel reproduzir SFX (servi�o ou SoundData inv�lido).");
                return;
            }

            // Preview non-spatial, volume controlado por contexto.
            var ctx = AudioContext.NonSpatial(sfxVolumeMultiplier);
            _sfxService.PlayOneShot(sd, ctx, sfxFadeIn);

            DebugUtility.LogVerbose<AudioPreviewPanel>(
                $"[AudioPreview][SFX] PlayOneShot: {sd.name} (volMult={sfxVolumeMultiplier:0.00}, fadeIn={sfxFadeIn:0.00}s)",
                DebugUtility.Colors.Info);
        }

        #endregion

        #region BGM

        private void DrawBgmPreviewSection()
        {
            GUILayout.Label("<b>BGM</b>");

            if (bgmClips == null || bgmClips.Length == 0)
            {
                GUILayout.Label("Nenhum SoundData de BGM configurado.");
                return;
            }

            _bgmIndex = Mathf.Clamp(_bgmIndex, 0, bgmClips.Length - 1);
            var current = bgmClips[_bgmIndex];
            string currentName = current != null ? current.name : "(null)";

            GUILayout.Label($"Selecionado: [{_bgmIndex}] {currentName}");

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("<<", GUILayout.Width(50)))
            {
                _bgmIndex = (_bgmIndex - 1 + bgmClips.Length) % bgmClips.Length;
            }

            GUI.enabled = _bgmAudioService != null && current != null;
            if (GUILayout.Button("Play (Loop)", GUILayout.Width(100)))
            {
                PlayBgm(current);
            }

            if (GUILayout.Button("Stop", GUILayout.Width(60)))
            {
                StopBgm();
            }
            GUI.enabled = true;

            if (GUILayout.Button(">>", GUILayout.Width(50)))
            {
                _bgmIndex = (_bgmIndex + 1) % bgmClips.Length;
            }

            GUILayout.EndHorizontal();
        }

        private void PlayBgm(SoundData sd)
        {
            if (_bgmAudioService == null || sd == null || sd.clip == null)
            {
                DebugUtility.LogWarning<AudioPreviewPanel>(
                    "[AudioPreview][BGM] N�o � poss�vel reproduzir BGM (servi�o ou SoundData inv�lido).");
                return;
            }

            _bgmAudioService.PlayBGM(sd, loop: true, fadeInDuration: bgmFadeIn);

            DebugUtility.LogVerbose<AudioPreviewPanel>(
                $"[AudioPreview][BGM] PlayBGM: {sd.name} (loop=ON, fadeIn={bgmFadeIn:0.00}s)",
                DebugUtility.Colors.Info);
        }

        private void StopBgm()
        {
            if (_bgmAudioService == null)
                return;

            _bgmAudioService.StopBGM(bgmFadeOut);

            DebugUtility.LogVerbose<AudioPreviewPanel>(
                $"[AudioPreview][BGM] StopBGM (fadeOut={bgmFadeOut:0.00}s)",
                DebugUtility.Colors.Info);
        }

        #endregion
    }
}

