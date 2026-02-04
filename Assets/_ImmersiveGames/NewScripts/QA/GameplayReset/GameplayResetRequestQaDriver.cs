#if UNITY_EDITOR || DEVELOPMENT_BUILD || NEWSCRIPTS_QA
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.Reset;
using _ImmersiveGames.NewScripts.Runtime.Actors;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.GameplayReset
{
    /// <summary>
    /// Driver de QA para exercitar variações de GameplayResetRequest.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayResetRequestQaDriver : MonoBehaviour
    {
        [Header("Request Config")]
        [SerializeField]
        private List<string> actorIds = new();

        [SerializeField]
        private ActorKind fillKind = ActorKind.Player;

        [SerializeField]
        private bool verboseLogs = true;

        private string _sceneName;
        private IActorRegistry _actorRegistry;
        private IGameplayResetOrchestrator _orchestrator;
        private IGameplayResetTargetClassifier _classifier;
        private readonly List<IActor> _actorBuffer = new(16);
        private readonly List<IActor> _resolvedTargets = new(16);
        private readonly IGameplayResetTargetClassifier _fallbackClassifier = new DefaultGameplayResetTargetClassifier();

        private void Awake()
        {
            _sceneName = gameObject.scene.name;
        }

        [ContextMenu("QA/GameplayResetRequest/Fill ActorIds From Registry (Kind)")]
        public void QA_FillActorIdsFromRegistry()
        {
            EnsureDependencies();

            if (_actorRegistry == null)
            {
                DebugUtility.LogWarning(typeof(GameplayResetRequestQaDriver),
                    "[QA][GameplayResetRequest] IActorRegistry ausente; não foi possível preencher ActorIds.");
                return;
            }

            _actorBuffer.Clear();
            _actorRegistry.GetActors(_actorBuffer);

            actorIds.Clear();

            foreach (var actor in _actorBuffer)
            {
                if (actor is not IActorKindProvider provider)
                {
                    continue;
                }

                if (provider.Kind != fillKind)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(actor.ActorId))
                {
                    actorIds.Add(actor.ActorId);
                }
            }

            actorIds = actorIds.Distinct(StringComparer.Ordinal).ToList();
            actorIds.Sort(StringComparer.Ordinal);

            if (verboseLogs)
            {
                DebugUtility.Log(typeof(GameplayResetRequestQaDriver),
                    $"[QA][GameplayResetRequest] ActorIds preenchidos (kind={fillKind}, count={actorIds.Count}).");
            }
            else
            {
                DebugUtility.Log(typeof(GameplayResetRequestQaDriver),
                    $"[QA][GameplayResetRequest] ActorIds preenchidos (count={actorIds.Count}).");
            }
        }

        [ContextMenu("QA/GameplayResetRequest/Run AllActorsInScene")]
        public void QA_RunAllActorsInScene()
        {
            _ = RunResetAsync(new GameplayResetRequest(
                GameplayResetTarget.AllActorsInScene,
                reason: "QA/GameplayResetRequestAllActors"));
        }

        [ContextMenu("QA/GameplayResetRequest/Run PlayersOnly")]
        public void QA_RunPlayersOnly()
        {
            _ = RunResetAsync(new GameplayResetRequest(
                GameplayResetTarget.PlayersOnly,
                reason: "QA/GameplayResetRequestPlayersOnly",
                actorKind: ActorKind.Player));
        }

        [ContextMenu("QA/GameplayResetRequest/Run EaterOnly")]
        public void QA_RunEaterOnly()
        {
            // IMPORTANT: explicitar ActorKind para não cair no default do enum (ex.: Player).
            _ = RunResetAsync(new GameplayResetRequest(
                GameplayResetTarget.EaterOnly,
                reason: "QA/GameplayResetRequestEaterOnly",
                actorKind: ActorKind.Eater));
        }

        [ContextMenu("QA/GameplayResetRequest/Run ActorIdSet")]
        public void QA_RunActorIdSet()
        {
            _ = RunResetAsync(new GameplayResetRequest(
                GameplayResetTarget.ActorIdSet,
                reason: "QA/GameplayResetRequestActorIdSet",
                actorIds: actorIds));
        }

        [ContextMenu("QA/GameplayResetRequest/Run ByActorKind (FillKind)")]
        public void QA_RunByActorKind()
        {
            _ = RunResetAsync(GameplayResetRequest.ByActorKind(
                fillKind,
                reason: "QA/GameplayResetRequestByActorKind"));
        }

        private async Task RunResetAsync(GameplayResetRequest request)
        {
            EnsureDependencies();

            DebugUtility.Log(typeof(GameplayResetRequestQaDriver),
                $"[QA][GameplayResetRequest] Request => {request} (scene='{_sceneName}')");

            LogResolvedTargets(request);

            if (_orchestrator == null)
            {
                DebugUtility.LogWarning(typeof(GameplayResetRequestQaDriver),
                    $"[QA][GameplayResetRequest] IGameplayResetOrchestrator não encontrado na cena '{_sceneName}'.");
                return;
            }

            try
            {
                await _orchestrator.RequestResetAsync(request);
                DebugUtility.Log(typeof(GameplayResetRequestQaDriver),
                    $"[QA][GameplayResetRequest] Completed => {request} (scene='{_sceneName}')");
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(GameplayResetRequestQaDriver),
                    $"[QA][GameplayResetRequest] Failed => {request}. ex={ex}");
            }
        }

        private void LogResolvedTargets(GameplayResetRequest request)
        {
            if (_actorRegistry == null)
            {
                DebugUtility.LogWarning(typeof(GameplayResetRequestQaDriver),
                    "[QA][GameplayResetRequest] IActorRegistry ausente; não foi possível resolver targets.");
                return;
            }

            _resolvedTargets.Clear();
            _classifier.CollectTargets(request, _actorRegistry, _resolvedTargets);

            if (_resolvedTargets.Count == 0)
            {
                DebugUtility.LogWarning(typeof(GameplayResetRequestQaDriver),
                    $"[QA][GameplayResetRequest] Resolved targets: 0 (target={request.Target}).");
                return;
            }

            if (!verboseLogs)
            {
                return;
            }

            var labels = new List<string>(_resolvedTargets.Count);
            foreach (var actor in _resolvedTargets)
            {
                if (actor == null)
                {
                    continue;
                }

                string name = string.IsNullOrWhiteSpace(actor.DisplayName) ? actor.ActorId : actor.DisplayName;
                labels.Add($"{name}:{actor.ActorId}");
            }

            string labelText = labels.Count > 0 ? string.Join(", ", labels) : "<none>";

            DebugUtility.Log(typeof(GameplayResetRequestQaDriver),
                $"[QA][GameplayResetRequest] Resolved targets: {_resolvedTargets.Count} => {labelText}");
        }

        private void EnsureDependencies()
        {
            if (string.IsNullOrWhiteSpace(_sceneName))
            {
                _sceneName = gameObject.scene.name;
            }

            var provider = DependencyManager.Provider;
            provider.TryGetForScene(_sceneName, out _actorRegistry);
            provider.TryGetForScene(_sceneName, out _orchestrator);
            provider.TryGetForScene(_sceneName, out _classifier);
            _classifier ??= _fallbackClassifier;
        }
    }
}
#endif

