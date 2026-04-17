using System;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.PreferencesRuntime.Contracts;
using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.NewScripts.PreferencesRuntime.Bindings
{
    /// <summary>
    /// Binder de intencao para opcoes de audio.
    ///
    /// Regras:
    /// - sincroniza os sliders com o estado canônico ao abrir o painel;
    /// - publica apenas intencao de mudanca;
    /// - não conversa com backend nem com PlayerPrefs diretamente.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioPreferencesOptionsBinder : MonoBehaviour
    {
        [Header("Sliders")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("Behavior")]
        [SerializeField] private bool syncOnEnable = true;

        [Header("SFX Preview")]
        [SerializeField] private bool enableSfxPreviewOnRelease;
        [SerializeField] private AudioSfxCueAsset sfxPreviewCue;

        private IPreferencesStateService _stateService;
        private IPreferencesSaveService _saveService;
        private bool _servicesResolved;
        private bool _listenersRegistered;
        private bool _syncingFromState;

        private void Awake()
        {
            ResolveServicesOrThrow();
            ValidateReferencesOrThrow();
            EnsureInteractionRelays();
        }

        private void OnEnable()
        {
            ResolveServicesOrThrow();
            EnsureInteractionRelays();
            RegisterListeners();

            if (syncOnEnable)
            {
                SyncFromCurrentState("AudioPreferences/OnEnable");
            }
        }

        private void OnDisable()
        {
            UnregisterListeners();
        }

        public void RefreshFromCurrentState(string reason = "AudioPreferences/Refresh")
        {
            SyncFromCurrentState(reason);
        }

        private void OnMasterVolumeChanged(float _)
        {
            PreviewAudioPreferences("AudioPreferences/MasterVolumeChanged");
        }

        private void OnBgmVolumeChanged(float _)
        {
            PreviewAudioPreferences("AudioPreferences/BgmVolumeChanged");
        }

        private void OnSfxVolumeChanged(float _)
        {
            PreviewAudioPreferences("AudioPreferences/SfxVolumeChanged");
        }

        private void PreviewAudioPreferences(string reason)
        {
            if (_syncingFromState)
            {
                return;
            }

            try
            {
                if (_saveService == null)
                {
                    throw new InvalidOperationException("[FATAL][Preferences] IPreferencesSaveService ausente no AudioPreferencesOptionsBinder.");
                }

                _saveService.TryPreviewAudioVolumes(
                    masterVolume: ReadSliderValue(masterVolumeSlider),
                    bgmVolume: ReadSliderValue(bgmVolumeSlider),
                    sfxVolume: ReadSliderValue(sfxVolumeSlider),
                    reason: reason,
                    out bool _);
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        public bool TryCommitAudioPreferences(string fieldHint, string reason)
        {
            try
            {
                if (_saveService == null)
                {
                    throw new InvalidOperationException("[FATAL][Preferences] IPreferencesSaveService ausente no AudioPreferencesOptionsBinder.");
                }

                return _saveService.TryCommitCurrentAudioVolumes(
                    reason: reason,
                    fieldHint: fieldHint,
                    out bool _,
                    out string _);
            }
            catch (Exception ex)
            {
                _ = ex;
                return false;
            }
        }

        public void RestoreAudioDefaults()
        {
            if (_saveService == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] IPreferencesSaveService ausente no AudioPreferencesOptionsBinder.");
            }

            _saveService.TryRestoreAudioDefaults(
                reason: "AudioPreferences/RestoreDefaults",
                out string _);

            SyncFromCurrentState("AudioPreferences/RestoreDefaults");
        }

        private void OnValidate()
        {
            if (enableSfxPreviewOnRelease && sfxPreviewCue == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] sfxPreviewCue obrigatorio quando enableSfxPreviewOnRelease estiver habilitado.");
            }
        }

        private void SyncFromCurrentState(string reason)
        {
            if (_stateService == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] IPreferencesStateService ausente no AudioPreferencesOptionsBinder.");
            }

            if (!_stateService.HasSnapshot)
            {
                throw new InvalidOperationException("[FATAL][Preferences] Snapshot ausente ao sincronizar os sliders de Audio.");
            }

            _syncingFromState = true;
            try
            {
                var snapshot = _stateService.CurrentSnapshot;

                SetSliderValue(masterVolumeSlider, snapshot.MasterVolume);
                SetSliderValue(bgmVolumeSlider, snapshot.BgmVolume);
                SetSliderValue(sfxVolumeSlider, snapshot.SfxVolume);
            }
            finally
            {
                _syncingFromState = false;
            }
        }

        private void RegisterListeners()
        {
            if (_listenersRegistered)
            {
                return;
            }

            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            _listenersRegistered = true;
        }

        private void UnregisterListeners()
        {
            if (!_listenersRegistered)
            {
                return;
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            }

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            }

            _listenersRegistered = false;
        }

        private void ResolveServicesOrThrow()
        {
            if (_servicesResolved)
            {
                return;
            }

            if (DependencyManager.Provider == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] DependencyManager indisponivel para AudioPreferencesOptionsBinder.");
            }

            if (!DependencyManager.Provider.TryGetGlobal(out _stateService) || _stateService == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] IPreferencesStateService obrigatorio ausente para AudioPreferencesOptionsBinder.");
            }

            if (!DependencyManager.Provider.TryGetGlobal(out _saveService) || _saveService == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] IPreferencesSaveService obrigatorio ausente para AudioPreferencesOptionsBinder.");
            }

            _servicesResolved = true;
        }

        private void ValidateReferencesOrThrow()
        {
            if (masterVolumeSlider == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] masterVolumeSlider obrigatorio ausente no AudioPreferencesOptionsBinder.");
            }

            if (bgmVolumeSlider == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] bgmVolumeSlider obrigatorio ausente no AudioPreferencesOptionsBinder.");
            }

            if (sfxVolumeSlider == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] sfxVolumeSlider obrigatorio ausente no AudioPreferencesOptionsBinder.");
            }
        }

        private static void SetSliderValue(Slider slider, float value)
        {
            if (slider == null)
            {
                return;
            }

            slider.SetValueWithoutNotify(value);
        }

        private static float ReadSliderValue(Slider slider)
        {
            if (slider == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] Slider ausente ao ler valor de AudioPreferencesOptionsBinder.");
            }

            return slider.value;
        }

        private void EnsureInteractionRelays()
        {
            EnsureInteractionRelay(masterVolumeSlider, AudioPreferenceSliderKind.Master);
            EnsureInteractionRelay(bgmVolumeSlider, AudioPreferenceSliderKind.Bgm);
            EnsureInteractionRelay(sfxVolumeSlider, AudioPreferenceSliderKind.Sfx);
        }

        private void EnsureInteractionRelay(Slider slider, AudioPreferenceSliderKind kind)
        {
            if (slider == null)
            {
                return;
            }

            var relays = slider.GetComponents<AudioPreferencesSliderInteractionRelay>();
            if (relays is { Length: > 1 })
            {
                throw new InvalidOperationException("[FATAL][Preferences] Relay duplicado encontrado no mesmo slider de Audio.");
            }

            var relay = relays is { Length: 1 } ? relays[0] : null;
            if (relay == null)
            {
                relay = slider.gameObject.AddComponent<AudioPreferencesSliderInteractionRelay>();
            }

            relay.Configure(this, kind, enableSfxPreviewOnRelease, sfxPreviewCue);
        }
    }
}

