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
            ProgressionSlotContext slotContext,
            string activityIdentity)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("[FATAL][Config][RouteActivitySave] SessionOperationalRouteCommand invalido para load-on-enter.");
            }

            ValidateProgressionSlotContextOrFail(slotContext);

            string normalizedActivityIdentity = Normalize(activityIdentity);
            if (string.IsNullOrWhiteSpace(normalizedActivityIdentity))
            {
                return new RouteActivitySaveLoadResult(
                    RouteActivitySaveLoadOutcomeKind.Skipped,
                    RouteActivitySaveSnapshotFailureKind.NoCurrentActivity,
                    RouteActivitySaveSkipKind.Unknown,
                    "no_activity_identity",
                    false,
                    "activity identity obrigatoria ausente para load-on-enter.");
            }

            string activitySaveKey = BuildActivitySaveKey(normalizedActivityIdentity);
            if (string.IsNullOrWhiteSpace(activitySaveKey))
            {
                return new RouteActivitySaveLoadResult(
                    RouteActivitySaveLoadOutcomeKind.Skipped,
                    RouteActivitySaveSnapshotFailureKind.UnknownFailure,
                    RouteActivitySaveSkipKind.Unknown,
                    "no_save_key",
                    false,
                    "activity save key obrigatoria ausente para load-on-enter.");
            }

            SaveConfigAsset saveConfig = SaveRuntimeConfigResolver.ResolveSaveConfigOrFail(runtimeModeConfig);
            SaveAddress address = new SaveAddress(
                SaveScope.Progression,
                SaveGroup.RouteActivity,
                ownerId: normalizedActivityIdentity,
                recordId: slotContext.SnapshotId.Value,
                slotId: slotContext.SlotId.Value,
                schemaId: "progression.route_activity",
                schemaVersion: saveConfig.SchemaVersion);
            SaveRequest request = new SaveRequest(
                address,
                new Dictionary<string, string>(StringComparer.Ordinal),
                revision: 0,
                savedAtUtc: DateTime.UtcNow.ToString("O"));

            bool loaded = _saveService.TryLoad(address, out SaveResult loadResult, out string loadReason);
            if (!loaded || loadResult == null || !loadResult.IsSuccess)
            {
                return new RouteActivitySaveLoadResult(
                    RouteActivitySaveLoadOutcomeKind.Skipped,
                    RouteActivitySaveSnapshotFailureKind.SnapshotPayloadMissing,
                    RouteActivitySaveSkipKind.NoSnapshotPayload,
                    "no_snapshot",
                    false,
                    $"requestAddress='{request.Address}' activitySaveKey='{activitySaveKey}' loadReason='{Normalize(loadReason)}'");
            }

            string activitySnapshotPayload = string.Empty;
            if (loadResult.Entries != null &&
                loadResult.Entries.TryGetValue(activitySaveKey, out string storedPayload) &&
                !string.IsNullOrWhiteSpace(storedPayload))
            {
                activitySnapshotPayload = storedPayload.Trim();
            }

            if (string.IsNullOrWhiteSpace(activitySnapshotPayload))
            {
                return new RouteActivitySaveLoadResult(
                    RouteActivitySaveLoadOutcomeKind.Skipped,
                    RouteActivitySaveSnapshotFailureKind.SnapshotPayloadMissing,
                    RouteActivitySaveSkipKind.NoSnapshotPayload,
                    "no_activity_snapshot",
                    false,
                    $"requestAddress='{request.Address}' activitySaveKey='{activitySaveKey}' detail='snapshot payload vazio para save key.'");
            }

            return new RouteActivitySaveLoadResult(
                RouteActivitySaveLoadOutcomeKind.Loaded,
                RouteActivitySaveSnapshotFailureKind.None,
                RouteActivitySaveSkipKind.None,
                string.Empty,
                true,
                $"requestAddress='{request.Address}' activitySaveKey='{activitySaveKey}' schemaVersion='{loadResult.SchemaVersion}' revision='{loadResult.Revision}' entriesCount='{loadResult.Entries?.Count ?? 0}'",
                activitySnapshotPayload);
        }

        public RouteActivitySaveSaveResult SaveActivityOnExit(
            RuntimeModeConfig runtimeModeConfig,
            ProgressionSlotContext slotContext,
            string previousActivityIdentity,
            string activitySnapshotPayload)
        {
            ValidateProgressionSlotContextOrFail(slotContext);

            string normalizedActivityIdentity = Normalize(previousActivityIdentity);
            if (string.IsNullOrWhiteSpace(normalizedActivityIdentity))
            {
                return new RouteActivitySaveSaveResult(
                    RouteActivitySaveSaveOutcomeKind.Skipped,
                    RouteActivitySaveSnapshotFailureKind.NoCurrentActivity,
                    RouteActivitySaveSkipKind.Unknown,
                    "no_activity_identity",
                    false,
                    "activity identity obrigatoria ausente para save-on-exit.");
            }

            string activitySaveKey = BuildActivitySaveKey(normalizedActivityIdentity);
            if (string.IsNullOrWhiteSpace(activitySaveKey))
            {
                return new RouteActivitySaveSaveResult(
                    RouteActivitySaveSaveOutcomeKind.Skipped,
                    RouteActivitySaveSnapshotFailureKind.UnknownFailure,
                    RouteActivitySaveSkipKind.Unknown,
                    "no_save_key",
                    false,
                    "activity save key obrigatoria ausente para save-on-exit.");
            }

            string normalizedPayload = Normalize(activitySnapshotPayload);
            if (string.IsNullOrWhiteSpace(normalizedPayload))
            {
                return new RouteActivitySaveSaveResult(
                    RouteActivitySaveSaveOutcomeKind.Skipped,
                    RouteActivitySaveSnapshotFailureKind.SnapshotPayloadMissing,
                    RouteActivitySaveSkipKind.NoSnapshotPayload,
                    "no_activity_snapshot",
                    false,
                    "activity snapshot payload obrigatorio ausente para save-on-exit.");
            }

            SaveConfigAsset saveConfig = SaveRuntimeConfigResolver.ResolveSaveConfigOrFail(runtimeModeConfig);
            Dictionary<string, string> entries = new(StringComparer.Ordinal)
            {
                [activitySaveKey] = normalizedPayload,
            };
            SaveAddress address = new SaveAddress(
                SaveScope.Progression,
                SaveGroup.RouteActivity,
                ownerId: normalizedActivityIdentity,
                recordId: slotContext.SnapshotId.Value,
                slotId: slotContext.SlotId.Value,
                schemaId: "progression.route_activity",
                schemaVersion: saveConfig.SchemaVersion);
            SaveRequest request = new SaveRequest(
                address,
                entries,
                revision: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                savedAtUtc: DateTime.UtcNow.ToString("O"),
                profileId: slotContext.ProfileId);
            bool saved = _saveService.TrySave(request, out SaveResult saveResult, out string saveReason);
            if (!saved || saveResult == null || !saveResult.IsSuccess)
            {
                string detail = $"requestAddress='{request.Address}' activitySaveKey='{activitySaveKey}' saveReason='{Normalize(saveReason)}'";
                throw new InvalidOperationException($"[FATAL][Config][RouteActivitySave] save-on-exit falhou. {detail}");
            }

            return new RouteActivitySaveSaveResult(
                RouteActivitySaveSaveOutcomeKind.Saved,
                RouteActivitySaveSnapshotFailureKind.None,
                RouteActivitySaveSkipKind.None,
                string.Empty,
                true,
                $"requestAddress='{request.Address}' activitySaveKey='{activitySaveKey}' schemaVersion='{saveResult.SchemaVersion}' revision='{saveResult.Revision}' entriesCount='{saveResult.Entries?.Count ?? 0}'");
        }

        private static void ValidateProgressionSlotContextOrFail(ProgressionSlotContext slotContext)
        {
            if (slotContext == null || !slotContext.IsValid)
            {
                throw new InvalidOperationException("[FATAL][Config][RouteActivitySave] ProgressionSlotContext obrigatorio ausente/invalido para load-on-enter/save-on-exit.");
            }
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
