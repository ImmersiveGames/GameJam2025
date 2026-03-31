using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
using UnityEngine.Serialization;
namespace _ImmersiveGames.NewScripts.Infrastructure.Pooling.Config
{
    [CreateAssetMenu(
        fileName = "PoolDefinitionAsset",
        menuName = "ImmersiveGames/NewScripts/Infrastructure/Pooling/PoolDefinitionAsset",
        order = 20)]
    public sealed class PoolDefinitionAsset : ScriptableObject
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int initialSize = 1;
        [SerializeField] private bool canExpand = true;
        [SerializeField] private int maxSize = 32;
        [SerializeField] private float autoReturnSeconds;
        [SerializeField] private string poolLabel = "pool";
        [FormerlySerializedAs("prewarmOnEnsure")]
        [SerializeField] private bool prewarm;

        public GameObject Prefab => prefab;
        public int InitialSize => initialSize;
        public bool CanExpand => canExpand;
        public int MaxSize => maxSize;
        public float AutoReturnSeconds => autoReturnSeconds;
        public string PoolLabel => poolLabel;
        public bool Prewarm => prewarm;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (prefab == null)
            {
                FailFast("PoolDefinitionAsset invalid: 'prefab' is required.");
            }

            if (initialSize < 0)
            {
                FailFast("PoolDefinitionAsset invalid: 'initialSize' must be >= 0.");
            }

            if (canExpand)
            {
                if (maxSize <= 0)
                {
                    FailFast("PoolDefinitionAsset invalid: 'maxSize' must be > 0 when 'canExpand' is true.");
                }

                if (maxSize < initialSize)
                {
                    FailFast("PoolDefinitionAsset invalid: 'maxSize' must be >= 'initialSize' when 'canExpand' is true.");
                }
            }
            else if (maxSize < initialSize)
            {
                FailFast("PoolDefinitionAsset invalid: 'maxSize' must be >= 'initialSize'.");
            }

        }
#endif

        private void FailFast(string message)
        {
            DebugUtility.LogError(typeof(PoolDefinitionAsset), $"[FATAL][Pooling][Config] {message} asset='{name}'.");
            throw new InvalidOperationException(message);
        }
    }
}
