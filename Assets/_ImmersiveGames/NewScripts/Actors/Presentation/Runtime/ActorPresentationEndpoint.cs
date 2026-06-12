using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Presentation.Authoring;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Presentation.Runtime
{
    /// <summary>
    /// Endpoint local de ActorPresentation no root lógico do Actor.
    /// Expõe containers explícitos para stages/adapters futuros.
    /// Não decide lifecycle, não materializa, não reseta e não libera presentation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ActorPresentationEndpoint : MonoBehaviour
    {
        [SerializeField] private string endpointId = "actor.presentation.endpoint";
        [SerializeField] private ActorPresentationProfileAsset profile;
        [SerializeField] private List<ActorPresentationContainer> containers = new List<ActorPresentationContainer>();

        public string EndpointId => Normalize(endpointId);
        public ActorPresentationProfileAsset Profile => profile;
        public IReadOnlyList<ActorPresentationContainer> Containers => (IReadOnlyList<ActorPresentationContainer>)containers ?? Array.Empty<ActorPresentationContainer>();

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(EndpointId) &&
            containers != null;

        public bool TryGetContainer(
            ActorPresentationSlotKind slotKind,
            string slotId,
            out ActorPresentationContainer container)
        {
            container = null;

            if (slotKind == ActorPresentationSlotKind.Unknown)
            {
                return false;
            }

            string normalizedSlotId = Normalize(slotId);
            if (string.IsNullOrWhiteSpace(normalizedSlotId) || containers == null)
            {
                return false;
            }

            for (int index = 0; index < containers.Count; index++)
            {
                var current = containers[index];
                if (current == null)
                {
                    continue;
                }

                if (current.SlotKind == slotKind && string.Equals(current.SlotId, normalizedSlotId, StringComparison.Ordinal))
                {
                    container = current;
                    return true;
                }
            }

            return false;
        }

        public void ValidateOrThrow(string source)
        {
            string origin = string.IsNullOrWhiteSpace(source)
                ? $"{nameof(ActorPresentationEndpoint)}:{name}"
                : source.Trim();

            if (string.IsNullOrWhiteSpace(EndpointId))
            {
                throw new InvalidOperationException($"{origin} requires non-empty endpointId.");
            }

            if (profile == null)
            {
                throw new InvalidOperationException($"{origin} requires ActorPresentationProfileAsset.");
            }

            if (containers == null)
            {
                throw new InvalidOperationException($"{origin} requires containers list.");
            }

            var observedKeys = new HashSet<string>(StringComparer.Ordinal);

            for (int index = 0; index < containers.Count; index++)
            {
                var container = containers[index];
                if (container == null)
                {
                    throw new InvalidOperationException($"{origin} has null container at index '{index}'.");
                }

                container.ValidateOrThrow($"{origin}/container[{index}]");

                string key = BuildKey(container.SlotKind, container.SlotId);
                if (!observedKeys.Add(key))
                {
                    throw new InvalidOperationException($"{origin} has duplicate container key '{key}'.");
                }
            }
        }

        private void OnValidate()
        {
            endpointId = Normalize(endpointId);
            containers ??= new List<ActorPresentationContainer>();
        }

        private static string BuildKey(ActorPresentationSlotKind slotKind, string slotId)
        {
            return $"{slotKind}:{Normalize(slotId)}";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
