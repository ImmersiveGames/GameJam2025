using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.AudioSystem.Core;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.Scripts.AudioSystem.Components;
using UnityEngine;
namespace _ImmersiveGames.Scripts.AudioSystem.Tests
{
    /// <summary>
    /// Auditor em runtime para o sistema de �udio.
    /// 
    /// Mostra:
    /// - Estado do BGM (clip atual, volume, playing/paused).
    /// - Estat�sticas de SFX (quantidade de SoundEmitter, quantos tocando).
    /// - Lista resumida dos SFX ativos (nome do objeto, clip, posi��o).
    /// 
    /// Use a tecla configurada (F9 por padr�o) para ligar/desligar o overlay.
    /// Opcionalmente, pode registrar um resumo peri�dico no Console.
    /// </summary>
    public class AudioRuntimeDiagnostics : MonoBehaviour
    {
        [Header("Overlay")]
        [Tooltip("Overlay de debug vis�vel em runtime.")]
        public bool overlayEnabled = true;

        [Tooltip("Tecla para ligar/desligar overlay.")]
        public KeyCode toggleOverlayKey = KeyCode.F9;

        [Tooltip("Atualiza��o da coleta de dados (segundos).")]
        [Range(0.1f, 3f)]
        public float updateInterval = 0.5f;

        [Tooltip("M�ximo de emitters listados no overlay.")]
        [Range(3, 32)]
        public int maxEmittersToList = 12;

        [Header("Log Peri�dico (opcional)")]
        [Tooltip("Se verdadeiro, registra resumo no Console periodicamente.")]
        public bool logSummaryPeriodically;

        [Tooltip("Intervalo para log peri�dico (segundos).")]
        [Range(1f, 60f)]
        public float logInterval = 10f;

        [Header("Posi��o do Overlay")]
        public Vector2 overlayPosition = new Vector2(10, 10);
        public Vector2 overlaySize = new Vector2(420, 260);

        private IAudioSfxService _sfxService;
        private IBgmAudioService _bgmAudioService;
        private GlobalBgmAudioService _globalBgmAudioService; // para acesso ao AudioSource de BGM

        private float _updateTimer;
        private float _logTimer;

        // snapshot
        private string _bgmStatus = "N/A";
        private float _bgmVolume;
        private bool _bgmIsPlaying;
        private bool _bgmIsPaused;

        private int _sfxEmitterTotal;
        private int _sfxEmitterPlaying;

        private readonly List<EmitterInfo> _emittersSnapshot = new List<EmitterInfo>();

        [Serializable]
        private struct EmitterInfo
        {
            public string name;
            public string clip;
            public bool isPlaying;
            public Vector3 position;
        }

        private void Awake()
        {
            AudioSystemBootstrap.EnsureAudioSystemInitialized();

            if (DependencyManager.Provider != null)
            {
                DependencyManager.Provider.TryGetGlobal(out _sfxService);
                DependencyManager.Provider.TryGetGlobal(out _bgmAudioService);
                DependencyManager.Provider.TryGetGlobal(out _globalBgmAudioService);
            }

            if (_sfxService == null)
            {
                DebugUtility.LogWarning<AudioRuntimeDiagnostics>(
                    "[AudioDiagnostics] IAudioSfxService n�o encontrado. Estat�sticas de SFX ficar�o limitadas.");
            }

            if (_bgmAudioService == null)
            {
                DebugUtility.LogWarning<AudioRuntimeDiagnostics>(
                    "[AudioDiagnostics] IBgmAudioService (BGM) n�o encontrado. Estado de BGM ficar� limitado.");
            }

            if (_globalBgmAudioService == null)
            {
                DebugUtility.LogVerbose<AudioRuntimeDiagnostics>(
                    "[AudioDiagnostics] GlobalBgmAudioService concreto n�o encontrado via DI. " +
                    "BGM ser� inspecionado apenas de forma superficial.",
                    DebugUtility.Colors.Info);
            }
        }

        private void Update()
        {
            HandleOverlayToggle();
            HandleSnapshotUpdate();
            HandlePeriodicLog();
        }

        private void HandleOverlayToggle()
        {
            if (Input.GetKeyDown(toggleOverlayKey))
            {
                overlayEnabled = !overlayEnabled;
            }
        }

        private void HandleSnapshotUpdate()
        {
            _updateTimer += Time.unscaledDeltaTime;
            if (_updateTimer >= updateInterval)
            {
                _updateTimer = 0f;
                RefreshSnapshot();
            }
        }

        private void HandlePeriodicLog()
        {
            if (!logSummaryPeriodically) return;

            _logTimer += Time.unscaledDeltaTime;
            if (_logTimer >= logInterval)
            {
                _logTimer = 0f;
                LogSummaryToConsole();
            }
        }

