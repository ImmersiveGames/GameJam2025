using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Player
{
    /// <summary>
    /// Implementação simples de IActor para o baseline de NewScripts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerActor : MonoBehaviour, IActor, IActorKindProvider
    {
        [SerializeField]
        private string actorId = string.Empty;

        [SerializeField]
        [Tooltip("Opcional: nome amigável exibido em logs do pipeline de baseline.")]
        private string displayName = string.Empty;

        public string ActorId => actorId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName)
            ? (gameObject != null ? gameObject.name : nameof(PlayerActor))
            : displayName;

        public Transform Transform => transform;

        public bool IsActive => isActiveAndEnabled;

        public ActorKind Kind => ActorKind.Player;

        public void Initialize(string newActorId)
        {
            if (!string.IsNullOrWhiteSpace(newActorId))
            {
                actorId = newActorId;
            }
        }
    }
}

