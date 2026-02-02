using UnityEngine;
namespace _ImmersiveGames.NewScripts.Gameplay.Content.Actors.Player.Movement
{
    /// <summary>
    /// Leitor mínimo de input para o stack do Player no NewScripts.
    /// Usa Input.GetAxis/GetAxisRaw (Horizontal/Vertical) para manter o fluxo leve
    /// e previsível durante resets.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NewPlayerInputReader : MonoBehaviour
    {
        [Header("Axes")]
        [SerializeField]
        private string horizontalAxis = "Horizontal";

        [SerializeField]
        private string verticalAxis = "Vertical";

        [SerializeField]
        private bool useRawAxes = true;

        [SerializeField]
        [Range(0f, 1f)]
        private float deadzone = 0.1f;

        [SerializeField]
        private bool clampMagnitude = true;

        private Vector2 _currentInput;
        private bool _inputEnabled = true;

        /// <summary>Input atual após deadzone e clamp.</summary>
        public Vector2 MoveInput => _currentInput;

        /// <summary>Deadzone aplicada na magnitude (ao quadrado).</summary>
        public float Deadzone => deadzone;

        /// <summary>Habilita/desabilita a leitura real; ao desativar, zera o cache.</summary>
        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;

            if (!enabled)
            {
                _currentInput = Vector2.zero;
            }
        }

        private void Update()
        {
            if (!_inputEnabled)
            {
                _currentInput = Vector2.zero;
                return;
            }

            float x = useRawAxes ? Input.GetAxisRaw(horizontalAxis) : Input.GetAxis(horizontalAxis);
            float y = useRawAxes ? Input.GetAxisRaw(verticalAxis) : Input.GetAxis(verticalAxis);

            var value = new Vector2(x, y);

            if (value.sqrMagnitude < deadzone * deadzone)
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

        /// <summary>Limpa o input atual sem alterar o estado de habilitado.</summary>
        public void ClearInput() => _currentInput = Vector2.zero;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>QA: injeta input sintético para testes determinísticos.</summary>
        public void QA_SetMoveInput(Vector2 input) => _currentInput = input;

        /// <summary>QA: limpa qualquer input em cache.</summary>
        public void QA_ClearInputs() => _currentInput = Vector2.zero;
#endif
    }
}
