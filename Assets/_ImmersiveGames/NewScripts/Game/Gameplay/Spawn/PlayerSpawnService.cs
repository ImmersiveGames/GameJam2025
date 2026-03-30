using _ImmersiveGames.NewScripts.Core.Identifiers;
using _ImmersiveGames.NewScripts.Game.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Game.Gameplay.Actors.Player;
using _ImmersiveGames.NewScripts.Game.Gameplay.Actors.Player.Movement;
using _ImmersiveGames.NewScripts.Game.Gameplay.State;
using _ImmersiveGames.NewScripts.Game.Gameplay.State.Core;
using _ImmersiveGames.NewScripts.Orchestration.SceneReset.Spawn;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Game.Gameplay.Spawn
{
    /// <summary>
    /// Serviço de spawn para instanciar o Player real no baseline.
    /// </summary>
    public sealed class PlayerSpawnService : ActorSpawnServiceBase
    {
        private readonly IGameplayStateGate _gameplayStateService;

        public PlayerSpawnService(
            IUniqueIdFactory uniqueIdFactory,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context,
            GameObject prefab,
            IGameplayStateGate gameplayStateService)
            : base(uniqueIdFactory, actorRegistry, context, prefab)
        {
            _gameplayStateService = gameplayStateService;
        }

        public override string Name => nameof(PlayerSpawnService);

        public override ActorKind SpawnedActorKind => ActorKind.Player;

        public override bool IsRequiredForWorldReset => true;

        protected override IActor ResolveActor(GameObject instance) =>
            PlayerSpawnActorResolver.ResolvePlayerActor(instance, EnsureActorIdForPlayer);

        protected override bool EnsureActorId(IActor actor, GameObject instance)
        {
            return actor switch
            {
                null => false,
                PlayerActor player => EnsureActorIdForPlayer(player, instance),
                _ => !string.IsNullOrWhiteSpace(actor.ActorId)
            };
        }

        private bool EnsureActorIdForPlayer(PlayerActor player, GameObject instance)
        {
            return player != null &&
                   EnsureGeneratedActorId(player.ActorId, instance, "Player", player.Initialize);
        }

        protected override void OnPostInstantiate(GameObject instance)
        {
            EnsureMovementStack(instance);
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
    }
}
