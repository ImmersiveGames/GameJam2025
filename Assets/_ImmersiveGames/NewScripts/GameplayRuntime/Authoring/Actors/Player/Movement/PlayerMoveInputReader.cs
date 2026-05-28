using UnityEngine;
using UnityEngine.InputSystem;
using _ImmersiveGames.NewScripts.Actors.Runtime;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Player.Movement
{
    /// <summary>
    /// Leitor canonico de movimento baseado em PlayerInput bound no ActivitySetup.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerMoveInputReader : MonoBehaviour, IActorIntentSource
    {
        private const string PlayerActionMapName = "Player";
        private const string MoveActionName = "Move";

        [Header("Filtering")]
        [SerializeField] [Range(0f, 1f)] private float deadzone = 0.1f;
        [SerializeField] private bool clampMagnitude = true;

        private Vector2 _currentInput;
        private PlayerInput _boundPlayerInput;
        private InputAction _moveAction;
        private bool _inputEnabled = true;
        private bool _bound;

        public Vector2 MoveInput => _currentInput;
        public bool IsBound => _bound;
        public PlayerInput BoundPlayerInput => _boundPlayerInput;
        public Transform Transform => transform;

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled)
            {
                _currentInput = Vector2.zero;
            }
        }

        public void Bind(PlayerInput playerInput)
        {
            if (playerInput == null)
            {
                throw new System.InvalidOperationException("PlayerMoveInputReader.Bind requer PlayerInput valido.");
            }

            if (playerInput.actions == null)
            {
                throw new System.InvalidOperationException("PlayerMoveInputReader.Bind requer PlayerInput.actions configurado.");
            }

            InputActionMap playerMap = playerInput.actions.FindActionMap(PlayerActionMapName, throwIfNotFound: false);
            if (playerMap == null)
            {
                throw new System.InvalidOperationException("PlayerMoveInputReader.Bind falhou: ActionMap 'Player' ausente.");
            }

            InputAction move = playerMap.FindAction(MoveActionName, throwIfNotFound: false);
            if (move == null)
            {
                throw new System.InvalidOperationException("PlayerMoveInputReader.Bind falhou: action 'Move' ausente no map 'Player'.");
            }

            _boundPlayerInput = playerInput;
            _moveAction = move;
            _bound = true;
            _currentInput = Vector2.zero;
        }

        public void ClearInput()
        {
            _currentInput = Vector2.zero;
        }

        private void Update()
        {
            if (!_inputEnabled || !_bound || _moveAction == null)
            {
                _currentInput = Vector2.zero;
                return;
            }

            Vector2 value = _moveAction.ReadValue<Vector2>();
            float sqrDeadzone = deadzone * deadzone;
            if (value.sqrMagnitude < sqrDeadzone)
            {
                _currentInput = Vector2.zero;
                return;
            }

            if (clampMagnitude && value.sqrMagnitude > 1f)
            {
                value = value.normalized;
            }

            _currentInput = value;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void QA_SetMoveInput(Vector2 input) => _currentInput = input;
        public void QA_ClearInputs() => _currentInput = Vector2.zero;
#endif
    }
}
