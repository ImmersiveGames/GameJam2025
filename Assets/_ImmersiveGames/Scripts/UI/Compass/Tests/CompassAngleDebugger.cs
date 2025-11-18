using _ImmersiveGames.Scripts.World.Compass;
using UnityEngine;

// Script auxiliar para inspecionar ângulos e posições de ícones da bússola.
// Pode ser colocado no mesmo GameObject do CompassHUD.
namespace _ImmersiveGames.Scripts.UI.Compass.Tests
{
    [RequireComponent(typeof(CompassHUD))]
    public class CompassAngleDebugger : MonoBehaviour
    {
        [Header("Tecla para log detalhado de ângulos e posições")]
        [SerializeField] private KeyCode logDetailsKey = KeyCode.G;

        private CompassHUD _compassHUD;

        private void Awake()
        {
            _compassHUD = GetComponent<CompassHUD>();
            if (_compassHUD == null)
            {
                Debug.LogError("[CompassAngleDebugger] Nenhum CompassHUD encontrado.");
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(logDetailsKey))
            {
                LogDetails();
            }
        }

        private void LogDetails()
        {
            if (_compassHUD == null)
            {
                Debug.LogWarning("[CompassAngleDebugger] CompassHUD nulo.");
                return;
            }

            var player = CompassRuntimeService.PlayerTransform;
            var trackables = CompassRuntimeService.Trackables;

            if (player == null)
            {
                Debug.LogWarning("[CompassAngleDebugger] PlayerTransform é NULL.");
                return;
            }

            if (trackables == null || trackables.Count == 0)
            {
                Debug.LogWarning("[CompassAngleDebugger] Nenhum trackable registrado.");
                return;
            }

            Debug.Log("[CompassAngleDebugger] --- Detalhes de alvos na bússola ---");

            for (int i = 0; i < trackables.Count; i++)
            {
                var t = trackables[i];
                if (t == null || t.Transform == null)
                {
                    Debug.LogWarning($"  [{i}] Trackable nulo ou sem Transform.");
                    continue;
                }

                // Cálculo de vetor e ângulo (deve ser idêntico ao interno da CompassHUD)
                Vector3 toTarget = t.Transform.position - player.position;
                toTarget.y = 0f;

                Vector3 forward = player.forward;
                forward.y = 0f;

                if (toTarget.sqrMagnitude < 0.0001f || forward.sqrMagnitude < 0.0001f)
                {
                    Debug.LogWarning($"  [{i}] Vetores inválidos para cálculo de ângulo.");
                    continue;
                }

                float angle = Vector3.SignedAngle(forward.normalized, toTarget.normalized, Vector3.up);

                // Aqui assumo que a CompassHUD expõe um método ou propriedade para converter ângulo em X.
                // Ex: public float CalculateXFromAngle(float angle)
                float xPos = _compassHUD.CalculateXFromAngle(angle);

                string side = angle > 0 ? "Direita" : (angle < 0 ? "Esquerda" : "Centro");

                Debug.Log(
                    $"  [{i}] Name={t.Transform.name}, Type={t.TargetType}\n" +
                    $"      WorldPos={t.Transform.position}, Angle={angle:F2} graus ({side}), UI_X={xPos:F2}"
                );
            }
        }
    }
}
