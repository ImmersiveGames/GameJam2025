using _ImmersiveGames.NewScripts.Core.Identifiers;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actions.States;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Eater;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Eater.Movement;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Spawn;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Spawning
{
    /// <summary>
    /// Serviço de spawn para instanciar o Eater no baseline do NewScripts.
    /// Agora herda de ActorSpawnServiceBase para lógica comum.
    /// </summary>
    public sealed class EaterSpawnService : ActorSpawnServiceBase
    {
        private readonly IStateDependentService _stateService;

        public EaterSpawnService(
            IUniqueIdFactory uniqueIdFactory,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context,
            EaterActor prefab,
            IStateDependentService stateService)
            : base(uniqueIdFactory, actorRegistry, context, prefab ? prefab.gameObject : null)
        {
            _stateService = stateService;
        }

        public override string Name => nameof(EaterSpawnService);

        protected override IActor ResolveActor(GameObject instance)
        {
            return instance ? instance.GetComponent<EaterActor>() as IActor : null;
        }

        protected override bool EnsureActorId(IActor actor, GameObject instance)
        {
            if (actor == null)
            {
                return false;
            }

            var eater = actor as EaterActor;
            if (!eater)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(eater.ActorId))
            {
                return true;
            }

            if (uniqueIdFactory == null)
            {
                DebugUtility.LogError(typeof(EaterSpawnService),
                    "IUniqueIdFactory ausente; não é possível gerar ActorId para Eater.");
                return false;
            }

            string actorId = uniqueIdFactory.GenerateId(eater.gameObject);

            if (string.IsNullOrWhiteSpace(actorId))
            {
                DebugUtility.LogError(typeof(EaterSpawnService),
                    "IUniqueIdFactory retornou ActorId vazio; abortando spawn do Eater.");
                return false;
            }

            eater.Initialize(actorId);
            return true;
        }

        protected override void OnPostInstantiate(GameObject instance)
        {
            // garantir injeção de state service em controllers específicos
            InjectStateService(instance);
        }

        protected override void InjectStateService(GameObject instance)
        {
            if (_stateService == null || !instance)
            {
                return;
            }

            if (instance.TryGetComponent(out EaterRandomMovementController controller))
            {
                controller.InjectStateService(_stateService);
            }
        }
    }
}



