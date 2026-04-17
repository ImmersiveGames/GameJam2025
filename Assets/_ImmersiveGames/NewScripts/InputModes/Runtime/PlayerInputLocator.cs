using System;
using System.Linq;
using _ImmersiveGames.NewScripts.ActorSystem.Semantic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace _ImmersiveGames.NewScripts.InputModes.Runtime
{
    internal sealed class PlayerInputLocator : IPlayerInputLocator
    {
        private readonly IActorSystemReadModelService _actorSystemReadModelService;
        private string _lastRelevantActorId = string.Empty;
        private string _lastActorSystemObservationKey = string.Empty;

        public PlayerInputLocator(IActorSystemReadModelService actorSystemReadModelService = null)
        {
            _actorSystemReadModelService = actorSystemReadModelService;
        }

        public PlayerInput[] GetActivePlayerInputs()
        {
            PlayerInput[] all = Object.FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
            if (all == null || all.Length == 0)
            {
                return Array.Empty<PlayerInput>();
            }

            int count = all.Count(pi => pi != null && pi.enabled && pi.gameObject.activeInHierarchy);
            if (count == 0)
            {
                return Array.Empty<PlayerInput>();
            }

            var result = new PlayerInput[count];
            int idx = 0;
            foreach (var pi in all)
            {
                if (pi != null && pi.enabled && pi.gameObject.activeInHierarchy)
                {
                    result[idx++] = pi;
                }
            }

            PrioritizeRelevantActorInput(result);
            return result;
        }

        private void PrioritizeRelevantActorInput(PlayerInput[] inputs)
        {
            if (inputs == null || inputs.Length == 0)
            {
                return;
            }

            if (_actorSystemReadModelService == null)
            {
                const string observationKey = "no-read-model-service";
                if (!string.Equals(_lastActorSystemObservationKey, observationKey, StringComparison.Ordinal))
                {
                    _lastActorSystemObservationKey = observationKey;
                    DebugUtility.LogVerbose(typeof(PlayerInputLocator), "[OBS][InputModes][ActorSystem] Consulta ignorada readModelResolved='false' prioritized='false' reason='sem-read-model' fallback='default-order'.", DebugUtility.Colors.Info);
                }
                return;
            }

            bool fromCurrentSnapshot = _actorSystemReadModelService.TryGetCurrent(out var snapshot);
            if (!fromCurrentSnapshot || !snapshot.IsValid)
            {
                snapshot = _actorSystemReadModelService.Refresh();
                fromCurrentSnapshot = false;
            }

            if (!snapshot.IsValid || string.IsNullOrWhiteSpace(snapshot.RelevantActorId))
            {
                string reason;
                if (!snapshot.IsValid)
                {
                    reason = "sem-read-model";
                }
                else if (string.Equals(snapshot.Reason, "no-runtime-match", StringComparison.Ordinal))
                {
                    reason = "actor-nao-encontrado";
                }
                else
                {
                    reason = "sem-relevant-actor-id";
                }

                string observationKey = $"no-relevant-actor|source={(fromCurrentSnapshot ? "current" : "refresh")}|reason={reason}";
                if (!string.Equals(_lastActorSystemObservationKey, observationKey, StringComparison.Ordinal))
                {
                    _lastActorSystemObservationKey = observationKey;
                    DebugUtility.LogVerbose(typeof(PlayerInputLocator), $"[OBS][InputModes][ActorSystem] Consulta sem priorizacao readModelResolved='true' source='{(fromCurrentSnapshot ? "TryGetCurrent" : "Refresh")}' relevantActorId='<none>' playerInputMatch='false' prioritized='false' reason='{reason}' fallback='default-order'.", DebugUtility.Colors.Info);
                }
                return;
            }

            int relevantIndex = -1;
            for (int index = 0; index < inputs.Length; index += 1)
            {
                if (TryResolveActorId(inputs[index], out string actorId)
                    && string.Equals(actorId, snapshot.RelevantActorId, StringComparison.Ordinal))
                {
                    relevantIndex = index;
                    break;
                }
            }

            if (relevantIndex < 0)
            {
                string observationKey = $"not-found|relevantActorId={snapshot.RelevantActorId}|source={(fromCurrentSnapshot ? "current" : "refresh")}";
                if (!string.Equals(_lastActorSystemObservationKey, observationKey, StringComparison.Ordinal))
                {
                    _lastActorSystemObservationKey = observationKey;
                    DebugUtility.LogVerbose(typeof(PlayerInputLocator), $"[OBS][InputModes][ActorSystem] Consulta sem correspondencia readModelResolved='true' source='{(fromCurrentSnapshot ? "TryGetCurrent" : "Refresh")}' relevantActorId='{snapshot.RelevantActorId}' playerInputMatch='false' prioritized='false' reason='actor-encontrado-sem-playerinput' fallback='default-order'.", DebugUtility.Colors.Info);
                }
                return;
            }

            if (relevantIndex <= 0)
            {
                string observationKey = $"already-prioritized|relevantActorId={snapshot.RelevantActorId}|source={(fromCurrentSnapshot ? "current" : "refresh")}";
                if (!string.Equals(_lastActorSystemObservationKey, observationKey, StringComparison.Ordinal))
                {
                    _lastActorSystemObservationKey = observationKey;
                    DebugUtility.LogVerbose(typeof(PlayerInputLocator), $"[OBS][InputModes][ActorSystem] Consulta com correspondencia readModelResolved='true' source='{(fromCurrentSnapshot ? "TryGetCurrent" : "Refresh")}' relevantActorId='{snapshot.RelevantActorId}' playerInputMatch='true' prioritized='false' reason='already-first'.", DebugUtility.Colors.Info);
                }
                return;
            }

            (inputs[relevantIndex], inputs[0]) = (inputs[0], inputs[relevantIndex]);

            if (!string.Equals(_lastRelevantActorId, snapshot.RelevantActorId, StringComparison.Ordinal))
            {
                _lastRelevantActorId = snapshot.RelevantActorId;
            }

            string prioritizedObservationKey = $"prioritized|relevantActorId={snapshot.RelevantActorId}|source={(fromCurrentSnapshot ? "current" : "refresh")}";
            if (!string.Equals(_lastActorSystemObservationKey, prioritizedObservationKey, StringComparison.Ordinal))
            {
                _lastActorSystemObservationKey = prioritizedObservationKey;
                DebugUtility.LogVerbose(typeof(PlayerInputLocator), $"[OBS][InputModes][ActorSystem] Consulta com correspondencia readModelResolved='true' source='{(fromCurrentSnapshot ? "TryGetCurrent" : "Refresh")}' relevantActorId='{snapshot.RelevantActorId}' playerInputMatch='true' prioritized='true' reason='reordered-to-front'.", DebugUtility.Colors.Info);
            }
        }

        private static bool TryResolveActorId(PlayerInput input, out string actorId)
        {
            actorId = string.Empty;
            if (input == null)
            {
                return false;
            }

            MonoBehaviour[] behaviours = input.GetComponentsInParent<MonoBehaviour>(true);
            for (int index = 0; index < behaviours.Length; index += 1)
            {
                if (behaviours[index] is not IActor actor || string.IsNullOrWhiteSpace(actor.ActorId))
                {
                    continue;
                }

                actorId = actor.ActorId.Trim();
                return true;
            }

            return false;
        }

    }
}
