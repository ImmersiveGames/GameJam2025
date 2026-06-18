using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Presentation.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PoolableSpawnOriginAnchor : MonoBehaviour
    {
        [SerializeField] private string originId = string.Empty;
        [SerializeField] private PoolableSpawnOriginKind originKind = PoolableSpawnOriginKind.Unknown;
        [SerializeField] private Transform originTransform;

        public PoolableSpawnOriginId OriginId => new(originId);
        public PoolableSpawnOriginKind OriginKind => originKind;
        public Transform OriginTransform => originTransform;

        public bool IsValid =>
            OriginId.IsValid &&
            originKind != PoolableSpawnOriginKind.Unknown &&
            OriginTransform != null;

        private void Reset()
        {
            originTransform = transform;
            originId = originId.TrimToEmpty();
        }

        private void OnValidate()
        {
            originId = originId.TrimToEmpty();

            if (originTransform == null)
            {
                originTransform = transform;
            }
        }
}
}
