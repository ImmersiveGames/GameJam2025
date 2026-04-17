using System;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Core;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace _ImmersiveGames.NewScripts.PreferencesRuntime.Bindings
{
    public enum AudioPreferenceSliderKind
    {
        Master,
        Bgm,
        Sfx,
    }

    /// <summary>
    /// Relay explicito de interacao do Slider.
    ///
    /// Fica no proprio GameObject do Slider para receber os eventos reais de press/release.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Slider))]
    public sealed class AudioPreferencesSliderInteractionRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IEndDragHandler
    {
        [SerializeField] private AudioPreferenceSliderKind sliderKind;

        private AudioPreferencesOptionsBinder _owner;
        private Slider _slider;
        private bool _enableSfxPreviewOnRelease;
        private AudioSfxCueAsset _sfxPreviewCue;
        private bool _interactionActive;
        private bool _releaseHandled;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
            if (_slider == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] Slider ausente no relay de interacao de Audio.");
            }
        }

        public void Configure(
            AudioPreferencesOptionsBinder owner,
            AudioPreferenceSliderKind kind,
            bool enableSfxPreviewOnRelease,
            AudioSfxCueAsset sfxPreviewCue)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            ValidateSliderKind(kind);
            sliderKind = kind;
            _enableSfxPreviewOnRelease = enableSfxPreviewOnRelease;
            _sfxPreviewCue = sfxPreviewCue;

            if (_slider == null)
            {
                _slider = GetComponent<Slider>();
            }

            if (_slider == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] Slider ausente ao configurar o relay de interacao de Audio.");
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_interactionActive)
            {
                return;
            }

            _interactionActive = true;
            _releaseHandled = false;

            DebugUtility.Log(typeof(AudioPreferencesSliderInteractionRelay),
                $"[Preferences] audio slider press slider='{FormatSliderName(sliderKind)}' value={_slider.value:0.###}.",
                DebugUtility.Colors.Info);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            FinalizeInteraction("AudioPreferences/PointerUp");
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            FinalizeInteraction("AudioPreferences/EndDrag");
        }

        private void OnDisable()
        {
            _interactionActive = false;
            _releaseHandled = false;
        }

        private void FinalizeInteraction(string reason)
        {
            if (!_interactionActive || _releaseHandled)
            {
                return;
            }

            _interactionActive = false;
            _releaseHandled = true;

            DebugUtility.Log(typeof(AudioPreferencesSliderInteractionRelay),
                $"[Preferences] audio slider release slider='{FormatSliderName(sliderKind)}' value={_slider.value:0.###}.",
                DebugUtility.Colors.Info);

            if (_owner == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] Binder ausente no relay de interacao de Audio.");
            }

            _owner.TryCommitAudioPreferences(FieldHintFor(sliderKind), reason);

            TryPlaySfxPreview(reason);
        }

        private void TryPlaySfxPreview(string reason)
        {
            if (sliderKind != AudioPreferenceSliderKind.Sfx)
            {
                DebugUtility.LogVerbose(typeof(AudioPreferencesSliderInteractionRelay),
                    $"[Preferences] preview skipped reason='field_not_sfx' slider='{FormatSliderName(sliderKind)}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!_enableSfxPreviewOnRelease)
            {
                DebugUtility.LogVerbose(typeof(AudioPreferencesSliderInteractionRelay),
                    "[Preferences] preview skipped reason='preview_disabled' slider='Sfx'.",
                    DebugUtility.Colors.Info);
                return;
            }

            DebugUtility.LogVerbose(typeof(AudioPreferencesSliderInteractionRelay),
                $"[Preferences] preview requested slider='Sfx' cue='{(_sfxPreviewCue != null ? _sfxPreviewCue.name : "null")}' globalAudioResolved='{TryResolveGlobalAudioService(out _)}'.",
                DebugUtility.Colors.Info);

            if (_sfxPreviewCue == null)
            {
                DebugUtility.LogWarning(typeof(AudioPreferencesSliderInteractionRelay),
                    "[Preferences] preview skipped reason='cue_null' slider='Sfx'.");
                return;
            }

            if (!TryResolveGlobalAudioService(out var globalAudioService) || globalAudioService == null)
            {
                DebugUtility.LogWarning(typeof(AudioPreferencesSliderInteractionRelay),
                    "[Preferences] preview skipped reason='global_audio_service_missing' slider='Sfx'.");
                return;
            }

            DebugUtility.Log(typeof(AudioPreferencesSliderInteractionRelay),
                $"[Preferences] preview play dispatch slider='Sfx' cue='{_sfxPreviewCue.name}'.",
                DebugUtility.Colors.Info);

            globalAudioService.Play(
                _sfxPreviewCue,
                AudioPlaybackContext.Global(reason: "Preferences/SfxPreview", volumeScale: 1f));
        }

        private static bool TryResolveGlobalAudioService(out IGlobalAudioService globalAudioService)
        {
            globalAudioService = null;

            if (!DependencyManager.HasInstance || DependencyManager.Provider == null)
            {
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGlobalAudioService>(out globalAudioService) || globalAudioService == null)
            {
                globalAudioService = null;
                return false;
            }

            return true;
        }

        private void ValidateSliderKind(AudioPreferenceSliderKind kind)
        {
            if (!Enum.IsDefined(typeof(AudioPreferenceSliderKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "[FATAL][Preferences] sliderKind invalido no relay de interacao de Audio.");
            }
        }

        private void OnValidate()
        {
            if (!Enum.IsDefined(typeof(AudioPreferenceSliderKind), sliderKind))
            {
                throw new InvalidOperationException("[FATAL][Preferences] sliderKind invalido no relay de interacao de Audio.");
            }
        }

        private static string FieldHintFor(AudioPreferenceSliderKind kind)
        {
            switch (kind)
            {
                case AudioPreferenceSliderKind.Master:
                    return "MasterVolume";
                case AudioPreferenceSliderKind.Bgm:
                    return "BgmVolume";
                case AudioPreferenceSliderKind.Sfx:
                    return "SfxVolume";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "sliderKind invalido.");
            }
        }

        private static string FormatSliderName(AudioPreferenceSliderKind kind)
        {
            switch (kind)
            {
                case AudioPreferenceSliderKind.Master:
                    return "Master";
                case AudioPreferenceSliderKind.Bgm:
                    return "Bgm";
                case AudioPreferenceSliderKind.Sfx:
                    return "Sfx";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "sliderKind invalido.");
            }
        }
    }
}

