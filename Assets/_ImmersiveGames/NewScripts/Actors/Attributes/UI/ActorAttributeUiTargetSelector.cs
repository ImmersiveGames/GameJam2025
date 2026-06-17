using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.UI
{
    [Serializable]
    public sealed class ActorAttributeUiTargetSelector
    {
        [Header("Selector")]
        [SerializeField] private ActorAttributeUiTargetSelectorKind kind = ActorAttributeUiTargetSelectorKind.PrimaryPlayer;

        [Header("Explicit Values")]
        [Tooltip("Usado apenas quando Kind for ExplicitActorId. Nao resolve ator neste corte.")]
        [SerializeField] private ActorId explicitActorId;
        [Tooltip("Usado apenas quando Kind for ExplicitActorInstanceRuntimeId. Nao resolve ator neste corte.")]
        [SerializeField] private ActorInstanceRuntimeId explicitActorInstanceRuntimeId;

        public ActorAttributeUiTargetSelectorKind Kind => kind;
        public ActorId ExplicitActorId => explicitActorId;
        public ActorInstanceRuntimeId ExplicitActorInstanceRuntimeId => explicitActorInstanceRuntimeId;

        public static ActorAttributeUiTargetSelector Create(ActorAttributeUiTargetSelectorKind kind)
        {
            return new ActorAttributeUiTargetSelector
            {
                kind = kind
            };
        }

        public static ActorAttributeUiTargetSelector Create(ActorId explicitActorId)
        {
            return new ActorAttributeUiTargetSelector
            {
                kind = ActorAttributeUiTargetSelectorKind.ExplicitActorId,
                explicitActorId = explicitActorId
            };
        }

        public static ActorAttributeUiTargetSelector Create(ActorInstanceRuntimeId explicitActorInstanceRuntimeId)
        {
            return new ActorAttributeUiTargetSelector
            {
                kind = ActorAttributeUiTargetSelectorKind.ExplicitActorInstanceRuntimeId,
                explicitActorInstanceRuntimeId = explicitActorInstanceRuntimeId
            };
        }

        public bool IsStructurallyValid => string.IsNullOrWhiteSpace(GetInvalidReason());

        public string GetInvalidReason()
        {
            switch (kind)
            {
                case ActorAttributeUiTargetSelectorKind.PrimaryPlayer:
                    return string.Empty;
                case ActorAttributeUiTargetSelectorKind.ExplicitActorId:
                    return explicitActorId.IsValid ? string.Empty : "explicit_actor_id_missing";
                case ActorAttributeUiTargetSelectorKind.ExplicitActorInstanceRuntimeId:
                    return explicitActorInstanceRuntimeId.IsValid ? string.Empty : "explicit_actor_instance_runtime_id_missing";
                default:
                    return "selector_kind_invalid";
            }
        }
    }
}
