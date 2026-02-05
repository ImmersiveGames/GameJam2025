using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Dummy
{
    /// <summary>
    /// Implementação mínima de um ator para validar o pipeline de spawn/despawn.
    /// </summary>
    public sealed class DummyActor : MonoBehaviour, IActor
    {
        [SerializeField]
        private string actorId = string.Empty;

        public string ActorId => actorId;

        public string DisplayName => gameObject != null ? gameObject.name : nameof(DummyActor);

        public Transform Transform => transform;

        public bool IsActive => isActiveAndEnabled;

        public void Initialize(string id)
        {
            actorId = id ?? string.Empty;
        }
    }
}
