using _ImmersiveGames.NewScripts.Core.Identifiers;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Runtime.Actors.Player.Movement;
using _ImmersiveGames.NewScripts.Runtime.Actors;
using _ImmersiveGames.NewScripts.Runtime.State;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Runtime.World.Spawn
{
    /// <summary>
    /// Serviço de spawn para instanciar o Player real no baseline, substituindo o DummyActor.
    /// Agora herda de ActorSpawnServiceBase.
    /// </summary>
    public sealed class PlayerSpawnService : ActorSpawnServiceBase
    {
        private readonly IStateDependentService _stateService;

        public PlayerSpawnService(
            IUniqueIdFactory uniqueIdFactory,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context,
            GameObject prefab,
            IStateDependentService stateService)
            : base(uniqueIdFactory, actorRegistry, context, prefab)
        {
            _stateService = stateService;
        }

        public override string Name => nameof(PlayerSpawnService);

        protected override IActor ResolveActor(GameObject instance) =>
            PlayerSpawnResolver.Resolve(instance, EnsureActorIdForPlayer, EnsureActorIdForAdapter);

        protected override bool EnsureActorId(IActor actor, GameObject instance)
        {
            return actor switch
            {
                null => false,
                PlayerActor player => EnsureActorIdForPlayer(player, instance),
                PlayerActorAdapter adapter => EnsureActorIdForAdapter(adapter, instance),
                _ => !string.IsNullOrWhiteSpace(actor.ActorId)
            };

        }

        private bool EnsureActorIdForPlayer(PlayerActor player, GameObject instance)
        {
            if (player == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(player.ActorId))
            {
                return true;
            }

            if (uniqueIdFactory == null)
            {
                DebugUtility.LogError(typeof(PlayerSpawnService),
                    "IUniqueIdFactory ausente; não é possível gerar ActorId para Player.");
                return false;
            }

            string actorId = uniqueIdFactory.GenerateId(instance);

            if (string.IsNullOrWhiteSpace(actorId))
            {
                DebugUtility.LogError(typeof(PlayerSpawnService),
                    "IUniqueIdFactory retornou ActorId vazio; abortando spawn do Player.");
                return false;
            }

            player.Initialize(actorId);
            return true;
        }

        private bool EnsureActorIdForAdapter(PlayerActorAdapter adapter, GameObject instance)
        {
            if (adapter == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(adapter.ActorId))
            {
                return true;
            }

            if (uniqueIdFactory == null)
            {
                DebugUtility.LogError(typeof(PlayerSpawnService),
                    "IUniqueIdFactory ausente; não é possível gerar ActorId para PlayerActorAdapter.");
                return false;
            }

            string actorId = uniqueIdFactory.GenerateId(instance);

            if (string.IsNullOrWhiteSpace(actorId))
            {
                DebugUtility.LogError(typeof(PlayerSpawnService),
                    "IUniqueIdFactory retornou ActorId vazio; abortando spawn do PlayerActorAdapter.");
                return false;
            }

            adapter.Initialize(actorId);
            return true;
        }

        protected override void OnPostInstantiate(GameObject instance)
        {
            EnsureMovementStack(instance);
            InjectStateService(instance);
        }

        private static void EnsureMovementStack(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            var input = instance.GetComponent<PlayerInputReader>() ?? instance.AddComponent<PlayerInputReader>();
            var controller = instance.GetComponent<PlayerMovementController>() ?? instance.AddComponent<PlayerMovementController>();

            if (controller != null && input != null)
            {
                controller.SetInputReader(input);
            }
        }

        protected override void InjectStateService(GameObject instance)
        {
            if (_stateService == null || instance == null)
            {
                return;
            }

            if (instance.TryGetComponent(out PlayerMovementController controller))
            {
                controller.InjectStateService(_stateService);
            }
        }
    }
}



