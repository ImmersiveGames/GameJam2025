using _ImmersiveGames.NewScripts.Core.Identifiers;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Game.Gameplay.Actors.Player.Movement;
using _ImmersiveGames.NewScripts.Game.Gameplay.State.Core;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Game.Gameplay.Spawn
{
    /// <summary>
    /// Serviço de spawn para instanciar o Player real no baseline.
    /// </summary>
    public sealed class PlayerSpawnService : ActorSpawnServiceBase
    {
        private readonly IGameplayStateGate _gameplayStateService;
        private readonly IGameplayParticipationFlowService _participationFlowService;

        public PlayerSpawnService(
            IUniqueIdFactory uniqueIdFactory,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context,
            GameObject prefab,
            IGameplayStateGate gameplayStateService,
            IGameplayParticipationFlowService participationFlowService)
            : base(uniqueIdFactory, actorRegistry, context, prefab)
        {
            _gameplayStateService = gameplayStateService;
            _participationFlowService = participationFlowService;
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
            if (_participationFlowService == null || !_participationFlowService.TryGetCurrent(out var snapshot))
            {
                return;
            }

            DebugUtility.Log(typeof(PlayerSpawnService),
                $"[OBS][Gameplay][SpawnBridge] Player spawn consumed participation signature='{snapshot.Signature}' readiness='{snapshot.Readiness.State}' localParticipantId='{snapshot.LocalParticipantId}' primaryParticipantId='{snapshot.PrimaryParticipantId}'.");
        }
    }
}
