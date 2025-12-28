using UnityEngine;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;

namespace _ImmersiveGames.NewScripts.QA.GameplayReset
{
    /// <summary>
    /// Dummy Actor de QA para validar GameplayReset por ActorKind.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayResetKindQaDummyActor : MonoBehaviour, IActor, IActorKindProvider
    {
        [SerializeField]
        private string actorId = string.Empty;

        [SerializeField]
        [Tooltip("Opcional: nome amigável exibido nos logs de QA.")]
        private string displayName = "QA Dummy";

        public string ActorId => actorId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName)
            ? (gameObject != null ? gameObject.name : nameof(GameplayResetKindQaDummyActor))
            : displayName;

        public Transform Transform => transform;

        public bool IsActive => isActiveAndEnabled;

        public ActorKind Kind => ActorKind.Dummy;

        public void Initialize(string id)
        {
            actorId = id ?? string.Empty;
        }
    }
}
