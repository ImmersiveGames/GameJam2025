using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.Spawn
{
    /// <summary>
    /// Marco canônico de que o spawn terminou com sucesso.
    /// Neste ponto, a identidade já foi atribuída e o actor já foi registrado no runtime.
    /// </summary>
    public readonly struct ActorSpawnCompletedEvent : IEvent
    {
        public ActorSpawnCompletedEvent(
            IActor actor,
            ActorKind actorKind,
            string actorId,
            string spawnServiceName,
            string sceneName,
            bool requiredForWorldReset)
        {
            Actor = actor;
            ActorKind = actorKind;
            ActorId = string.IsNullOrWhiteSpace(actorId) ? string.Empty : actorId.Trim();
            SpawnServiceName = string.IsNullOrWhiteSpace(spawnServiceName) ? string.Empty : spawnServiceName.Trim();
            SceneName = string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName.Trim();
            RequiredForWorldReset = requiredForWorldReset;
        }

        public IActor Actor { get; }

        public ActorKind ActorKind { get; }

        public string ActorId { get; }

        public string SpawnServiceName { get; }

        public string SceneName { get; }

        public bool RequiredForWorldReset { get; }

        public bool HasActor => Actor != null;
    }
}

