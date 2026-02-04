using _ImmersiveGames.NewScripts.Core.Identifiers;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Runtime.Actors;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Runtime.World.Spawn
{
    /// <summary>
    /// Serviço de spawn que cria um único DummyActor para validar o pipeline.
    /// Agora delega a maioria da lógica à ActorSpawnServiceBase.
    /// </summary>
    public sealed class DummyActorSpawnService : ActorSpawnServiceBase
    {
        public DummyActorSpawnService(
            IUniqueIdFactory uniqueIdFactory,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context,
            GameObject prefab)
            : base(uniqueIdFactory, actorRegistry, context, prefab)
        {
        }

        public override string Name => nameof(DummyActorSpawnService);

        protected override IActor ResolveActor(GameObject instance)
        {
            return instance.GetComponent<DummyActor>();
        }

        protected override bool EnsureActorId(IActor actor, GameObject instance)
        {
            if (actor == null)
            {
                return false;
            }

            var dummy = actor as DummyActor;
            if (dummy == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(dummy.ActorId))
            {
                return true;
            }

            if (uniqueIdFactory == null)
            {
                DebugUtility.LogError(typeof(DummyActorSpawnService),
                    "IUniqueIdFactory ausente; não é possível gerar ActorId para DummyActor.");
                return false;
            }

            string actorId = uniqueIdFactory.GenerateId(instance);
            if (string.IsNullOrWhiteSpace(actorId))
            {
                DebugUtility.LogError(typeof(DummyActorSpawnService),
                    "IUniqueIdFactory retornou ActorId vazio; abortando spawn do DummyActor.");
                return false;
            }

            dummy.Initialize(actorId);
            return true;
        }
    }
}

