using UnityEngine;
namespace _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup
{
    [DisallowMultipleComponent]
    public sealed class PlayerActorPlacementMarker : MonoBehaviour
    {
        [SerializeField] private string placementId;
        [SerializeField] private string playerId;
        [SerializeField] private string slotId;

        public string PlacementId => Normalize(placementId);
        public string PlayerId => Normalize(playerId);
        public string SlotId => Normalize(slotId);
        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;
        public bool IsValid => !string.IsNullOrWhiteSpace(PlacementId);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
