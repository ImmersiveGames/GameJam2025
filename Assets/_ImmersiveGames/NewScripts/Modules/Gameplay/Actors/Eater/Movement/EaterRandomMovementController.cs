using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actions;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actions.States;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Eater.Movement
{
    /// <summary>
    /// Controlador simples de movimentação aleatória para o Eater no pipeline NewScripts.
    /// Respeita o IStateDependentService para bloquear/liberar a ação de movimento.
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

        private IStateDependentService _stateService;
        private Vector3 _currentDirection = Vector3.forward;
        private float _timeToNextDirection;
        private bool _stateBlockedLogged;
        private string _sceneName;

        private void Awake()
        {
            _sceneName = gameObject.scene.name;
            ResolveStateServiceOrDisable();
            _timeToNextDirection = 0f;
        }

        private void OnEnable()
        {
            ResolveStateServiceOrDisable();
        }

        private void Update()
        {
            if (_stateService == null)
            {
                return;
            }

            if (!_stateService.CanExecuteGameplayAction(GameplayAction.Move))
            {
                LogStateBlockedOnce();
                return;
            }

            _stateBlockedLogged = false;

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

        private void ResolveStateServiceOrDisable()
        {
            if (_stateService != null)
            {
                return;
            }

            DependencyManager.Provider.TryGetGlobal(out _stateService);

            if (_stateService != null)
            {
                return;
            }

            DebugUtility.LogWarning(typeof(EaterRandomMovementController),
                $"[EaterMovement] IStateDependentService não encontrado; movimento desativado. scene='{_sceneName}'.");
            enabled = false;
        }

        public void InjectStateService(IStateDependentService stateService)
        {
            if (stateService == null)
            {
                return;
            }

            _stateService = stateService;
            _stateBlockedLogged = false;

            if (!enabled)
            {
                enabled = true;
            }
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

        private void LogStateBlockedOnce()
        {
            if (_stateBlockedLogged)
            {
                return;
            }

            DebugUtility.LogVerbose<EaterRandomMovementController>(
                "[EaterMovement] Movement blocked by IStateDependentService.");
            _stateBlockedLogged = true;
        }
    }
}



