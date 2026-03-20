using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Pooling.Config;
using _ImmersiveGames.NewScripts.Infrastructure.Pooling.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Pooling.Runtime
{
    /// <summary>
    /// Package A implementation: global DI wiring + explicit stubs.
    /// Runtime behavior (prewarm/rent/return) is intentionally deferred to Package B.
    /// </summary>
    public sealed class PoolService : IPoolService
    {
        private readonly HashSet<PoolDefinitionAsset> _knownDefinitions = new();

        public PoolService()
        {
            IsBootstrapped = true;
            DebugUtility.Log(typeof(PoolService),
                "[BOOT][Pooling] PoolService bootstrapped (Package A wiring-only).",
                DebugUtility.Colors.Info);
        }

        public bool IsBootstrapped { get; }

        public void EnsureRegistered(PoolDefinitionAsset definition)
        {
            var validatedDefinition = ValidateDefinition(definition);
            if (_knownDefinitions.Add(validatedDefinition))
            {
                DebugUtility.LogVerbose(typeof(PoolService),
                    $"[BOOT][Pooling] Registered pool definition (asset='{validatedDefinition.name}', label='{Sanitize(validatedDefinition.PoolLabel)}').",
                    DebugUtility.Colors.Info);
            }
        }

        public void Prewarm(PoolDefinitionAsset definition)
        {
            EnsureRegistered(definition);
            ThrowPackageBNotImplemented(nameof(Prewarm), definition);
        }

        public GameObject Rent(PoolDefinitionAsset definition, Transform parent = null)
        {
            EnsureRegistered(definition);
            ThrowPackageBNotImplemented(nameof(Rent), definition);
            return null;
        }

        public void Return(PoolDefinitionAsset definition, GameObject instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance),
                    "Pooling Return requires a non-null GameObject instance.");
            }

            EnsureRegistered(definition);
            ThrowPackageBNotImplemented(nameof(Return), definition);
        }

        public void Shutdown()
        {
            _knownDefinitions.Clear();
            DebugUtility.Log(typeof(PoolService),
                "[BOOT][Pooling] PoolService shutdown complete (Package A).",
                DebugUtility.Colors.Info);
        }

        private static PoolDefinitionAsset ValidateDefinition(PoolDefinitionAsset definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition),
                    "Pooling identity must be a PoolDefinitionAsset reference (null is invalid).");
            }

            return definition;
        }

        private static void ThrowPackageBNotImplemented(string operation, PoolDefinitionAsset definition)
        {
            string message =
                $"[Pooling] Operation '{operation}' is intentionally deferred to Package B. asset='{definition.name}'.";
            DebugUtility.LogWarning(typeof(PoolService), message);
            throw new NotSupportedException(message);
        }

        private static string Sanitize(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? "n/a" : text.Trim();
        }
    }
}
