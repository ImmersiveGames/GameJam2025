using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionActivityPipeline;
using UnityEngine;
using SessionOperational = _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline.Bindings
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/NewScripts/SessionFlow/Session Operational Sandbox Host")]
    public sealed class SessionOperationalPipelineHost : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private string sessionPipelineId = "SessionOperationalPipeline.Base11.Sandbox";
        [SerializeField] private string sessionStateId = "SessionOperationalSandboxSession";
        [SerializeField] private string routeId = "to-session-activity-sandbox";
        [SerializeField] private string routeProfileId = "session-activity-sandbox";
        [SerializeField] private int transitionSequence = 1;

        private SessionActivityMiniCatalog _catalog;
        private _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionActivityPipeline.SessionActivityPipeline _activityPipeline;
        private SessionOperational.SessionOperationalPipeline _pipeline;

        public SessionOperational.SessionOperationalRuntimeState State => _pipeline != null ? _pipeline.State : null;
        public SessionActivityMiniCatalog Catalog => _catalog;
        public _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionActivityPipeline.SessionActivityPipeline ActivityPipeline => _activityPipeline;

        private void Awake()
        {
            _catalog = new SessionActivityMiniCatalog();
            _activityPipeline = new _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionActivityPipeline.SessionActivityPipeline(_catalog, sessionStateId);
            _pipeline = new SessionOperational.SessionOperationalPipeline(
                _catalog,
                _activityPipeline,
                sessionPipelineId,
                sessionStateId,
                routeId,
                routeProfileId,
                transitionSequence);

            Debug.Log(BuildHostBanner());
        }

        public SessionOperational.SessionOperationalResult StartEnvelopeAndActivity()
        {
            EnsurePipeline();
            SessionOperational.SessionOperationalResult result = _pipeline.StartEnvelopeAndActivity(nameof(SessionOperationalPipelineHost), "Start envelope and activity");
            LogResult("StartEnvelopeAndActivity", result);
            return result;
        }

        public string DumpOperationalState()
        {
            EnsurePipeline();
            string dump = _pipeline.DumpOperationalState();
            return dump;
        }

        private void EnsurePipeline()
        {
            if (_pipeline != null)
            {
                return;
            }

            throw new InvalidOperationException("SessionOperationalPipelineHost pipeline is not initialized.");
        }

        private void LogResult(string action, SessionOperational.SessionOperationalResult result)
        {
            if (!result.IsValid)
            {
                throw new InvalidOperationException($"Invalid result returned by action '{action}'.");
            }

            Debug.Log($"[OBS][SessionOperationalPipeline][Host] action='{action}' outcome='{result.Kind}' reason='{result.Reason}' sessionStateId='{_pipeline.State.SessionStateId}' routeId='{_pipeline.State.RouteId}' routeProfileId='{_pipeline.State.RouteProfileId}' transitionSequence='{_pipeline.State.TransitionSequence}'");

            for (int index = 0; index < result.Facts.Count; index++)
            {
                Debug.Log(result.Facts[index].ToString());
            }

            for (int index = 0; index < result.Snapshots.Count; index++)
            {
                Debug.Log(result.Snapshots[index].ToString());
            }

        }

        private string BuildHostBanner()
        {
            return $"[OBS][SessionOperationalPipeline][Host] initialized sessionPipelineId='{sessionPipelineId}' sessionStateId='{sessionStateId}' routeId='{routeId}' routeProfileId='{routeProfileId}' transitionSequence='{transitionSequence}'";
        }
    }
}
