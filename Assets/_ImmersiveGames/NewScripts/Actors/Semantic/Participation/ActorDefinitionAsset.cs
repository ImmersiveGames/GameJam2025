using _ImmersiveGames.NewScripts.Foundation.Core.Identifiers;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Actors.Semantic.Participation
{
    public enum ActorDefinitionKind
    {
        Unknown = 0,
        Player = 1,
        SceneActor = 2,
        Object = 3,
    }

    public enum ActorPlacementMode
    {
        None = 0,
        SceneMarker = 1,
        FixedTransform = 2,
    }

    [CreateAssetMenu(
        fileName = "ActorDefinition",
        menuName = "ImmersiveGames/Actors/Semantic/Actor Definition",
        order = 59)]
    public sealed class ActorDefinitionAsset : ScriptableObject
    {
        private static readonly IUniqueIdFactory IdFactory = new UniqueIdFactory();

        [SerializeField, Tooltip("Canonical authoring definition id. This identifies the ActorDefinition asset/domain, not the runtime actor.")]
        private string actorDefinitionId;
        [SerializeField] private string displayName;
        [SerializeField] private ActorDefinitionKind actorKind = ActorDefinitionKind.Unknown;
        [SerializeField] private GameObject prefabReference;
        [SerializeField] private ActorPlacementMode placementMode = ActorPlacementMode.None;
        [SerializeField] private string placementKey;
        [SerializeField] private Vector3 localPosition;
        [SerializeField] private Vector3 localRotation;

        public string ActorDefinitionId => Normalize(actorDefinitionId);
        public string DisplayName => Normalize(displayName);
        public ActorDefinitionKind ActorKind => actorKind;
        public GameObject PrefabReference => prefabReference;
        public ActorPlacementMode PlacementMode => placementMode;
        public string PlacementKey => Normalize(placementKey);
        public Vector3 LocalPosition => localPosition;
        public Vector3 LocalRotation => localRotation;
        public bool HasPlacementPlan => ResolveHasPlacementPlan(placementMode, placementKey);

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureDefinitionIdentityGenerated();
        }
#endif

        public bool TryValidate(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(ActorDefinitionId))
            {
                errorMessage = "actorDefinitionId is required.";
                return false;
            }

            if (actorKind == ActorDefinitionKind.Unknown)
            {
                errorMessage = $"actorKind cannot be Unknown actorDefinitionId='{ActorDefinitionId}'.";
                return false;
            }

            if (!IsValidPlacementMode(placementMode))
            {
                errorMessage = $"placementMode is invalid actorDefinitionId='{ActorDefinitionId}' placementMode='{placementMode}'.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private void EnsureDefinitionIdentityGenerated()
        {
            if (string.IsNullOrWhiteSpace(actorDefinitionId))
            {
                string generatedDefinitionId = IdFactory.GenerateId(null, "ActorDefinition");
                if (!string.IsNullOrWhiteSpace(generatedDefinitionId))
                {
                    actorDefinitionId = generatedDefinitionId.Trim();
                }
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static bool IsValidPlacementMode(ActorPlacementMode value)
        {
            return value == ActorPlacementMode.None ||
                   value == ActorPlacementMode.SceneMarker ||
                   value == ActorPlacementMode.FixedTransform;
        }

        private static bool ResolveHasPlacementPlan(ActorPlacementMode mode, string key)
        {
            return mode == ActorPlacementMode.FixedTransform ||
                   mode == ActorPlacementMode.SceneMarker && !string.IsNullOrWhiteSpace(key);
        }
    }
}
