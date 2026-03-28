using System;
using System.Text;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Preferences.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Preferences.Runtime
{
    public sealed class PlayerPrefsPreferencesBackend : IPreferencesBackend
    {
        private const string KeyPrefix = "NewScripts.Preferences.Audio.v1";

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

        private static void BuildKeys(
            string profileId,
            string slotId,
            out string masterKey,
            out string bgmKey,
            out string sfxKey)
        {
            string scope = $"{KeyPrefix}.{NormalizeSegment(profileId)}.{NormalizeSegment(slotId)}";
            masterKey = $"{scope}.MasterVolume";
            bgmKey = $"{scope}.BgmVolume";
            sfxKey = $"{scope}.SfxVolume";
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
