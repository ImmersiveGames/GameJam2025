using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Experience.Preferences.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.NewScripts.Experience.Preferences.Bindings
{
    /// <summary>
    /// Binder de intencao para opcoes de video.
    ///
    /// Regras:
    /// - sincroniza os controles com o estado canônico ao abrir o painel;
    /// - publica apenas intenção de mudança;
    /// - não conversa com Screen.SetResolution nem com backend diretamente.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VideoPreferencesOptionsBinder : MonoBehaviour
    {
        [Header("Controls")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;

        [Header("Behavior")]
        [SerializeField] private bool syncOnEnable = true;

        private IPreferencesStateService _stateService;
        private IPreferencesSaveService _saveService;
        private bool _servicesResolved;
        private bool _listenersRegistered;
        private bool _syncingFromState;
        private readonly List<Vector2Int> _availablePresets = new();

        private void Awake()
        {
            ResolveServicesOrThrow();
            ValidateReferencesOrThrow();
            RefreshResolutionOptions();
        }

        private void OnEnable()
        {
            ResolveServicesOrThrow();
            ValidateReferencesOrThrow();
            RegisterListeners();

            if (syncOnEnable)
            {
                SyncFromCurrentState("VideoPreferences/OnEnable");
            }
        }

        private void OnDisable()
        {
            UnregisterListeners();
        }

        public void RefreshFromCurrentState(string reason = "VideoPreferences/Refresh")
        {
            SyncFromCurrentState(reason);
        }

        public void RestoreVideoDefaults()
        {
            if (_saveService == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] IPreferencesSaveService ausente no VideoPreferencesOptionsBinder.");
            }

            _saveService.TryRestoreVideoDefaults(
                reason: "VideoPreferences/RestoreDefaults",
                out string _);

            SyncFromCurrentState("VideoPreferences/RestoreDefaults");
        }

        private void OnResolutionChanged(int _)
        {
            ApplySelection("Resolution", "VideoPreferences/ResolutionChanged");
        }

        private void OnFullscreenChanged(bool _)
        {
            ApplySelection("Fullscreen", "VideoPreferences/FullscreenChanged");
        }

        private void ApplySelection(string fieldHint, string reason)
        {
            if (_syncingFromState)
            {
                return;
            }

            try
            {
                if (_saveService == null)
                {
                    throw new InvalidOperationException("[FATAL][Preferences] IPreferencesSaveService ausente no VideoPreferencesOptionsBinder.");
                }

                Vector2Int selectedPreset = ReadSelectedPreset();
                bool fullscreen = ReadFullscreen();

                _saveService.TryPreviewVideoResolution(
                    width: selectedPreset.x,
                    height: selectedPreset.y,
                    fullscreen: fullscreen,
                    reason: reason,
                    out bool _);

                _saveService.TryCommitCurrentVideoResolution(
                    reason: reason,
                    fieldHint: fieldHint,
                    out bool _,
                    out string _);
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        private void SyncFromCurrentState(string reason)
        {
            if (_stateService == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] IPreferencesStateService ausente no VideoPreferencesOptionsBinder.");
            }

            if (!_stateService.HasVideoSnapshot)
            {
                throw new InvalidOperationException("[FATAL][Preferences] Video snapshot ausente ao sincronizar o painel de Video.");
            }

            _syncingFromState = true;
            try
            {
                RefreshResolutionOptions();

                var snapshot = _stateService.CurrentVideoSnapshot;
                int presetIndex = ResolvePresetIndex(snapshot.ResolutionWidth, snapshot.ResolutionHeight);
                SetDropdownValue(resolutionDropdown, presetIndex);
                SetToggleValue(fullscreenToggle, snapshot.Fullscreen);
            }
            finally
            {
                _syncingFromState = false;
            }
        }

        private void RefreshResolutionOptions()
        {
            if (_stateService == null || resolutionDropdown == null)
            {
                return;
            }

            var presets = _stateService.GetVideoResolutionPresets();
            _availablePresets.Clear();

            foreach (var preset in presets)
            {
                if (preset.x <= 0 || preset.y <= 0)
                {
                    continue;
                }

                if (!_availablePresets.Contains(preset))
                {
                    _availablePresets.Add(preset);
                }
            }

            if (_availablePresets.Count == 0)
            {
                throw new InvalidOperationException("[FATAL][Preferences] Nenhum preset de resolucao disponivel para o painel de Video.");
            }

            var labels = new List<string>(_availablePresets.Count);
            foreach (var preset in _availablePresets)
            {
                labels.Add(FormatPresetLabel(preset));
            }

            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(labels);
        }

        private int ResolvePresetIndex(int width, int height)
        {
            for (int i = 0; i < _availablePresets.Count; i++)
            {
                var preset = _availablePresets[i];
                if (preset.x == width && preset.y == height)
                {
                    return i;
                }
            }

            return 0;
        }

        private Vector2Int ReadSelectedPreset()
        {
            if (_availablePresets.Count == 0)
            {
                throw new InvalidOperationException("[FATAL][Preferences] Nenhum preset de resolucao carregado no VideoPreferencesOptionsBinder.");
            }

            int index = Mathf.Clamp(resolutionDropdown.value, 0, _availablePresets.Count - 1);
            return _availablePresets[index];
        }

        private bool ReadFullscreen()
        {
            if (fullscreenToggle == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] fullscreenToggle ausente no VideoPreferencesOptionsBinder.");
            }

            return fullscreenToggle.isOn;
        }

        private void RegisterListeners()
        {
            if (_listenersRegistered)
            {
                return;
            }

            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            _listenersRegistered = true;
        }

        private void UnregisterListeners()
        {
            if (!_listenersRegistered)
            {
                return;
            }

            if (resolutionDropdown != null)
            {
                resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
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
                throw new InvalidOperationException("[FATAL][Preferences] DependencyManager indisponivel para VideoPreferencesOptionsBinder.");
            }

            if (!DependencyManager.Provider.TryGetGlobal(out _stateService) || _stateService == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] IPreferencesStateService obrigatorio ausente para VideoPreferencesOptionsBinder.");
            }

            if (!DependencyManager.Provider.TryGetGlobal(out _saveService) || _saveService == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] IPreferencesSaveService obrigatorio ausente para VideoPreferencesOptionsBinder.");
            }

            _servicesResolved = true;
        }

        private void ValidateReferencesOrThrow()
        {
            if (resolutionDropdown == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] resolutionDropdown obrigatorio ausente no VideoPreferencesOptionsBinder.");
            }

            if (fullscreenToggle == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] fullscreenToggle obrigatorio ausente no VideoPreferencesOptionsBinder.");
            }
        }

        private static void SetDropdownValue(TMP_Dropdown dropdown, int value)
        {
            if (dropdown == null)
            {
                return;
            }

            dropdown.SetValueWithoutNotify(value);
        }

        private static void SetToggleValue(Toggle toggle, bool value)
        {
            if (toggle == null)
            {
                return;
            }

            toggle.SetIsOnWithoutNotify(value);
        }

        private static string FormatPresetLabel(Vector2Int preset)
        {
            return $"{preset.x}x{preset.y}";
        }
    }
}
