using _ImmersiveGames.NewScripts.Core.Identifiers;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Dummy;
using _ImmersiveGames.NewScripts.Modules.SceneReset.Spawn;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Spawn
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

        protected override bool EnsureActorId(IActor actor, GameObject instance)
        {
            return actor is DummyActor dummy &&
                   EnsureGeneratedActorId(dummy.ActorId, instance, "DummyActor", dummy.Initialize);
        }
    }
}
