using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Actors
{
    /// <summary>
    /// Implementação mínima de um ator para validar o pipeline de spawn/despawn.
    /// </summary>
    public sealed class DummyActor : MonoBehaviour, IActor
    {
        [SerializeField]
        private string _actorId = string.Empty;

        public string ActorId => _actorId;

        public string DisplayName => gameObject != null ? gameObject.name : nameof(DummyActor);

        public Transform Transform => transform;

        public bool IsActive => isActiveAndEnabled;

        public void Initialize(string actorId)
        {
            _actorId = actorId ?? string.Empty;
        }
    }
}