        private void RefreshSnapshot()
        {
            RefreshBgmSnapshot();
            RefreshSfxSnapshot();
        }

        private void RefreshBgmSnapshot()
        {
            ResetBgmSnapshotDefaults();

            var bgmSource = GetPrimaryBgmSource() ?? FindFallbackBgmSource();
            if (bgmSource == null)
            {
                _bgmStatus = "Nenhuma fonte de BGM detectada";
                return;
            }

            UpdateBgmSnapshotFromSource(bgmSource);
        }

        private void ResetBgmSnapshotDefaults()
        {
            _bgmStatus = "N/A";
            _bgmIsPlaying = false;
            _bgmIsPaused = false;
            _bgmVolume = 0f;
        }

        private AudioSource GetPrimaryBgmSource()
        {
            if (_globalBgmAudioService != null && _globalBgmAudioService.BgmAudioSource != null)
            {
                return _globalBgmAudioService.BgmAudioSource;
            }

            return null;
        }

        private AudioSource FindFallbackBgmSource()
        {
            // Fallback: tenta encontrar qualquer AudioSource com tag/nome sugestivo,
            // mas sem inventar muita l�gica. Isso � apenas um fallback de debug.
            AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            return allSources.Where(src => src.isActiveAndEnabled).FirstOrDefault(src => src.loop && src.clip != null);

        }

        private void UpdateBgmSnapshotFromSource(AudioSource bgmSource)
        {
            string clipName = bgmSource.clip != null ? bgmSource.clip.name : "(sem clip)";
            _bgmIsPlaying = bgmSource.isPlaying;
            _bgmVolume = bgmSource.volume;

            // Unity n�o exp�e diretamente "isPaused", ent�o inferimos:
            _bgmIsPaused = !_bgmIsPlaying && bgmSource.time > 0f && bgmSource.time < bgmSource.clip.length;

            _bgmStatus = $"{clipName} | Vol={_bgmVolume:0.00} | " +
                         (_bgmIsPlaying ? "Playing" : _bgmIsPaused ? "Paused" : "Stopped");
        }

        private void RefreshSfxSnapshot()
        {
            _emittersSnapshot.Clear();

            SoundEmitter[] emitters = FindObjectsByType<SoundEmitter>(FindObjectsSortMode.None);
            _sfxEmitterTotal = emitters.Length;
            _sfxEmitterPlaying = 0;

            int listed = 0;
            foreach (var emitter in emitters)
            {
                if (emitter == null || !emitter.isActiveAndEnabled) continue;

                var src = emitter.GetComponent<AudioSource>();
                if (src == null) continue;

                bool playing = src.isPlaying;
                if (playing) _sfxEmitterPlaying++;

                if (listed < maxEmittersToList)
                {
                    listed++;
                    _emittersSnapshot.Add(new EmitterInfo
                    {
                        name = emitter.gameObject.name,
                        clip = src.clip != null ? src.clip.name : "(sem clip)",
                        isPlaying = playing,
                        position = emitter.transform.position
                    });
                }
            }
        }

        private void LogSummaryToConsole()
        {
            DebugUtility.Log<AudioRuntimeDiagnostics>(
                $"[AudioDiagnostics] BGM: {_bgmStatus} | SFX: {_sfxEmitterPlaying}/{_sfxEmitterTotal} emitters tocando.",
                DebugUtility.Colors.Info);
        }

        private void OnGUI()
        {
            if (!overlayEnabled) return;

            var rect = new Rect(overlayPosition.x, overlayPosition.y, overlaySize.x, overlaySize.y);
            GUI.depth = 0;

            GUI.Box(rect, GUIContent.none);

            GUILayout.BeginArea(rect);
            GUILayout.BeginVertical();

            GUILayout.Label("<b><size=14>Audio Runtime Diagnostics</size></b>");

            GUILayout.Space(4);
            GUILayout.Label("<b>BGM</b>");
            GUILayout.Label($"Status: {_bgmStatus}");

            GUILayout.Space(4);
            GUILayout.Label("<b>SFX</b>");
            GUILayout.Label($"Emitters: {_sfxEmitterPlaying}/{_sfxEmitterTotal} tocando");

            GUILayout.Space(4);
            GUILayout.Label("<b>Emitters (amostra)</b>");

            if (_emittersSnapshot.Count == 0)
            {
                GUILayout.Label("Nenhum SoundEmitter ativo.");
            }
            else
            {
                foreach (var info in _emittersSnapshot)
                {
                    GUILayout.Label(
                        $"- {info.name} | Clip={info.clip} | " +
                        $"{(info.isPlaying ? "Playing" : "Idle")} | Pos=({info.position.x:0.0}, {info.position.y:0.0}, {info.position.z:0.0})"
                    );
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label($"Toggle: {toggleOverlayKey} | Update: {updateInterval:0.00}s");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}

