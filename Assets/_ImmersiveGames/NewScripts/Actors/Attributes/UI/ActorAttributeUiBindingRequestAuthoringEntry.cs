using System;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.UI
{
    [Serializable]
    public sealed class ActorAttributeUiBindingRequestAuthoringEntry
    {
        [Header("Request")]
        [Tooltip("Se desativado, a entrada e ignorada pelo provider.")]
        [SerializeField] private bool requestEnabled = true;
        [SerializeField] private ActorAttributeUiTargetSelectorKind selectorKind = ActorAttributeUiTargetSelectorKind.PrimaryPlayer;

        [Header("Explicit Selector Values")]
        [Tooltip("Usado apenas quando Selector Kind for ExplicitActorId.")]
        [SerializeField] private ActorId explicitActorId;
        [Tooltip("Usado apenas quando Selector Kind for ExplicitActorInstanceRuntimeId.")]
        [SerializeField] private ActorInstanceRuntimeId explicitActorInstanceRuntimeId;

        [Header("Binding")]
        [SerializeField] private ActorAttributeId attributeId;
        [SerializeField] private ActorAttributeImageFillSink imageFillSink;

        public bool RequestEnabled => requestEnabled;
        public ActorAttributeUiTargetSelectorKind SelectorKind => selectorKind;
        public ActorId ExplicitActorId => explicitActorId;
        public ActorInstanceRuntimeId ExplicitActorInstanceRuntimeId => explicitActorInstanceRuntimeId;
        public ActorAttributeId AttributeId => attributeId;
        public ActorAttributeImageFillSink ImageFillSink => imageFillSink;
    }
}
