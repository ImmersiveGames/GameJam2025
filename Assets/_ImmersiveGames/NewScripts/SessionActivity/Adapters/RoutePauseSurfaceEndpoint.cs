using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Adapters
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/SessionActivity/Route Pause Surface Endpoint")]
    public sealed class RoutePauseSurfaceEndpoint : MonoBehaviour
    {
        private readonly List<ActivityPauseContentInstanceRecord> _boundActivityContent = new();

        [SerializeField] private string surfaceId = "route.pause.surface.sandbox";
        [SerializeField] private string overlayRootId = "pause.overlay.root";
        [SerializeField] private string activityContentRootId = "pause.activity.content.root";
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private GameObject activityContentRoot;
        [SerializeField] private bool hideOverlayOnAwake = true;

        public string SurfaceId => surfaceId.TrimToEmpty();
        public string OverlayRootId => overlayRootId.TrimToEmpty();
        public string ActivityContentRootId => activityContentRootId.TrimToEmpty();
        public GameObject OverlayRoot => overlayRoot;
        public GameObject ActivityContentRoot => activityContentRoot;

        private void Awake()
        {
            if (hideOverlayOnAwake && overlayRoot != null)
            {
                ApplyVisible(overlayRoot, false);
            }
        }

        public bool Matches(SessionActivityRoutePauseSurfaceContext context)
        {
            if (!context.HasSurface || !context.IsValid)
            {
                return false;
            }

            if (!string.Equals(SurfaceId, context.SurfaceId, StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.Equals(OverlayRootId, context.OverlayRootId, StringComparison.Ordinal))
            {
                return false;
            }

            // O root de conteudo da Activity pertence ao PAUSE-6. No PAUSE-5, o show/hide do overlay
            // deve resolver a surface mesmo quando esse container ainda nao esta configurado na cena.
            if (string.IsNullOrWhiteSpace(ActivityContentRootId) || string.IsNullOrWhiteSpace(context.ActivityContentRootId))
            {
                return true;
            }

            return string.Equals(ActivityContentRootId, context.ActivityContentRootId, StringComparison.Ordinal);
        }

        public void Show(SessionActivityIdentity identity, SessionActivityRoutePauseSurfaceContext context, string source, string reason)
        {
            ValidateOverlayOrFail(context, nameof(Show));
            ApplyVisible(overlayRoot, true);

            DebugUtility.Log(typeof(RoutePauseSurfaceEndpoint),
                $"event='PauseOverlayShown' surfaceId='{context.SurfaceId}' sceneName='{context.SceneName}' overlayRootId='{context.OverlayRootId}' overlayRoot='{overlayRoot.name}' activityContentRootId='{context.ActivityContentRootId}' activityContentRoot='{FormatOptionalName(activityContentRoot)}' identity='{identity}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'.",
                DebugUtility.Colors.Success);
        }

        public void Hide(SessionActivityIdentity identity, SessionActivityRoutePauseSurfaceContext context, string source, string reason)
        {
            ValidateOverlayOrFail(context, nameof(Hide));
            ApplyVisible(overlayRoot, false);

            DebugUtility.Log(typeof(RoutePauseSurfaceEndpoint),
                $"event='PauseOverlayHidden' surfaceId='{context.SurfaceId}' sceneName='{context.SceneName}' overlayRootId='{context.OverlayRootId}' overlayRoot='{overlayRoot.name}' activityContentRootId='{context.ActivityContentRootId}' activityContentRoot='{FormatOptionalName(activityContentRoot)}' identity='{identity}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'.",
                DebugUtility.Colors.Success);
        }

        public ActivityPauseContentBindingResult BindActivityPauseContent(ActivityPauseContentBindingCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityPauseContentBindingCommand is invalid.");
            }

            ValidateActivityContentOrFail(command.RoutePauseSurfaceContext, nameof(BindActivityPauseContent));
            ReleaseBoundContentForIdentity(command.Identity, "RoutePauseSurfaceEndpoint", "pause_content_rebind_cleanup");

            var slotMap = BuildSlotMapOrFail();
            int boundCount = 0;
            int skippedCount = 0;
            for (int i = 0; i < command.Profile.Contributions.Count; i++)
            {
                var contribution = command.Profile.Contributions[i];
                if (!contribution.IsValid)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][PauseContent] contribution invalida profileId='{command.Profile.ProfileId}' index='{i}' activityId='{command.ActivityId}' entrySequence='{command.EntrySequence}'.");
                }

                if (!slotMap.TryGetValue(contribution.SlotId.Value, out var slot) || slot == null)
                {
                    if (!contribution.Required)
                    {
                        skippedCount += 1;
                        continue;
                    }

                    throw new InvalidOperationException(
                        $"[FATAL][PauseContent] slot obrigatorio ausente surfaceId='{SurfaceId}' slotId='{contribution.SlotId}' activityId='{command.ActivityId}' entrySequence='{command.EntrySequence}' profileId='{command.Profile.ProfileId}'.");
                }

                var instance = Instantiate(contribution.Prefab, slot.transform, false);
                instance.name = $"{contribution.Prefab.name}::{command.ActivityId}::{command.EntrySequence}::PauseContent";
                _boundActivityContent.Add(new ActivityPauseContentInstanceRecord(
                    command.Identity.PipelineId,
                    command.Identity.SessionId,
                    command.ActivityId,
                    command.EntrySequence,
                    command.Profile.ProfileId,
                    contribution.SlotId.Value,
                    instance));
                boundCount += 1;
            }

            DebugUtility.Log(typeof(RoutePauseSurfaceEndpoint),
                $"event='ActivityPauseContentBound' surfaceId='{SurfaceId}' sceneName='{command.RoutePauseSurfaceContext.SceneName}' activityId='{command.ActivityId}' entrySequence='{command.EntrySequence}' profileId='{command.Profile.ProfileId}' requestedCount='{command.Profile.Contributions.Count}' boundCount='{boundCount}' skippedCount='{skippedCount}' activityContentRoot='{activityContentRoot.name}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);

            return new ActivityPauseContentBindingResult(
                true,
                false,
                command.Identity,
                command.Profile.ProfileId,
                command.Profile.Contributions.Count,
                boundCount,
                skippedCount,
                "activity_pause_content_bound");
        }

        public ActivityPauseContentReleaseResult ReleaseActivityPauseContent(ActivityPauseContentReleaseCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityPauseContentReleaseCommand is invalid.");
            }

            ValidateOverlayOrFail(command.RoutePauseSurfaceContext, nameof(ReleaseActivityPauseContent));
            int releasedCount = ReleaseBoundContentForIdentity(command.Identity, command.Source, command.Reason);
            bool skipped = releasedCount == 0;

            DebugUtility.Log(typeof(RoutePauseSurfaceEndpoint),
                $"event='ActivityPauseContentReleased' surfaceId='{SurfaceId}' sceneName='{command.RoutePauseSurfaceContext.SceneName}' activityId='{command.Identity.ActivityId}' entrySequence='{command.Identity.EntrySequence}' profileId='{command.CurrentBinding.ProfileId}' releasedCount='{releasedCount}' skipped='{skipped.ToString().ToLowerInvariant()}' source='{command.Source}' reason='{command.Reason}'.",
                skipped ? DebugUtility.Colors.Warning : DebugUtility.Colors.Success);

            return new ActivityPauseContentReleaseResult(
                true,
                skipped,
                command.Identity,
                command.CurrentBinding.ProfileId,
                releasedCount,
                skipped ? "activity_pause_content_release_skipped_no_bound_content" : "activity_pause_content_released");
        }

        private int ReleaseBoundContentForIdentity(SessionActivityIdentity identity, string source, string reason)
        {
            int releasedCount = 0;
            for (int i = _boundActivityContent.Count - 1; i >= 0; i--)
            {
                var record = _boundActivityContent[i];
                if (!record.Matches(identity))
                {
                    continue;
                }

                if (record.Instance != null)
                {
                    Destroy(record.Instance);
                    releasedCount += 1;
                }

                _boundActivityContent.RemoveAt(i);
            }

            if (releasedCount > 0)
            {
                DebugUtility.LogVerbose(typeof(RoutePauseSurfaceEndpoint),
                    $"event='ActivityPauseContentInstancesDestroyed' surfaceId='{SurfaceId}' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' releasedCount='{releasedCount}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'.",
                    DebugUtility.Colors.Info);
            }

            return releasedCount;
        }

        private Dictionary<string, RoutePauseSurfaceSlot> BuildSlotMapOrFail()
        {
            if (activityContentRoot == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][PauseContent] activityContentRoot obrigatorio ausente endpoint='{name}' surfaceId='{SurfaceId}' activityContentRootId='{ActivityContentRootId}' operation='BuildSlotMap'.");
            }

            var slots = activityContentRoot.GetComponentsInChildren<RoutePauseSurfaceSlot>(true);
            Dictionary<string, RoutePauseSurfaceSlot> slotMap = new(StringComparer.Ordinal);
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null)
                {
                    continue;
                }

                if (!slot.IsValid)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][PauseContent] RoutePauseSurfaceSlot invalido surfaceId='{SurfaceId}' slotObject='{slot.name}' index='{i}'.");
                }

                if (slotMap.ContainsKey(slot.SlotId))
                {
                    throw new InvalidOperationException(
                        $"[FATAL][PauseContent] RoutePauseSurfaceSlot duplicado surfaceId='{SurfaceId}' slotId='{slot.SlotId}'.");
                }

                slotMap.Add(slot.SlotId, slot);
            }

            return slotMap;
        }

        private void ValidateOverlayOrFail(SessionActivityRoutePauseSurfaceContext context, string operation)
        {
            if (!context.HasSurface || !context.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][PauseSurface] Invalid RoutePauseSurface context operation='{operation}' endpoint='{name}'.");
            }

            if (string.IsNullOrWhiteSpace(SurfaceId))
            {
                throw new InvalidOperationException(
                    $"[FATAL][PauseSurface] surfaceId obrigatorio ausente endpoint='{name}' operation='{operation}'.");
            }

            if (string.IsNullOrWhiteSpace(OverlayRootId))
            {
                throw new InvalidOperationException(
                    $"[FATAL][PauseSurface] overlayRootId obrigatorio ausente endpoint='{name}' operation='{operation}'.");
            }

            if (!string.Equals(SurfaceId, context.SurfaceId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"[FATAL][PauseSurface] surfaceId divergente endpoint='{name}' expected='{context.SurfaceId}' actual='{SurfaceId}' operation='{operation}'.");
            }

            if (!string.Equals(OverlayRootId, context.OverlayRootId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"[FATAL][PauseSurface] overlayRootId divergente endpoint='{name}' expected='{context.OverlayRootId}' actual='{OverlayRootId}' operation='{operation}'.");
            }

            if (overlayRoot == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][PauseSurface] overlayRoot obrigatorio ausente endpoint='{name}' surfaceId='{SurfaceId}' overlayRootId='{OverlayRootId}' operation='{operation}'.");
            }

            if (!string.IsNullOrWhiteSpace(ActivityContentRootId) &&
                !string.IsNullOrWhiteSpace(context.ActivityContentRootId) &&
                !string.Equals(ActivityContentRootId, context.ActivityContentRootId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"[FATAL][PauseSurface] activityContentRootId divergente endpoint='{name}' expected='{context.ActivityContentRootId}' actual='{ActivityContentRootId}' operation='{operation}'.");
            }
        }

        private void ValidateActivityContentOrFail(SessionActivityRoutePauseSurfaceContext context, string operation)
        {
            ValidateOverlayOrFail(context, operation);

            if (string.IsNullOrWhiteSpace(ActivityContentRootId))
            {
                throw new InvalidOperationException(
                    $"[FATAL][PauseContent] activityContentRootId obrigatorio ausente endpoint='{name}' operation='{operation}'.");
            }

            if (activityContentRoot == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][PauseContent] activityContentRoot obrigatorio ausente endpoint='{name}' surfaceId='{SurfaceId}' activityContentRootId='{ActivityContentRootId}' operation='{operation}'.");
            }
        }

        private static string FormatOptionalName(GameObject root)
        {
            return root != null ? root.name : "<none>";
        }

        private static void ApplyVisible(GameObject root, bool visible)
        {
            root.SetActive(visible);

            var canvasGroup = root.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        private readonly struct ActivityPauseContentInstanceRecord
        {
            public ActivityPauseContentInstanceRecord(
                string pipelineId,
                string sessionStateId,
                string activityId,
                int entrySequence,
                string profileId,
                string slotId,
                GameObject instance)
            {
                PipelineId = pipelineId.TrimToEmpty();
                SessionStateId = sessionStateId.TrimToEmpty();
                ActivityId = activityId.TrimToEmpty();
                EntrySequence = entrySequence;
                ProfileId = profileId.TrimToEmpty();
                SlotId = slotId.TrimToEmpty();
                Instance = instance;
            }

            public string PipelineId { get; }
            public string SessionStateId { get; }
            public string ActivityId { get; }
            public int EntrySequence { get; }
            public string ProfileId { get; }
            public string SlotId { get; }
            public GameObject Instance { get; }

            public bool Matches(SessionActivityIdentity identity)
            {
                return string.Equals(PipelineId, identity.PipelineId, StringComparison.Ordinal) &&
                    string.Equals(SessionStateId, identity.SessionId, StringComparison.Ordinal) &&
                    string.Equals(ActivityId, identity.ActivityId, StringComparison.Ordinal) &&
                    EntrySequence == identity.EntrySequence;
            }
        }
    }
}
