using _ImmersiveGames.NewScripts.Foundation.Core.Identifiers;
using _ImmersiveGames.NewScripts.GameplayRuntime.ActorRegistry;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Eater;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Eater.Movement;
using _ImmersiveGames.NewScripts.GameplayRuntime.StateGate.Core;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.Spawn
{
    /// <summary>
    /// Serviço de spawn para instanciar o Eater no baseline do NewScripts.
    /// Agora herda de ActorSpawnServiceBase para lógica comum.
    /// </summary>
    public sealed class EaterSpawnService : ActorSpawnServiceBase
    {
        private readonly IGameplayStateGate _gameplayStateService;

        public EaterSpawnService(
            IUniqueIdFactory uniqueIdFactory,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context,
            EaterActor prefab,
            IGameplayStateGate gameplayStateService)
            : base(uniqueIdFactory, actorRegistry, context, prefab ? prefab.gameObject : null)
        {
            _gameplayStateService = gameplayStateService;
        }

        public override string Name => nameof(EaterSpawnService);

        public override ActorKind SpawnedActorKind => ActorKind.Eater;

        public override bool IsRequiredForWorldReset => true;

        protected override IActor ResolveActor(GameObject instance)
        {
            return instance ? instance.GetComponent<EaterActor>() as IActor : null;
        }

        protected override void OnPostInstantiate(GameObject instance)
        {
            GameplayStateControllerInjector.TryInject<EaterRandomMovementController>(
                instance,
                _gameplayStateService,
                static (controller, stateService) => controller.InjectStateService(stateService));
        }
    }
}

