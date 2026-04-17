using System;
using System.Text;
using ImmersiveGames.GameJam2025.Core.Logging;
using ImmersiveGames.GameJam2025.Experience.Preferences.Contracts;
using UnityEngine;
namespace ImmersiveGames.GameJam2025.Experience.Preferences.Runtime
{
    public sealed class PlayerPrefsPreferencesBackend : IPreferencesBackend
    {
        private const string AudioKeyPrefix = "NewScripts.Preferences.Audio.v1";
        private const string VideoKeyPrefix = "NewScripts.Preferences.Video.v1";

        public string BackendId => "PlayerPrefs";
        public bool IsAvailable => true;

        public bool TryLoad(
            string profileId,
            string slotId,
            out AudioPreferencesSnapshot snapshot,
            out string reason)
        {
            ValidateIdentity(profileId, nameof(profileId));
            ValidateIdentity(slotId, nameof(slotId));

            DebugUtility.LogVerbose(typeof(PlayerPrefsPreferencesBackend),
                $"[Preferences] backend='{BackendId}' load requested. profile='{profileId}' slot='{slotId}'.",
                DebugUtility.Colors.Info);

            BuildKeys(profileId, slotId, out string masterKey, out string bgmKey, out string sfxKey);

            bool hasMaster = PlayerPrefs.HasKey(masterKey);
            bool hasBgm = PlayerPrefs.HasKey(bgmKey);
            bool hasSfx = PlayerPrefs.HasKey(sfxKey);

            if (!hasMaster && !hasBgm && !hasSfx)
            {
                snapshot = null;
                reason = "no_saved_data";
                DebugUtility.Log(typeof(PlayerPrefsPreferencesBackend),
                    $"[Preferences] backend='{BackendId}' load skipped. reason='{reason}' profile='{profileId}' slot='{slotId}'.",
                    DebugUtility.Colors.Info);
                return false;
            }

            if (!hasMaster || !hasBgm || !hasSfx)
            {
                snapshot = null;
                reason = "load_failed";
                DebugUtility.LogWarning(typeof(PlayerPrefsPreferencesBackend),
                    $"[Preferences] backend='{BackendId}' load failed. reason='{reason}' profile='{profileId}' slot='{slotId}' keys=[master:{hasMaster}, bgm:{hasBgm}, sfx:{hasSfx}].");
                return false;
            }

            float master = PlayerPrefs.GetFloat(masterKey);
            float bgm = PlayerPrefs.GetFloat(bgmKey);
            float sfx = PlayerPrefs.GetFloat(sfxKey);

            if (!IsFinite(master) || !IsFinite(bgm) || !IsFinite(sfx))
            {
                snapshot = null;
                reason = "load_failed";
                DebugUtility.LogWarning(typeof(PlayerPrefsPreferencesBackend),
                    $"[Preferences] backend='{BackendId}' load failed. reason='{reason}' profile='{profileId}' slot='{slotId}'.");
                return false;
            }

            snapshot = new AudioPreferencesSnapshot(profileId, slotId, master, bgm, sfx);
            reason = "loaded";

            DebugUtility.Log(typeof(PlayerPrefsPreferencesBackend),
                $"[Preferences] backend='{BackendId}' load executed. snapshot={snapshot}.",
                DebugUtility.Colors.Info);

            return true;
        }

        public bool TrySave(
            AudioPreferencesSnapshot snapshot,
            out string reason)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            BuildKeys(snapshot.ProfileId, snapshot.SlotId, out string masterKey, out string bgmKey, out string sfxKey);

            PlayerPrefs.SetFloat(masterKey, snapshot.MasterVolume);
            PlayerPrefs.SetFloat(bgmKey, snapshot.BgmVolume);
            PlayerPrefs.SetFloat(sfxKey, snapshot.SfxVolume);
            PlayerPrefs.Save();

            reason = "saved";

            DebugUtility.Log(typeof(PlayerPrefsPreferencesBackend),
                $"[Preferences] backend='{BackendId}' save executed. snapshot={snapshot}.",
                DebugUtility.Colors.Info);

            return true;
        }

