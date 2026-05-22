using System;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Presentation.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ActorPresentationContainer : MonoBehaviour
    {
        [SerializeField] private ActorPresentationSlotKind slotKind = ActorPresentationSlotKind.Unknown;
        [SerializeField] private string slotId = string.Empty;
        [SerializeField] private Transform containerTransform;

        public ActorPresentationSlotKind SlotKind => slotKind;
        public string SlotId => Normalize(slotId);
        public Transform ContainerTransform => containerTransform;
        public bool HasContainerTransform => containerTransform != null;

        public bool IsValid =>
            slotKind != ActorPresentationSlotKind.Unknown &&
            !string.IsNullOrWhiteSpace(SlotId) &&
            containerTransform != null;

        public ActorPresentationSlotBinding ToBinding()
        {
            return new ActorPresentationSlotBinding(slotKind, SlotId, containerTransform);
        }

        public void ValidateOrThrow(string source)
        {
            string origin = string.IsNullOrWhiteSpace(source)
                ? $"{nameof(ActorPresentationContainer)}:{name}"
                : source.Trim();

            if (slotKind == ActorPresentationSlotKind.Unknown)
            {
                throw new InvalidOperationException($"{origin} requires explicit slotKind.");
            }

            if (string.IsNullOrWhiteSpace(SlotId))
            {
                throw new InvalidOperationException($"{origin} requires non-empty slotId.");
            }

            if (containerTransform == null)
            {
                throw new InvalidOperationException($"{origin} requires explicit containerTransform.");
            }
        }

        private void Reset()
        {
            // Default de authoring local: o próprio marker é o container.
            containerTransform = transform;
            slotId = Normalize(slotId);
        }

        private void OnValidate()
        {
            slotId = Normalize(slotId);

            if (containerTransform == null)
            {
                // Não busca objeto externo; apenas mantém o próprio marker como container explícito.
                containerTransform = transform;
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
