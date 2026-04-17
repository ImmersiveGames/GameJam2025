using ImmersiveGames.GameJam2025.Core.Identifiers;
using ImmersiveGames.GameJam2025.Game.Gameplay.Actors.Core;
using ImmersiveGames.GameJam2025.Game.Gameplay.Actors.Dummy;
using UnityEngine;
namespace ImmersiveGames.GameJam2025.Game.Gameplay.Spawn
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

