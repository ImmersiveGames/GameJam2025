using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.SaveRuntime.Authoring;
using _ImmersiveGames.NewScripts.SaveRuntime.Contracts;
using _ImmersiveGames.NewScripts.SaveRuntime.Models;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;

namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{
    public sealed class SessionOperationalActivitySaveAdapter : ISessionOperationalActivitySaveAdapter
    {
        private readonly ISaveService _saveService;

        public SessionOperationalActivitySaveAdapter(ISaveService saveService)
        {
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
        }

        public RouteActivitySaveLoadResult LoadActivitySaveOnEnter(
            RuntimeModeConfig runtimeModeConfig,
            SessionOperationalRouteCommand command,
            string activityIdentity)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("[FATAL][Config][RouteActivitySave] SessionOperationalRouteCommand invalido para load-on-enter.");
            }

            string normalizedActivityIdentity = Normalize(activityIdentity);
            if (string.IsNullOrWhiteSpace(normalizedActivityIdentity))
            {
                return new RouteActivitySaveLoadResult(
                    RouteActivitySaveLoadOutcomeKind.Skipped,
                    "no_activity_identity",
                    null,
                    "activity identity obrigatoria ausente para load-on-enter.");
            }

            string activitySaveKey = BuildActivitySaveKey(normalizedActivityIdentity);
            if (string.IsNullOrWhiteSpace(activitySaveKey))
            {
                return new RouteActivitySaveLoadResult(
                    RouteActivitySaveLoadOutcomeKind.Skipped,
                    "no_save_key",
                    null,
                    "activity save key obrigatoria ausente para load-on-enter.");
            }

            SaveConfigAsset saveConfig = SaveRuntimeConfigResolver.ResolveSaveConfigOrFail(runtimeModeConfig);
            SaveIdentity saveIdentity = saveConfig.BuildDefaultIdentityOrFail();

            bool loaded = _saveService.TryLoad(saveIdentity, out SaveRecord record, out string loadReason);
            if (!loaded || record == null)
            {
                return new RouteActivitySaveLoadResult(
                    RouteActivitySaveLoadOutcomeKind.Skipped,
                    "no_snapshot",
                    null,
                    $"saveIdentity='{saveIdentity}' activitySaveKey='{activitySaveKey}' loadReason='{Normalize(loadReason)}'");
            }

            return new RouteActivitySaveLoadResult(
                RouteActivitySaveLoadOutcomeKind.Loaded,
                string.Empty,
                record,
                $"saveIdentity='{saveIdentity}' activitySaveKey='{activitySaveKey}' schemaVersion='{record.SchemaVersion}' revision='{record.Revision}' entriesCount='{record.Entries?.Count ?? 0}'");
        }

        public RouteActivitySaveSaveResult SaveActivityOnExit(
            RuntimeModeConfig runtimeModeConfig,
            string previousActivityIdentity,
            string activitySnapshotPayload)
        {
            string normalizedActivityIdentity = Normalize(previousActivityIdentity);
            if (string.IsNullOrWhiteSpace(normalizedActivityIdentity))
            {
                return new RouteActivitySaveSaveResult(
                    RouteActivitySaveSaveOutcomeKind.Skipped,
                    "no_activity_identity",
                    null,
                    "activity identity obrigatoria ausente para save-on-exit.");
            }

            string activitySaveKey = BuildActivitySaveKey(normalizedActivityIdentity);
            if (string.IsNullOrWhiteSpace(activitySaveKey))
            {
                return new RouteActivitySaveSaveResult(
                    RouteActivitySaveSaveOutcomeKind.Skipped,
                    "no_save_key",
                    null,
                    "activity save key obrigatoria ausente para save-on-exit.");
            }

            string normalizedPayload = Normalize(activitySnapshotPayload);
            if (string.IsNullOrWhiteSpace(normalizedPayload))
            {
                return new RouteActivitySaveSaveResult(
                    RouteActivitySaveSaveOutcomeKind.Skipped,
                    "no_activity_snapshot",
                    null,
                    "activity snapshot payload obrigatorio ausente para save-on-exit.");
            }

            SaveConfigAsset saveConfig = SaveRuntimeConfigResolver.ResolveSaveConfigOrFail(runtimeModeConfig);
            SaveIdentity saveIdentity = saveConfig.BuildDefaultIdentityOrFail();
            Dictionary<string, string> entries = new(StringComparer.Ordinal)
            {
                [activitySaveKey] = normalizedPayload,
            };

            SaveRecord record = new(
                saveIdentity,
                saveConfig.SchemaVersion,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                DateTime.UtcNow.ToString("O"),
                entries);

            bool saved = _saveService.TrySave(record, out string saveReason);
            if (!saved)
            {
                string detail = $"saveIdentity='{saveIdentity}' activitySaveKey='{activitySaveKey}' saveReason='{Normalize(saveReason)}'";
                throw new InvalidOperationException($"[FATAL][Config][RouteActivitySave] save-on-exit falhou. {detail}");
            }

            return new RouteActivitySaveSaveResult(
                RouteActivitySaveSaveOutcomeKind.Saved,
                string.Empty,
                record,
                $"saveIdentity='{saveIdentity}' activitySaveKey='{activitySaveKey}' schemaVersion='{record.SchemaVersion}' revision='{record.Revision}' entriesCount='{record.Entries?.Count ?? 0}'");
        }

        private static string BuildActivitySaveKey(string activityIdentity)
        {
            string normalized = Normalize(activityIdentity);
            return string.IsNullOrWhiteSpace(normalized) ? string.Empty : $"activity:{normalized}";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
