using System;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using UnityEngine;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{
    public readonly struct SessionPlayerSlotsValidationContext
    {
        public SessionPlayerSlotsValidationContext(
            Transform persistentRoot,
            PlayerInputManager playerInputManager,
            int maxPlayerSlots)
        {
            PersistentRoot = persistentRoot ?? throw new ArgumentNullException(nameof(persistentRoot));
            PlayerInputManager = playerInputManager ?? throw new ArgumentNullException(nameof(playerInputManager));
            MaxPlayerSlots = maxPlayerSlots;
        }

        public Transform PersistentRoot { get; }
        public PlayerInputManager PlayerInputManager { get; }
        public int MaxPlayerSlots { get; }
    }

    public static class SessionPlayerSlotsValidator
    {
        public static SessionPlayerSlotsValidationContext ValidateOrFail(
            RuntimeModeConfig runtimeModeConfig,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            var config = InputModesRuntimeConfigResolver.ResolveOrFail(runtimeModeConfig);

            DebugUtility.Log(typeof(SessionPlayerSlotsValidator),
                BuildLog("SessionPlayerSlotsValidationStarted", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"maxPlayerSlots='{config.MaxPlayerSlots}'"),
                DebugUtility.Colors.Info);

            PlayerInputManager[] playerInputManagers = UnityEngine.Object.FindObjectsByType<PlayerInputManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            if (playerInputManagers == null || playerInputManagers.Length == 0)
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    "PlayerInputManager obrigatorio ausente.");
            }

            if (playerInputManagers.Length > 1)
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"PlayerInputManager duplicado detectado. count='{playerInputManagers.Length}'.");
            }

            var playerInputManager = playerInputManagers[0];

            DebugUtility.Log(typeof(SessionPlayerSlotsValidator),
                BuildLog("PlayerInputManagerObserved", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"playerInputManager='{playerInputManager.name}' observedMaxPlayerCount='{playerInputManager.maxPlayerCount}'"),
                DebugUtility.Colors.Info);

            if (playerInputManager.maxPlayerCount != config.MaxPlayerSlots)
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"PlayerInputManager.maxPlayerCount mismatch. observed='{playerInputManager.maxPlayerCount}' expected='{config.MaxPlayerSlots}'.");
            }

            var persistentRoot = ResolvePersistentRootOrFail(
                playerInputManager,
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                source,
                reason);

            DebugUtility.Log(typeof(SessionPlayerSlotsValidator),
                BuildLog("MaxPlayerSlotsValidated", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"maxPlayerSlots='{config.MaxPlayerSlots}'"),
                DebugUtility.Colors.Success);

            return new SessionPlayerSlotsValidationContext(
                persistentRoot,
                playerInputManager,
                config.MaxPlayerSlots);
        }

        private static Transform ResolvePersistentRootOrFail(
            PlayerInputManager playerInputManager,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            var marker = playerInputManager.GetComponent<PersistentRuntimeObject>();
            if (marker == null)
            {
                marker = playerInputManager.GetComponentInParent<PersistentRuntimeObject>();
            }

            if (marker == null)
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"PersistentRuntimeObject obrigatorio ausente no PlayerInputManager/root. playerInputManager='{playerInputManager.name}'.");
            }

            var root = marker.transform.root;
            if (root == null)
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    "root persistente invalido para validacao de EventSystem.");
            }

            return root;
        }

        private static InvalidOperationException BuildFatal(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason,
            string detail)
        {
            string message = BuildLog(
                "SessionPlayerSlotsValidationFailed",
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                source,
                reason,
                detail);

            DebugUtility.LogError(typeof(SessionPlayerSlotsValidator), $"[FATAL][Config][SessionPlayerSlots] {message}");
            return new InvalidOperationException($"[FATAL][Config][SessionPlayerSlots] {message}");
        }

        private static string BuildLog(
            string eventName,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason,
            string extra)
        {
            return $"[OBS][SessionPlayerSlots] event='{eventName}' routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' source='{source}' reason='{reason}' {extra}.";
        }
    }
}