        public bool TryLoadVideo(
            string profileId,
            string slotId,
            out VideoPreferencesSnapshot snapshot,
            out string reason)
        {
            ValidateIdentity(profileId, nameof(profileId));
            ValidateIdentity(slotId, nameof(slotId));

            DebugUtility.LogVerbose(typeof(PlayerPrefsPreferencesBackend),
                $"[Preferences] backend='{BackendId}' video load requested. profile='{profileId}' slot='{slotId}'.",
                DebugUtility.Colors.Info);

            BuildVideoKeys(profileId, slotId, out string widthKey, out string heightKey, out string fullscreenKey);

            bool hasWidth = PlayerPrefs.HasKey(widthKey);
            bool hasHeight = PlayerPrefs.HasKey(heightKey);
            bool hasFullscreen = PlayerPrefs.HasKey(fullscreenKey);

            if (!hasWidth && !hasHeight && !hasFullscreen)
            {
                snapshot = null;
                reason = "no_saved_data";
                DebugUtility.Log(typeof(PlayerPrefsPreferencesBackend),
                    $"[Preferences] backend='{BackendId}' video load skipped. reason='{reason}' profile='{profileId}' slot='{slotId}'.",
                    DebugUtility.Colors.Info);
                return false;
            }

            if (!hasWidth || !hasHeight || !hasFullscreen)
            {
                snapshot = null;
                reason = "load_failed";
                DebugUtility.LogWarning(typeof(PlayerPrefsPreferencesBackend),
                    $"[Preferences] backend='{BackendId}' video load failed. reason='{reason}' profile='{profileId}' slot='{slotId}' keys=[width:{hasWidth}, height:{hasHeight}, fullscreen:{hasFullscreen}].");
                return false;
            }

            int width = PlayerPrefs.GetInt(widthKey);
            int height = PlayerPrefs.GetInt(heightKey);
            int fullscreenRaw = PlayerPrefs.GetInt(fullscreenKey);

            if (width <= 0 || height <= 0 || (fullscreenRaw != 0 && fullscreenRaw != 1))
            {
                snapshot = null;
                reason = "load_failed";
                DebugUtility.LogWarning(typeof(PlayerPrefsPreferencesBackend),
                    $"[Preferences] backend='{BackendId}' video load failed. reason='{reason}' profile='{profileId}' slot='{slotId}' values=[width:{width}, height:{height}, fullscreen:{fullscreenRaw}].");
                return false;
            }

            snapshot = new VideoPreferencesSnapshot(profileId, slotId, width, height, fullscreenRaw == 1);
            reason = "loaded";

            DebugUtility.Log(typeof(PlayerPrefsPreferencesBackend),
                $"[Preferences] backend='{BackendId}' video load executed. snapshot={snapshot}.",
                DebugUtility.Colors.Info);

            return true;
        }

        public bool TrySaveVideo(
            VideoPreferencesSnapshot snapshot,
            out string reason)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            BuildVideoKeys(snapshot.ProfileId, snapshot.SlotId, out string widthKey, out string heightKey, out string fullscreenKey);

            PlayerPrefs.SetInt(widthKey, snapshot.ResolutionWidth);
            PlayerPrefs.SetInt(heightKey, snapshot.ResolutionHeight);
            PlayerPrefs.SetInt(fullscreenKey, snapshot.Fullscreen ? 1 : 0);
            PlayerPrefs.Save();

            reason = "saved";

            DebugUtility.Log(typeof(PlayerPrefsPreferencesBackend),
                $"[Preferences] backend='{BackendId}' video save executed. snapshot={snapshot}.",
                DebugUtility.Colors.Info);

            return true;
        }

        private static void BuildKeys(
            string profileId,
            string slotId,
            out string masterKey,
            out string bgmKey,
            out string sfxKey)
        {
            string scope = $"{AudioKeyPrefix}.{NormalizeSegment(profileId)}.{NormalizeSegment(slotId)}";
            masterKey = $"{scope}.MasterVolume";
            bgmKey = $"{scope}.BgmVolume";
            sfxKey = $"{scope}.SfxVolume";
        }

        private static void BuildVideoKeys(
            string profileId,
            string slotId,
            out string widthKey,
            out string heightKey,
            out string fullscreenKey)
        {
            string scope = $"{VideoKeyPrefix}.{NormalizeSegment(profileId)}.{NormalizeSegment(slotId)}";
            widthKey = $"{scope}.ResolutionWidth";
            heightKey = $"{scope}.ResolutionHeight";
            fullscreenKey = $"{scope}.Fullscreen";
        }

        private static string NormalizeSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Identity is required.");
            }

            string trimmed = value.Trim().ToLowerInvariant();
            var builder = new StringBuilder(trimmed.Length);

            for (int i = 0; i < trimmed.Length; i++)
            {
                char c = trimmed[i];
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
                {
                    builder.Append(c);
                }
                else
                {
                    builder.Append('_');
                }
            }

            return builder.ToString();
        }

        private static void ValidateIdentity(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Identity is required.", paramName);
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}

