using _ImmersiveGames.NewScripts.Core.Infrastructure.Pooling.Contracts;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Core.Infrastructure.Pooling.QA
{
    /// <summary>
    /// Mock neutro para observar lifecycle do pooling em Play Mode.
    /// Deve estar presente no prefab usado pelo PoolDefinitionAsset de QA.
    /// </summary>
    public sealed class PoolingQaMockPooledObject : PooledBehaviour
    {
        [SerializeField] private string qaLabel = "qa-mock";

        public int CreatedCount { get; private set; }
        public int RentedCount { get; private set; }
        public int ReturnedCount { get; private set; }
        public int DestroyedCount { get; private set; }

        // Comentario em portugues: hooks simples para auditoria manual no Inspector/log.
        protected override void OnAfterPoolCreated()
        {
            CreatedCount++;
            DebugUtility.LogVerbose(typeof(PoolingQaMockPooledObject),
                $"[QA][Pooling] MockCreated label='{qaLabel}' go='{name}' created={CreatedCount}.",
                DebugUtility.Colors.Info);
        }

        protected override void OnAfterPoolRent()
        {
            RentedCount++;
            DebugUtility.Log(typeof(PoolingQaMockPooledObject),
                $"[QA][Pooling] MockRent label='{qaLabel}' go='{name}' rented={RentedCount} totalRentCount={RentCount}.",
                DebugUtility.Colors.Info);
        }

        protected override void OnAfterPoolReturn()
        {
            ReturnedCount++;
            DebugUtility.Log(typeof(PoolingQaMockPooledObject),
                $"[QA][Pooling] MockReturn label='{qaLabel}' go='{name}' returned={ReturnedCount}.",
                DebugUtility.Colors.Info);
        }

        protected override void OnAfterPoolDestroyed()
        {
            DestroyedCount++;
            DebugUtility.LogVerbose(typeof(PoolingQaMockPooledObject),
                $"[QA][Pooling] MockDestroyed label='{qaLabel}' go='{name}' destroyed={DestroyedCount}.",
                DebugUtility.Colors.Info);
        }
    }
}
