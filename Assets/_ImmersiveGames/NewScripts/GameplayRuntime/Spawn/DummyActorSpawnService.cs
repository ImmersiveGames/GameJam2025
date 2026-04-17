using _ImmersiveGames.NewScripts.Foundation.Core.Identifiers;
using _ImmersiveGames.NewScripts.GameplayRuntime.ActorRegistry;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Dummy;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.Spawn
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

        public override ActorKind SpawnedActorKind => ActorKind.Dummy;

        public override bool IsRequiredForWorldReset => false;

        protected override IActor ResolveActor(GameObject instance)
        {
            return instance.GetComponent<DummyActor>();
        }
    }
}

