using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup
{
    [DisallowMultipleComponent]
    public sealed class PlayerActorPlacementMarker : MonoBehaviour
    {
        [SerializeField] private string placementId;

        public string PlacementId => placementId.TrimToEmpty();
        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;
        public bool IsValid => !string.IsNullOrWhiteSpace(PlacementId);
}
}
