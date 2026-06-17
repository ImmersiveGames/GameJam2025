using System;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.UI
{
    [Serializable]
    public readonly struct ActorAttributeUiBindingRequest
    {
        public ActorAttributeUiBindingRequest(
            ActorAttributeUiTargetSelector selector,
            ActorAttributeId attributeId)
        {
            Selector = selector;
            AttributeId = attributeId;
        }

        public ActorAttributeUiTargetSelector Selector { get; }
        public ActorAttributeId AttributeId { get; }

        public bool IsValid =>
            Selector != null &&
            Selector.IsStructurallyValid &&
            AttributeId.IsValid;

        public string GetInvalidReason()
        {
            if (Selector == null)
            {
                return "selector_missing";
            }

            if (!Selector.IsStructurallyValid)
            {
                return Selector.GetInvalidReason();
            }

            if (!AttributeId.IsValid)
            {
                return "attribute_id_missing";
            }

            return string.Empty;
        }
    }
}
