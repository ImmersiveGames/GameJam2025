using System;
using System.Collections;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Runtime
{
    /// <summary>
    /// Handle runtime de playback direto de SFX (F4, sem pooling).
    /// </summary>
    public sealed class AudioSfxPlaybackHandle : MonoBehaviour, IAudioPlaybackHandle
    {
        private AudioSource _source;
        private Transform _followTarget;
        private Action<AudioSfxPlaybackHandle, int, string, string, string> _onCompleted;
        private Coroutine _stopRoutine;

        private int _cueId;
        private string _cueName;
        private string _modeLabel;
        private string _reason;
        private bool _isValid;
        private bool _isStopping;

        public bool IsValid => _isValid;
        public bool IsPlaying => _isValid && _source != null && _source.isPlaying;

        public void Initialize(
            int cueId,
            string cueName,
            AudioSource source,
            Transform followTarget,
            string modeLabel,
            string reason,
            Action<AudioSfxPlaybackHandle, int, string, string, string> onCompleted)
        {
            _cueId = cueId;
            _cueName = string.IsNullOrWhiteSpace(cueName) ? "<unknown>" : cueName.Trim();
            _source = source;
            _followTarget = followTarget;
            _modeLabel = string.IsNullOrWhiteSpace(modeLabel) ? "2D" : modeLabel.Trim();
            _reason = string.IsNullOrWhiteSpace(reason) ? "unspecified" : reason.Trim();
            _onCompleted = onCompleted;
            _isValid = true;
            _isStopping = false;
        }

        public void Stop(float fadeOutSeconds = 0f)
        {
            if (!_isValid)
            {
                return;
            }

            if (_source == null)
            {
                Complete("stop_source_missing");
                return;
            }

            float fade = Mathf.Max(0f, fadeOutSeconds);
            if (fade <= 0f || !_source.isPlaying)
            {
                _source.Stop();
                Complete(fade <= 0f ? "stop_immediate" : "stop_not_playing");
                return;
            }

            _isStopping = true;
            if (_stopRoutine != null)
            {
                StopCoroutine(_stopRoutine);
            }

            _stopRoutine = StartCoroutine(FadeStopRoutine(fade));
            DebugUtility.LogVerbose(typeof(AudioSfxPlaybackHandle),
                $"[Audio][SFX] Stop requested cue='{_cueName}' cueId={_cueId} mode='{_modeLabel}' fade={fade:0.###} reason='{_reason}'.",
                DebugUtility.Colors.Info);
        }

        private void Update()
        {
            if (!_isValid)
            {
                return;
            }

            if (_followTarget != null)
            {
                transform.position = _followTarget.position;
            }

            if (_isStopping)
            {
                return;
            }

            if (_source == null)
            {
                Complete("source_missing");
                return;
            }

            if (!_source.loop && !_source.isPlaying)
            {
                Complete("clip_finished");
            }
        }

        private IEnumerator FadeStopRoutine(float fadeOutSeconds)
        {
            if (_source == null)
            {
                Complete("fade_source_missing");
                yield break;
            }

            float startVolume = _source.volume;
            float elapsed = 0f;

            while (elapsed < fadeOutSeconds)
            {
                if (!_isValid || _source == null)
                {
                    Complete("fade_aborted");
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutSeconds);
                _source.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            if (_source != null)
            {
                _source.Stop();
            }

            Complete("stop_fade_complete");
        }

        private void OnDestroy()
        {
            if (_isValid)
            {
                Complete("destroyed");
            }
        }

        private void Complete(string completionReason)
        {
            if (!_isValid)
            {
                return;
            }

            _isValid = false;
            _isStopping = false;

            if (_source != null)
            {
                _source.Stop();
                _source.clip = null;
            }

            _onCompleted?.Invoke(this, _cueId, _cueName, _modeLabel, completionReason);
            _onCompleted = null;

            Destroy(gameObject);
        }
    }
}
