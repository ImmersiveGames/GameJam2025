using _ImmersiveGames.NewScripts.Foundation.Core.Identifiers;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Actors.Semantic.Participation
{
    public enum ActorDefinitionKind
    {
        Unknown = 0,
        Player = 1,
        NPC = 2,
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
        menuName = "ImmersiveGames/NewScripts/Actors/Semantic/Actor Definition",
        order = 59)]
    public sealed class ActorDefinitionAsset : ScriptableObject
    {
        private static readonly IUniqueIdFactory IdFactory = new UniqueIdFactory();

        [SerializeField, Tooltip("Canonical auto-generated actorId. Do not edit manually.")]
        private string actorId;
        [SerializeField] private string displayName;
        [SerializeField] private ActorDefinitionKind actorKind = ActorDefinitionKind.Unknown;
        [SerializeField] private GameObject prefabReference;
        [SerializeField] private ActorPlacementMode placementMode = ActorPlacementMode.None;
        [SerializeField] private string placementKey;
        [SerializeField] private Vector3 localPosition;
        [SerializeField] private Vector3 localRotation;

        public string ActorId => Normalize(actorId);
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
            EnsureActorIdGenerated();
        }
#endif

        public bool TryValidate(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(ActorId))
            {
                errorMessage = "actorId is required.";
                return false;
            }

            if (actorKind == ActorDefinitionKind.Unknown)
            {
                errorMessage = $"actorKind cannot be Unknown actorId='{ActorId}'.";
                return false;
            }

            if (!IsValidPlacementMode(placementMode))
            {
                errorMessage = $"placementMode is invalid actorId='{ActorId}' placementMode='{placementMode}'.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private void EnsureActorIdGenerated()
        {
            if (!string.IsNullOrWhiteSpace(actorId))
            {
                return;
            }

            string generated = IdFactory.GenerateId(null, "ActorDefinition");
            if (!string.IsNullOrWhiteSpace(generated))
            {
                actorId = generated.Trim();
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
                   (mode == ActorPlacementMode.SceneMarker && !string.IsNullOrWhiteSpace(key));
        }
    }
}
