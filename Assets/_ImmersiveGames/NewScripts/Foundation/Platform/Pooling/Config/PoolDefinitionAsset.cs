using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using UnityEngine;
using UnityEngine.Serialization;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config
{
    public enum PoolLifetimeScope
    {
        Global = 0,
        Activity = 1,
    }

    public enum PoolRegistrationMode
    {
        LazyOnFirstRent = 0,
        ExplicitPrepareOnly = 1,
        ActivityEntry = 2,
        RouteEntry = 3,
        GlobalBoot = 4
    }

    [CreateAssetMenu(
        fileName = "PoolDefinitionAsset",
        menuName = "ImmersiveGames/Infrastructure/Pooling/PoolDefinitionAsset",
        order = 20)]
    public sealed class PoolDefinitionAsset : ScriptableObject
    {
        [Header("Objeto reutilizado")]
        [SerializeField, InspectorName("Prefab reutilizado"), Tooltip("Prefab técnico alugado por este pool. A identidade final de gameplay deve ser aplicada no rent, não no prefab.")]
        private GameObject prefab;

        [Header("Capacidade")]
        [SerializeField, InspectorName("Instâncias iniciais"), Tooltip("Quantidade inicial criada quando o pool é preparado/preaquecido.")]
        private int initialSize = 1;
        [SerializeField, InspectorName("Pode crescer se acabar"), Tooltip("Permite criar novas instâncias quando o pool esgota.")]
        private bool canExpand = true;
        [SerializeField, InspectorName("Limite máximo"), Tooltip("Limite máximo de instâncias quando 'Pode crescer se acabar' está ativo.")]
        private int maxSize = 32;

        [Header("Lifetime técnico")]
        [SerializeField, InspectorName("Escopo do pool"), Tooltip("Escopo de vida do pool. Global preserva o comportamento atual. Activity materializa apenas durante rotas SessionActivity e libera o pool ao sair delas.")]
        private PoolLifetimeScope lifetimeScope = PoolLifetimeScope.Global;
        [SerializeField, InspectorName("Retorno técnico automático (s)"), Tooltip("Retorno automático técnico em segundos. 0 desativa. Lifetime de gameplay deve ficar em profile/policy do objeto gerado quando esse fluxo existir.")]
        private float autoReturnSeconds;

        [Header("Debug / bootstrap")]
        [SerializeField, InspectorName("Nome amigável do pool"), Tooltip("Label observacional do pool no Inspector/log. Não substitui referência tipada ao PoolDefinitionAsset.")]
        private string poolLabel = "pool";
        [SerializeField, InspectorName("Quando registrar"), Tooltip("Define quando o pool deve ser registrado pelo owner canônico. LazyOnFirstRent mantém o registro no primeiro Rent; os modos explícitos dependem de pipeline/composer dedicados.")]
        private PoolRegistrationMode registrationMode = PoolRegistrationMode.LazyOnFirstRent;
        [FormerlySerializedAs("prewarmOnEnsure")]
        [SerializeField, InspectorName("Criar instâncias ao registrar"), Tooltip("Se ativo, o pool cria initialSize no momento em que for registrado. Não decide quando o registro acontece.")]
        private bool prewarm;

        public GameObject Prefab => prefab;
        public int InitialSize => initialSize;
        public bool CanExpand => canExpand;
        public int MaxSize => maxSize;
        public PoolLifetimeScope LifetimeScope => lifetimeScope;
        public float AutoReturnSeconds => autoReturnSeconds;
        public string PoolLabel => poolLabel;
        public PoolRegistrationMode RegistrationMode => registrationMode;
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

            if (!Enum.IsDefined(typeof(PoolRegistrationMode), registrationMode))
            {
                FailFast("PoolDefinitionAsset invalid: 'registrationMode' must be a defined value.");
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
