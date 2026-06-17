using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Actors.Attributes.Authoring
{
    [CreateAssetMenu(
        fileName = "ActorAttributeDefinition",
        menuName = "ImmersiveGames/Actors/Attributes/Attribute Definition")]
    public sealed class ActorAttributeDefinitionAsset : ScriptableObject
    {
        [SerializeField] private string attributeId = string.Empty;
        [SerializeField] private ActorAttributeSemanticKind semanticKind = ActorAttributeSemanticKind.Custom;
        [SerializeField] private string displayName = string.Empty;
        [TextArea]
        [SerializeField] private string description = string.Empty;

        public string AttributeId => attributeId;
        public ActorAttributeSemanticKind SemanticKind => semanticKind;
        public string DisplayName => displayName;
        public string Description => description;

        public ActorAttributeId ToRuntimeId()
        {
            return new ActorAttributeId(attributeId);
        }

        public bool TryValidate(out string reason)
        {
            if (string.IsNullOrWhiteSpace(attributeId))
            {
                reason = "attribute_id_missing";
                return false;
            }

            reason = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!string.IsNullOrWhiteSpace(attributeId))
            {
                attributeId = attributeId.Trim();
            }

            if (!string.IsNullOrWhiteSpace(displayName))
            {
                displayName = displayName.Trim();
            }
        }
#endif
    }
}
