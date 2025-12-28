using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Actors
{
    /// <summary>
    /// Implementação mínima de um ator para validar o pipeline de spawn/despawn.
    /// </summary>
    public sealed class DummyEnemy : MonoBehaviour, IActor, IActorKindProvider
    {
        [SerializeField]
        private string actorId = string.Empty;

        public string ActorId => actorId;

        public string DisplayName => gameObject != null ? gameObject.name : nameof(DummyEnemy);

        public Transform Transform => transform;

        public bool IsActive => isActiveAndEnabled;

        public void Initialize(string id)
        {
            actorId = id ?? string.Empty;
        }
        public ActorKind Kind => ActorKind.Enemy;
    }
}
