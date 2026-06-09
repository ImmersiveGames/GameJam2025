using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using UnityEngine;
using UnityEngine.Serialization;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config
{
    [CreateAssetMenu(
        fileName = "PoolDefinitionAsset",
        menuName = "ImmersiveGames/Infrastructure/Pooling/PoolDefinitionAsset",
        order = 20)]
    public sealed class PoolDefinitionAsset : ScriptableObject
    {
        [Header("Prefab")]
        [SerializeField, Tooltip("Prefab técnico alugado por este pool. A identidade final de gameplay deve ser aplicada no rent, não no prefab.")]
        private GameObject prefab;

        [Header("Capacity")]
        [SerializeField, Tooltip("Quantidade inicial criada quando o pool é preparado/preaquecido.")]
        private int initialSize = 1;
        [SerializeField, Tooltip("Permite criar novas instâncias quando o pool esgota.")]
        private bool canExpand = true;
        [SerializeField, Tooltip("Limite máximo de instâncias quando Can Expand está ativo.")]
        private int maxSize = 32;

        [Header("Lifetime")]
        [SerializeField, Tooltip("Retorno automático técnico em segundos. 0 desativa. Para projectiles, o reset canônico futuro deve retornar ao pool por policy/objeto, não por fallback silencioso.")]
        private float autoReturnSeconds;

        [Header("Identity / Bootstrap")]
        [SerializeField, Tooltip("Label técnico observacional do pool. Não deve substituir referência tipada ao PoolDefinitionAsset.")]
        private string poolLabel = "pool";
        [FormerlySerializedAs("prewarmOnEnsure")]
        [SerializeField, Tooltip("Se ativo, o pool é preaquecido quando garantido pelo serviço canônico.")]
        private bool prewarm;

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

