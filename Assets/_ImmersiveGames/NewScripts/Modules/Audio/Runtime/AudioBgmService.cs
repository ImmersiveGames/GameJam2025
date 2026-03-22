using System;
using System.Collections;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Audio.Config;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Runtime
{
    /// <summary>
    /// Runtime canônico de BGM global (single-channel lógico) para F3 do ADR-0028.
    /// Internamente usa duas fontes para suportar crossfade sem concorrência estrutural de BGM.
    /// </summary>
    public sealed class AudioBgmService : MonoBehaviour, IAudioBgmService
    {
        private const string RuntimeObjectName = "NewScripts_AudioBgmRuntime";
        private const float MinFadeSeconds = 0.001f;

        private AudioDefaultsAsset _defaults;
        private IAudioSettingsService _settings;
        private IAudioRoutingResolver _routing;

        private AudioSource _sourceA;
        private AudioSource _sourceB;
        private AudioSource _activeSource;

        private Coroutine _transitionRoutine;
        private TransitionState _activeTransition;
        private int _nextTransitionToken;
        private bool _pauseDuckingEnabled;

        public AudioBgmCueAsset ActiveCue { get; private set; }

        private enum TransitionKind
        {
            None = 0,
            FadeIn = 1,
            Crossfade = 2,
            FadeOut = 3
        }

        private struct TransitionState
        {
            public int Token;
            public TransitionKind Kind;
            public float ConfiguredSeconds;
            public string CueName;
            public string Reason;
            public double QueuedAt;
            public double RuntimeStartedAt;

            public bool IsActive => Token > 0 && Kind != TransitionKind.None;
        }

        public static IAudioBgmService Create(
            AudioDefaultsAsset defaults,
            IAudioSettingsService settings,
            IAudioRoutingResolver routing)
        {
            var runtimeObject = new GameObject(RuntimeObjectName);
            DontDestroyOnLoad(runtimeObject);

            var service = runtimeObject.AddComponent<AudioBgmService>();
            service.Initialize(defaults, settings, routing);

            return service;
        }

        public void Play(AudioBgmCueAsset cue, float fadeInSeconds = -1f, string reason = null)
        {
            if (cue == null)
            {
                DebugUtility.LogWarning(typeof(AudioBgmService),
                    $"[Audio][BGM] Play ignored: cue is null. reason='{SafeReason(reason)}'.");
                return;
            }

            if (!cue.ValidateRuntime(out string validationReason))
            {
                DebugUtility.LogWarning(typeof(AudioBgmService),
                    $"[Audio][BGM] Play ignored: invalid cue '{cue.name}'. validation='{validationReason}'. reason='{SafeReason(reason)}'.");
                return;
            }

            if (!cue.TryPickClip(out var clip) || clip == null)
            {
                DebugUtility.LogWarning(typeof(AudioBgmService),
                    $"[Audio][BGM] Play ignored: cue '{cue.name}' has no playable clip. reason='{SafeReason(reason)}'.");
                return;
            }

            if (_activeSource != null && _activeSource.isPlaying && ActiveCue == cue)
            {
                DebugUtility.LogVerbose(typeof(AudioBgmService),
                    $"[Audio][BGM] Play no-op: cue '{cue.name}' already active.",
                    DebugUtility.Colors.Info);
                return;
            }

            float fade = ResolveFadeSeconds(fadeInSeconds);
            bool hasCurrentPlayback = (_sourceA != null && _sourceA.isPlaying) || (_sourceB != null && _sourceB.isPlaying);

            CancelActiveTransition("interrupted_by_play", reason);

            if (hasCurrentPlayback && fade > MinFadeSeconds)
            {
                var fromSource = ResolveCurrentlyPlayingSource();
                var toSource = GetOtherSource(fromSource);
                StartCrossfade(fromSource, toSource, cue, clip, fade, reason);
                return;
            }

            if (hasCurrentPlayback)
            {
                DebugUtility.LogVerbose(typeof(AudioBgmService),
                    $"[Audio][BGM] Play immediate-switch cue='{cue.name}' requestedFade={fadeInSeconds:0.###} effectiveFade={fade:0.###} reason='{SafeReason(reason)}'.",
                    DebugUtility.Colors.Info);
            }

            var targetSource = ResolvePlayableSource();
            if (targetSource == null)
            {
                DebugUtility.LogError(typeof(AudioBgmService),
                    "[Audio][BGM] Play failed: no audio source available.");
                return;
            }

            ConfigureSourceForCue(targetSource, cue, clip);

            ActiveCue = cue;
            _activeSource = targetSource;

            if (fade > MinFadeSeconds)
            {
                targetSource.volume = 0f;
                targetSource.Play();
                int transitionToken = BeginTransition(TransitionKind.FadeIn, cue.name, fade, reason);
                _transitionRoutine = StartCoroutine(FadeInRoutine(targetSource, cue, fade, transitionToken));
                DebugUtility.LogVerbose(typeof(AudioBgmService),
                    $"[Audio][BGM] FadeIn start token={transitionToken} cue='{cue.name}' requested={fadeInSeconds:0.###} effective={fade:0.###} reason='{SafeReason(reason)}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            targetSource.volume = ComputeCueTargetVolume(cue);
            targetSource.Play();

            DebugUtility.LogVerbose(typeof(AudioBgmService),
                $"[Audio][BGM] Play cue='{cue.name}' clip='{clip.name}' reason='{SafeReason(reason)}'.",
                DebugUtility.Colors.Info);
        }

        public void Stop(float fadeOutSeconds = -1f, string reason = null)
        {
            if (!HasAnyPlayback())
            {
                DebugUtility.LogVerbose(typeof(AudioBgmService),
                    $"[Audio][BGM] Stop no-op: no active playback. reason='{SafeReason(reason)}'.",
                    DebugUtility.Colors.Info);
                ActiveCue = null;
                return;
            }

            float fade = ResolveStopFadeSeconds(fadeOutSeconds);
            CancelActiveTransition("interrupted_by_stop", reason);

            if (fade <= MinFadeSeconds)
            {
                DebugUtility.LogVerbose(typeof(AudioBgmService),
                    $"[Audio][BGM] Stop requested as no-fade. Routing to StopImmediate. requested={fadeOutSeconds:0.###} effective={fade:0.###} reason='{SafeReason(reason)}'.",
                    DebugUtility.Colors.Info);
                StopImmediate(reason);
                return;
            }

            int transitionToken = BeginTransition(TransitionKind.FadeOut, ActiveCue != null ? ActiveCue.name : "none", fade, reason);
            _transitionRoutine = StartCoroutine(FadeOutAllRoutine(fade, transitionToken));

            DebugUtility.LogVerbose(typeof(AudioBgmService),
                $"[Audio][BGM] FadeOut start token={transitionToken} requested={fadeOutSeconds:0.###} effective={fade:0.###} reason='{SafeReason(reason)}'.",
                DebugUtility.Colors.Info);
        }

        public void StopImmediate(string reason = null)
        {
            CancelActiveTransition("interrupted_by_stop_immediate", reason);
            StopAndResetSource(_sourceA);
            StopAndResetSource(_sourceB);

            ActiveCue = null;
            _activeSource = _sourceA != null ? _sourceA : _sourceB;

            DebugUtility.LogVerbose(typeof(AudioBgmService),
                $"[Audio][BGM] StopImmediate reason='{SafeReason(reason)}'.",
                DebugUtility.Colors.Info);
        }

        public void SetPauseDucking(bool paused, string reason = null)
        {
            if (_pauseDuckingEnabled == paused)
            {
                return;
            }

            _pauseDuckingEnabled = paused;
            ApplyCurrentDuckingImmediately();

            DebugUtility.LogVerbose(typeof(AudioBgmService),
                paused
                    ? $"[Audio][BGM] Pause (ducking) applied reason='{SafeReason(reason)}'."
                    : $"[Audio][BGM] Resume (ducking removed) reason='{SafeReason(reason)}'.",
                DebugUtility.Colors.Info);
        }

        private void OnDestroy()
        {
            CancelActiveTransition("runtime_destroyed", "lifecycle_on_destroy");
            StopAndResetSource(_sourceA);
            StopAndResetSource(_sourceB);
            ActiveCue = null;
        }

        private void Initialize(AudioDefaultsAsset defaults, IAudioSettingsService settings, IAudioRoutingResolver routing)
        {
            _defaults = defaults;
            _settings = settings ?? new AudioSettingsService(1f, 1f, 1f, 1f, 1f);
            _routing = routing ?? new AudioRoutingResolver(defaults);

            _sourceA = CreateConfiguredSource("BgmSource_A");
            _sourceB = CreateConfiguredSource("BgmSource_B");
            _activeSource = _sourceA;

            if (defaults == null)
            {
                DebugUtility.LogWarning(typeof(AudioBgmService),
                    "[Audio][BGM] AudioDefaultsAsset missing; runtime uses safe fallback values.");
            }

            DebugUtility.LogVerbose(typeof(AudioBgmService),
                "[Audio][BOOT] IAudioBgmService runtime created (F3, single-channel global BGM).",
                DebugUtility.Colors.Info);
        }

        private AudioSource CreateConfiguredSource(string sourceName)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);

            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.reverbZoneMix = 0f;
            source.volume = 0f;

            return source;
        }

        private void StartCrossfade(AudioSource fromSource, AudioSource toSource, AudioBgmCueAsset cue, AudioClip clip, float fadeSeconds, string reason)
        {
            if (fromSource == null)
            {
                fromSource = ResolveCurrentlyPlayingSource();
            }

            if (toSource == null)
            {
                toSource = GetOtherSource(fromSource);
            }

            ConfigureSourceForCue(toSource, cue, clip);
            toSource.volume = 0f;
            toSource.Play();

            _activeSource = toSource;
            ActiveCue = cue;
            int transitionToken = BeginTransition(TransitionKind.Crossfade, cue.name, fadeSeconds, reason);
            _transitionRoutine = StartCoroutine(CrossfadeRoutine(fromSource, toSource, cue, fadeSeconds, transitionToken));

            DebugUtility.LogVerbose(typeof(AudioBgmService),
                $"[Audio][BGM] Crossfade start token={transitionToken} cue='{cue.name}' fade={fadeSeconds:0.###} reason='{SafeReason(reason)}'.",
                DebugUtility.Colors.Info);
        }

        private IEnumerator FadeInRoutine(AudioSource source, AudioBgmCueAsset cue, float fadeSeconds, int transitionToken)
        {
            MarkTransitionRuntimeStarted(transitionToken);
            double elapsed = 0d;

            while (elapsed < fadeSeconds)
            {
                if (!IsTransitionStillValid(transitionToken))
                {
                    yield break;
                }

                if (source == null || !source.isActiveAndEnabled)
                {
                    FinalizeTransition(transitionToken, "aborted", "fadein_source_invalid");
                    yield break;
                }

                double frameDelta = Math.Max(0d, Time.unscaledDeltaTime);
                elapsed = Math.Min(fadeSeconds, elapsed + frameDelta);
                float t = Mathf.Clamp01((float)(elapsed / fadeSeconds));
                source.volume = Mathf.Lerp(0f, ComputeCueTargetVolume(cue), t);
                yield return null;
            }

            if (!IsTransitionStillValid(transitionToken))
            {
                yield break;
            }

            source.volume = ComputeCueTargetVolume(cue);
            FinalizeTransition(transitionToken, "complete", "normal", elapsed);
        }

        private IEnumerator CrossfadeRoutine(AudioSource fromSource, AudioSource toSource, AudioBgmCueAsset targetCue, float fadeSeconds, int transitionToken)
        {
            MarkTransitionRuntimeStarted(transitionToken);
            float fromStartVolume = fromSource != null ? fromSource.volume : 0f;
            double elapsed = 0d;

            while (elapsed < fadeSeconds)
            {
                if (!IsTransitionStillValid(transitionToken))
                {
                    yield break;
                }

                if (toSource == null || !toSource.isActiveAndEnabled)
                {
                    FinalizeTransition(transitionToken, "aborted", "crossfade_target_invalid");
                    yield break;
                }

                double frameDelta = Math.Max(0d, Time.unscaledDeltaTime);
                elapsed = Math.Min(fadeSeconds, elapsed + frameDelta);
                float t = Mathf.Clamp01((float)(elapsed / fadeSeconds));

                if (fromSource != null)
                {
                    fromSource.volume = Mathf.Lerp(fromStartVolume, 0f, t);
                }

                if (toSource != null)
                {
                    float targetVolume = ComputeCueTargetVolume(targetCue);
                    toSource.volume = Mathf.Lerp(0f, targetVolume, t);
                }

                yield return null;
            }

            if (!IsTransitionStillValid(transitionToken))
            {
                yield break;
            }

            if (fromSource != null)
            {
                StopAndResetSource(fromSource);
            }

            if (toSource != null)
            {
                toSource.volume = ComputeCueTargetVolume(targetCue);
            }

            _activeSource = toSource;
            FinalizeTransition(transitionToken, "complete", "normal", elapsed);
        }

        private IEnumerator FadeOutAllRoutine(float fadeSeconds, int transitionToken)
        {
            MarkTransitionRuntimeStarted(transitionToken);
            float sourceAStart = _sourceA != null ? _sourceA.volume : 0f;
            float sourceBStart = _sourceB != null ? _sourceB.volume : 0f;
            double elapsed = 0d;

            while (elapsed < fadeSeconds)
            {
                if (!IsTransitionStillValid(transitionToken))
                {
                    yield break;
                }

                if (!HasAnyPlayback())
                {
                    FinalizeTransition(transitionToken, "aborted", "fadeout_no_playing_source");
                    yield break;
                }

                double frameDelta = Math.Max(0d, Time.unscaledDeltaTime);
                elapsed = Math.Min(fadeSeconds, elapsed + frameDelta);
                float t = Mathf.Clamp01((float)(elapsed / fadeSeconds));

                if (_sourceA != null && _sourceA.isPlaying)
                {
                    _sourceA.volume = Mathf.Lerp(sourceAStart, 0f, t);
                }

                if (_sourceB != null && _sourceB.isPlaying)
                {
                    _sourceB.volume = Mathf.Lerp(sourceBStart, 0f, t);
                }

                yield return null;
            }

            StopAndResetSource(_sourceA);
            StopAndResetSource(_sourceB);
            ActiveCue = null;
            _activeSource = _sourceA != null ? _sourceA : _sourceB;
            FinalizeTransition(transitionToken, "complete", "normal", elapsed);
        }

        private void ConfigureSourceForCue(AudioSource source, AudioBgmCueAsset cue, AudioClip clip)
        {
            if (source == null || cue == null)
            {
                return;
            }

            source.Stop();
            source.clip = clip;
            source.loop = cue.Loop;
            source.pitch = UnityEngine.Random.Range(cue.PitchMin, cue.PitchMax);
            source.outputAudioMixerGroup = ResolveMixerGroup(cue);
        }

        private UnityEngine.Audio.AudioMixerGroup ResolveMixerGroup(AudioBgmCueAsset cue)
        {
            if (_routing != null)
            {
                return _routing.ResolveBgmMixerGroup(cue);
            }

            return cue != null ? cue.MixerGroup : null;
        }

        private void ApplyCurrentDuckingImmediately()
        {
            if (ActiveCue == null)
            {
                return;
            }

            float targetVolume = ComputeCueTargetVolume(ActiveCue);

            if (_activeSource != null && _activeSource.isPlaying)
            {
                _activeSource.volume = targetVolume;
            }

            var secondary = GetOtherSource(_activeSource);
            if (secondary != null && secondary.isPlaying)
            {
                secondary.volume = Mathf.Min(secondary.volume, targetVolume);
            }
        }

        private float ComputeCueTargetVolume(AudioBgmCueAsset cue)
        {
            if (cue == null)
            {
                return 0f;
            }

            float baseVolume = Mathf.Clamp01(cue.BaseVolume);
            float master = _settings != null ? Mathf.Clamp01(_settings.MasterVolume) : 1f;
            float bgm = _settings != null ? Mathf.Clamp01(_settings.BgmVolume) : 1f;
            float category = _settings != null ? Mathf.Max(0f, _settings.BgmCategoryMultiplier) : 1f;
            float ducking = _pauseDuckingEnabled ? ResolvePauseDuckingScale() : 1f;

            return Mathf.Clamp01(baseVolume * master * bgm * category * ducking);
        }

        private float ResolvePauseDuckingScale()
        {
            if (_defaults == null)
            {
                return 0.35f;
            }

            return Mathf.Clamp01(_defaults.PauseDuckingScale);
        }

        private float ResolveFadeSeconds(float requestedSeconds)
        {
            if (requestedSeconds >= 0f)
            {
                return requestedSeconds;
            }

            if (_defaults == null)
            {
                return 1f;
            }

            return Mathf.Max(0f, _defaults.DefaultBgmFadeSeconds);
        }

        private float ResolveStopFadeSeconds(float requestedSeconds)
        {
            if (requestedSeconds >= 0f)
            {
                return requestedSeconds;
            }

            if (_defaults == null)
            {
                return 1f;
            }

            return Mathf.Max(0f, _defaults.DefaultBgmFadeSeconds);
        }

        private bool HasAnyPlayback()
        {
            return (_sourceA != null && _sourceA.isPlaying) || (_sourceB != null && _sourceB.isPlaying);
        }

        private AudioSource ResolveCurrentlyPlayingSource()
        {
            if (_activeSource != null && _activeSource.isPlaying)
            {
                return _activeSource;
            }

            if (_sourceA != null && _sourceA.isPlaying)
            {
                return _sourceA;
            }

            if (_sourceB != null && _sourceB.isPlaying)
            {
                return _sourceB;
            }

            return _activeSource;
        }

        private AudioSource ResolvePlayableSource()
        {
            if (_activeSource != null)
            {
                return _activeSource;
            }

            if (_sourceA != null)
            {
                return _sourceA;
            }

            return _sourceB;
        }

        private AudioSource GetOtherSource(AudioSource source)
        {
            if (source == _sourceA)
            {
                return _sourceB;
            }

            if (source == _sourceB)
            {
                return _sourceA;
            }

            return _sourceA;
        }

        private int BeginTransition(TransitionKind kind, string cueName, float configuredSeconds, string reason)
        {
            _nextTransitionToken++;
            _activeTransition = new TransitionState
            {
                Token = _nextTransitionToken,
                Kind = kind,
                CueName = string.IsNullOrWhiteSpace(cueName) ? "null" : cueName,
                ConfiguredSeconds = Mathf.Max(0f, configuredSeconds),
                Reason = SafeReason(reason),
                QueuedAt = Time.realtimeSinceStartupAsDouble,
                RuntimeStartedAt = 0d
            };

            return _activeTransition.Token;
        }

        private double MarkTransitionRuntimeStarted(int transitionToken)
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (!IsTransitionStillValid(transitionToken))
            {
                return now;
            }

            if (_activeTransition.RuntimeStartedAt > 0d)
            {
                return _activeTransition.RuntimeStartedAt;
            }

            var updated = _activeTransition;
            updated.RuntimeStartedAt = now;
            _activeTransition = updated;
            return now;
        }

        private bool IsTransitionStillValid(int transitionToken)
        {
            return _activeTransition.IsActive && _activeTransition.Token == transitionToken;
        }

        private void CancelActiveTransition(string cancelCause, string triggerReason)
        {
            if (_transitionRoutine == null || !_activeTransition.IsActive)
            {
                return;
            }

            var snapshot = _activeTransition;
            double elapsed = ComputeElapsedSeconds(snapshot);

            StopCoroutine(_transitionRoutine);
            _transitionRoutine = null;
            _activeTransition = default;

            DebugUtility.LogVerbose(typeof(AudioBgmService),
                $"[Audio][BGM] {snapshot.Kind} canceled token={snapshot.Token} cue='{snapshot.CueName}' configured={snapshot.ConfiguredSeconds:0.###} elapsed={elapsed:0.###} queuedAt={snapshot.QueuedAt:0.###} runtimeStartAt={snapshot.RuntimeStartedAt:0.###} cause='{cancelCause}' transitionReason='{snapshot.Reason}' triggerReason='{SafeReason(triggerReason)}'.",
                DebugUtility.Colors.Info);
        }

        private void FinalizeTransition(int transitionToken, string status, string detail, double? explicitElapsedSeconds = null)
        {
            if (!IsTransitionStillValid(transitionToken))
            {
                return;
            }

            var snapshot = _activeTransition;
            double elapsed = explicitElapsedSeconds.HasValue
                ? Math.Max(0d, explicitElapsedSeconds.Value)
                : ComputeElapsedSeconds(snapshot);

            _transitionRoutine = null;
            _activeTransition = default;

            DebugUtility.LogVerbose(typeof(AudioBgmService),
                $"[Audio][BGM] {snapshot.Kind} {status} token={snapshot.Token} cue='{snapshot.CueName}' configured={snapshot.ConfiguredSeconds:0.###} elapsed={elapsed:0.###} elapsedMode='{(explicitElapsedSeconds.HasValue ? "coroutine_unscaled" : "runtime_clock")}' queuedAt={snapshot.QueuedAt:0.###} runtimeStartAt={snapshot.RuntimeStartedAt:0.###} detail='{detail}' reason='{snapshot.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private static double ComputeElapsedSeconds(in TransitionState transition)
        {
            double startedAt = transition.RuntimeStartedAt > 0d ? transition.RuntimeStartedAt : transition.QueuedAt;
            return Mathf.Max(0f, (float)(Time.realtimeSinceStartupAsDouble - startedAt));
        }

        private static void StopAndResetSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.Stop();
            source.clip = null;
            source.volume = 0f;
        }

        private static string SafeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "unspecified" : reason;
        }
    }
}

