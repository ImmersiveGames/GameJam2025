using _ImmersiveGames.NewScripts.Foundation.Core.Identifiers;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.GameplayRuntime.ActorRegistry;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Player.Movement;
using _ImmersiveGames.NewScripts.GameplayRuntime.StateGate.Core;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.Spawn
{
    /// <summary>
    /// Serviço de spawn para instanciar o Player real no baseline.
    /// </summary>
    public sealed class PlayerSpawnService : ActorSpawnServiceBase
    {
        private readonly IGameplayStateGate _gameplayStateService;
        private readonly ISpawnResetParticipationReadPort _participationReadPort;

        public PlayerSpawnService(
            IUniqueIdFactory uniqueIdFactory,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context,
            GameObject prefab,
            IGameplayStateGate gameplayStateService,
            ISpawnResetParticipationReadPort participationReadPort)
            : base(uniqueIdFactory, actorRegistry, context, prefab)
        {
            _gameplayStateService = gameplayStateService;
            _participationReadPort = participationReadPort;
        }

        public override string Name => nameof(PlayerSpawnService);

        public override ActorKind SpawnedActorKind => ActorKind.Player;

        public override bool IsRequiredForWorldReset => true;

        protected override IActor ResolveActor(GameObject instance) =>
            PlayerSpawnActorResolver.ResolvePlayerActor(instance);

        protected override void OnPostInstantiate(GameObject instance)
        {
            EnsureMovementStack(instance);
            LogParticipationBridge();
            GameplayStateControllerInjector.TryInject<PlayerMovementController>(
                instance,
                _gameplayStateService,
                static (controller, stateService) => controller.InjectStateService(stateService));
        }

        private static void EnsureMovementStack(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            var input = instance.GetComponent<PlayerMoveInputReader>() ?? instance.AddComponent<PlayerMoveInputReader>();
            var controller = instance.GetComponent<PlayerMovementController>() ?? instance.AddComponent<PlayerMovementController>();

            if (controller != null && input != null)
            {
                controller.SetInputReader(input);
            }
        }

        private void LogParticipationBridge()
        {
            if (_participationReadPort == null || !_participationReadPort.TryGetCurrent(out var snapshot))
            {
                return;
            }

            DebugUtility.Log(typeof(PlayerSpawnService),
                $"[OBS][Gameplay][SpawnBridge] Player spawn consumed participation signature='{snapshot.Signature}' readiness='{snapshot.ReadinessState}' localParticipantId='{snapshot.LocalParticipantId}' primaryParticipantId='{snapshot.PrimaryParticipantId}'.");
        }
    }
}

