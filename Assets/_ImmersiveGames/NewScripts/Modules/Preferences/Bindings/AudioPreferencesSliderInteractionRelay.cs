using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _ImmersiveGames.NewScripts.Modules.Preferences.Bindings
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

        public void Configure(AudioPreferencesOptionsBinder owner, AudioPreferenceSliderKind kind)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            ValidateSliderKind(kind);
            sliderKind = kind;

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
