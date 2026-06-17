using System;
using _ImmersiveGames.NewScripts.Actors.Attributes.Authoring;
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
        [Tooltip("Definition asset do atributo. O ID runtime e derivado do asset para evitar digitacao manual no binding.")]
        [SerializeField] private ActorAttributeDefinitionAsset attributeDefinition;
        [SerializeField] private ActorAttributeImageFillSink imageFillSink;

        public bool RequestEnabled => requestEnabled;
        public ActorAttributeUiTargetSelectorKind SelectorKind => selectorKind;
        public ActorId ExplicitActorId => explicitActorId;
        public ActorInstanceRuntimeId ExplicitActorInstanceRuntimeId => explicitActorInstanceRuntimeId;
        public ActorAttributeDefinitionAsset AttributeDefinition => attributeDefinition;
        public ActorAttributeId AttributeId => ActorAttributeId.FromDefinition(attributeDefinition);
        public ActorAttributeImageFillSink ImageFillSink => imageFillSink;
    }
}
