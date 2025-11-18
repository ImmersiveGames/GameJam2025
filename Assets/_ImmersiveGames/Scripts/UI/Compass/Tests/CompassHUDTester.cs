using _ImmersiveGames.Scripts.World.Compass;
using UnityEngine;

// Script temporário para inspecionar o CompassHUD em runtime.
// Deve ser colocado no MESMO GameObject que possui o CompassHUD.
namespace _ImmersiveGames.Scripts.UI.Compass
{
    [RequireComponent(typeof(CompassHUD))]
    public class CompassHUDTester : MonoBehaviour
    {
        [Header("Teclas de debug")]
        [SerializeField] private KeyCode logSummaryKey = KeyCode.H;

        private CompassHUD _compassHUD;

        private void Awake()
        {
            _compassHUD = GetComponent<CompassHUD>();
            if (_compassHUD == null)
            {
                Debug.LogError("[CompassHUDTester] Nenhum CompassHUD encontrado no mesmo GameObject.");
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(logSummaryKey))
            {
                LogSummary();
            }
        }

        private void LogSummary()
        {
            if (_compassHUD == null)
            {
                Debug.LogWarning("[CompassHUDTester] CompassHUD nulo.");
                return;
            }

            var player = CompassRuntimeService.PlayerTransform;
            var trackables = CompassRuntimeService.Trackables;

            Debug.Log($"[CompassHUDTester] Player: {(player != null ? player.name : "NULL")}");
            Debug.Log($"[CompassHUDTester] Trackables count: {(trackables != null ? trackables.Count : -1)}");

            // Se você tiver na CompassHUD um método ou propriedade para pegar a contagem de ícones, use aqui.
            // Exemplo sugerido: public int IconCount => _iconsByTarget.Count;
            int iconCount = _compassHUD.IconCount; // Certifique-se de expor isso em CompassHUD.
            Debug.Log($"[CompassHUDTester] IconCount interno do CompassHUD: {iconCount}");
        }
    }
}