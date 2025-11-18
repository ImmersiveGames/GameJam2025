using _ImmersiveGames.Scripts.World.Compass;
using UnityEngine;

// Script temporário para inspecionar o estado do CompassRuntimeService em runtime.
namespace _ImmersiveGames.Scripts.UI.Compass
{
    public class CompassRuntimeServiceTester : MonoBehaviour
    {
        [Header("Aperte as teclas indicadas para inspecionar o runtime service")]
        [SerializeField] private KeyCode logPlayerKey = KeyCode.P;
        [SerializeField] private KeyCode logTargetsKey = KeyCode.Y;

        private void Update()
        {
            if (Input.GetKeyDown(logPlayerKey))
            {
                LogPlayerInfo();
            }

            if (Input.GetKeyDown(logTargetsKey))
            {
                LogTargetsInfo();
            }
        }

        private void LogPlayerInfo()
        {
            var player = CompassRuntimeService.PlayerTransform;
            if (player == null)
            {
                Debug.LogWarning("[CompassRuntimeServiceTester] PlayerTransform é NULL no CompassRuntimeService.");
            }
            else
            {
                Debug.Log($"[CompassRuntimeServiceTester] Player atual: {player.name} - posição: {player.position}");
            }
        }

        private void LogTargetsInfo()
        {
            var trackables = CompassRuntimeService.Trackables;
            if (trackables == null)
            {
                Debug.LogWarning("[CompassRuntimeServiceTester] Trackables é NULL.");
                return;
            }

            Debug.Log($"[CompassRuntimeServiceTester] Trackables count: {trackables.Count}");

            for (int i = 0; i < trackables.Count; i++)
            {
                var t = trackables[i];
                if (t == null)
                {
                    Debug.LogWarning($"  [{i}] Trackable NULO.");
                    continue;
                }

                Debug.Log(
                    $"  [{i}] Name={t.Transform.name}, Type={t.TargetType}, IsActive={t.IsActive}, Pos={t.Transform.position}"
                );
            }
        }
    }
}