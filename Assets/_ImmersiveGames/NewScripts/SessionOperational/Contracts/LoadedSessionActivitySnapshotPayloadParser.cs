using System;
using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionOperational.Contracts
{
    public enum LoadedSessionActivitySnapshotPayloadParseFailureKind
    {
        None = 0,
        PayloadMissing = 1,
        PayloadEmpty = 2,
        InvalidJson = 3,
        MissingActivityId = 4,
        MissingEntrySequence = 5,
        MissingObjectSnapshots = 6,
        InvalidEntrySequence = 7,
        UnknownFailure = 8,
    }

    public readonly struct LoadedSessionActivitySnapshotPayloadParseResult
    {
        public LoadedSessionActivitySnapshotPayloadParseResult(
            bool succeeded,
            LoadedSessionActivitySnapshotPayload payload,
            LoadedSessionActivitySnapshotPayloadParseFailureKind failureKind,
            string failureReason,
            string detail)
        {
            Succeeded = succeeded;
            Payload = payload;
            FailureKind = failureKind;
            FailureReason = Normalize(failureReason);
            Detail = Normalize(detail);
        }

        public bool Succeeded { get; }
        public LoadedSessionActivitySnapshotPayload Payload { get; }
        public LoadedSessionActivitySnapshotPayloadParseFailureKind FailureKind { get; }
        public string FailureReason { get; }
        public string Detail { get; }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public static class LoadedSessionActivitySnapshotPayloadParser
    {
        public static LoadedSessionActivitySnapshotPayloadParseResult Parse(string payload, string expectedSchemaId)
        {
            if (payload == null)
            {
                return Fail(LoadedSessionActivitySnapshotPayloadParseFailureKind.PayloadMissing, "payload_missing");
            }

            string normalizedPayload = payload.Trim();
            if (string.IsNullOrWhiteSpace(normalizedPayload))
            {
                return Fail(LoadedSessionActivitySnapshotPayloadParseFailureKind.PayloadEmpty, "payload_empty");
            }

            SnapshotPayloadDto dto;
            try
            {
                dto = JsonUtility.FromJson<SnapshotPayloadDto>(normalizedPayload);
            }
            catch (Exception ex)
            {
                return Fail(LoadedSessionActivitySnapshotPayloadParseFailureKind.InvalidJson, "invalid_json", ex.Message);
            }

            if (dto == null)
            {
                return Fail(LoadedSessionActivitySnapshotPayloadParseFailureKind.InvalidJson, "invalid_json");
            }

            string schemaId = Normalize(dto.schemaId);
            if (string.IsNullOrWhiteSpace(schemaId) || !string.Equals(schemaId, Normalize(expectedSchemaId), StringComparison.Ordinal))
            {
                return Fail(LoadedSessionActivitySnapshotPayloadParseFailureKind.UnknownFailure, $"schema_id_invalid:{schemaId}");
            }

            string sessionStateId = Normalize(dto.sessionStateId);
            string activityId = Normalize(dto.activityId);
            if (string.IsNullOrWhiteSpace(sessionStateId) || string.IsNullOrWhiteSpace(activityId))
            {
                return Fail(LoadedSessionActivitySnapshotPayloadParseFailureKind.MissingActivityId, "missing_activity_id");
            }

            if (dto.entrySequence == 0)
            {
                return Fail(LoadedSessionActivitySnapshotPayloadParseFailureKind.MissingEntrySequence, "missing_entry_sequence");
            }

            if (dto.entrySequence < 0)
            {
                return Fail(LoadedSessionActivitySnapshotPayloadParseFailureKind.InvalidEntrySequence, "invalid_entry_sequence");
            }

            if (dto.objects == null || dto.objects.Length == 0)
            {
                return Fail(LoadedSessionActivitySnapshotPayloadParseFailureKind.MissingObjectSnapshots, "missing_object_snapshots");
            }

            List<LoadedSessionActivitySnapshotPayloadObject> objects = new(dto.objects.Length);
            for (int index = 0; index < dto.objects.Length; index++)
            {
                SnapshotPayloadObjectDto obj = dto.objects[index];
                string targetId = Normalize(obj.targetId);
                if (string.IsNullOrWhiteSpace(targetId))
                {
                    return Fail(LoadedSessionActivitySnapshotPayloadParseFailureKind.UnknownFailure, "payload_target_id_invalid");
                }

                objects.Add(new LoadedSessionActivitySnapshotPayloadObject(
                    targetId,
                    obj.position.x,
                    obj.position.y,
                    obj.position.z,
                    obj.rotation.x,
                    obj.rotation.y,
                    obj.rotation.z,
                    obj.rotation.w,
                    obj.scale.x,
                    obj.scale.y,
                    obj.scale.z));
            }

            LoadedSessionActivitySnapshotPayload loadedPayload = new(
                schemaId,
                sessionStateId,
                activityId,
                dto.entrySequence,
                objects);
            if (!loadedPayload.IsValid)
            {
                return Fail(LoadedSessionActivitySnapshotPayloadParseFailureKind.UnknownFailure, "loaded_payload_invalid");
            }

            return new LoadedSessionActivitySnapshotPayloadParseResult(
                true,
                loadedPayload,
                LoadedSessionActivitySnapshotPayloadParseFailureKind.None,
                "parsed",
                string.Empty);
        }

        private static LoadedSessionActivitySnapshotPayloadParseResult Fail(
            LoadedSessionActivitySnapshotPayloadParseFailureKind failureKind,
            string failureReason,
            string detail = "")
        {
            return new LoadedSessionActivitySnapshotPayloadParseResult(
                false,
                default,
                failureKind,
                failureReason,
                detail);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        [Serializable]
        private sealed class SnapshotPayloadDto
        {
            public string schemaId;
            public string sessionStateId;
            public string activityId;
            public int entrySequence;
            public SnapshotPayloadObjectDto[] objects;
        }

        [Serializable]
        private sealed class SnapshotPayloadObjectDto
        {
            public string targetId;
            public Vector3Dto position;
            public QuaternionDto rotation;
            public Vector3Dto scale;
        }

        [Serializable]
        private struct Vector3Dto
        {
            public float x;
            public float y;
            public float z;
        }

        [Serializable]
        private struct QuaternionDto
        {
            public float x;
            public float y;
            public float z;
            public float w;
        }
    }
}
