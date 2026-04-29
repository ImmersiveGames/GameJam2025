using _ImmersiveGames.NewScripts.Foundation.Core.Identifiers;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.GameplayRuntime.ActorRegistry;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Player.Movement;
using _ImmersiveGames.NewScripts.GameplayRuntime.StateGate.Core;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.Spawn
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
            ActorSpecRecord actorSpec,
            GameObject prefab,
            IGameplayStateGate gameplayStateService)
            : base(uniqueIdFactory, actorRegistry, context, actorSpec, prefab)
        {
            _gameplayStateService = gameplayStateService;
        }

        public override string Name => nameof(PlayerSpawnService);

        public override ActorKind SpawnedActorKind => ActorKind.Player;

        public override bool IsRequiredForWorldReset => true;

        protected override IActor ResolveActor(GameObject instance) =>
            PlayerSpawnActorResolver.ResolvePlayerActor(instance);

        protected override string ResolveSemanticParticipantId(IActor actor, in ActorSpawnRequest request)
        {
            _ = actor;

            if (request.HasSemanticParticipantId)
            {
                DebugUtility.Log(typeof(PlayerSpawnService),
                    $"[OBS][Gameplay][SpawnBridge] Player spawn consumed semanticParticipantId from ActorSpawnRequest semanticParticipantId='{request.SemanticParticipantId}' actorSpecId='{request.ActorSpecId}' actorSetRef='{request.ActorSetRef}' recipe='{request.OperationalRecipeKind}' source='{request.Source}'.");
                return request.SemanticParticipantId;
            }

            HardFailFastH1.Trigger(typeof(PlayerSpawnService),
                $"[FATAL][H1][Gameplay][SpawnBridge] Player spawn canônico sem semanticParticipantId no ActorSpawnRequest actorSpecId='{request.ActorSpecId}' actorSetRef='{request.ActorSetRef}' recipe='{request.OperationalRecipeKind}' source='{request.Source}'.");
            return string.Empty;
        }

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
            // F2-D remove a redescoberta de participação; neste ponto só registramos
            // que o spawn do player consumiu o semanticParticipantId do request canônico.
        }
    }
}

