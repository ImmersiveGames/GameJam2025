using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Eater.Movement
{
    /// <summary>
    /// Controlador simples de movimentação aleatória para o Eater no pipeline NewScripts.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EaterActor))]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class EaterRandomMovementController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField]
        [Tooltip("Velocidade de deslocamento (unidades por segundo).")]
        private float moveSpeed = 3f;

        [SerializeField]
        [Tooltip("Intervalo (segundos) para sortear nova direção.")]
        private float directionChangeInterval = 2f;

        [SerializeField]
        [Tooltip("Quando true, aplica movimento no espaço local do Eater.")]
        private bool useLocalSpace;

        private Vector3 _currentDirection = Vector3.forward;
        private float _timeToNextDirection;
        private string _sceneName;

        private void Awake()
        {
            _sceneName = gameObject.scene.name;
            _timeToNextDirection = 0f;
        }

        private void OnEnable()
        {
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            _timeToNextDirection -= deltaTime;

            if (_timeToNextDirection <= 0f)
            {
                _currentDirection = PickNewDirection();
                _timeToNextDirection = Mathf.Max(0.1f, directionChangeInterval);

                DebugUtility.LogVerbose<EaterRandomMovementController>(
                    $"[EaterMovement] New direction: {_currentDirection} (scene='{_sceneName}').");
            }

            Vector3 displacement = _currentDirection * (moveSpeed * deltaTime);
            transform.Translate(displacement, useLocalSpace ? Space.Self : Space.World);
        }

        private Vector3 PickNewDirection()
        {
            for (int attempt = 0; attempt < 6; attempt++)
            {
                var dir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
                if (dir.sqrMagnitude > 0.01f)
                {
                    return dir.normalized;
                }
            }

            return Vector3.forward;
        }
    }
}
