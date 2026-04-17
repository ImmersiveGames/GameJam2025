using _ImmersiveGames.NewScripts.Core.Identifiers;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Game.Gameplay.Actors.Player.Movement;
using _ImmersiveGames.NewScripts.Game.Gameplay.State.Core;
using _ImmersiveGames.NewScripts.Orchestration.SessionIntegration.Runtime;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Game.Gameplay.Spawn
{
    /// <summary>
    /// Serviço de spawn para instanciar o Player real no baseline.
    /// </summary>
    public sealed class PlayerSpawnService : ActorSpawnServiceBase
    {
        private readonly IGameplayStateGate _gameplayStateService;
        private readonly ISessionIntegrationContextService _sessionIntegrationContextService;

        public PlayerSpawnService(
            IUniqueIdFactory uniqueIdFactory,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context,
            GameObject prefab,
            IGameplayStateGate gameplayStateService,
            ISessionIntegrationContextService sessionIntegrationContextService)
            : base(uniqueIdFactory, actorRegistry, context, prefab)
        {
            _gameplayStateService = gameplayStateService;
            _sessionIntegrationContextService = sessionIntegrationContextService;
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
            if (_sessionIntegrationContextService == null || !_sessionIntegrationContextService.TryGetCurrentParticipation(out var snapshot))
            {
                return;
            }

            DebugUtility.Log(typeof(PlayerSpawnService),
                $"[OBS][Gameplay][SpawnBridge] Player spawn consumed participation signature='{snapshot.Signature}' readiness='{snapshot.Readiness.State}' localParticipantId='{snapshot.LocalParticipantId}' primaryParticipantId='{snapshot.PrimaryParticipantId}'.");
        }
    }
}
