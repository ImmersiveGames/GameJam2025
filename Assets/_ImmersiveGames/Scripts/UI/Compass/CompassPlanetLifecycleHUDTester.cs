using System.Linq;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.World.Compass;
using UnityEngine;

namespace _ImmersiveGames.Scripts.UI.Compass
{
    /// <summary>
    /// Utilitário opcional para depurar a quantidade de ícones da bússola em tempo real.
    /// Funciona em conjunto com <see cref="CompassDamageLifecycleTester"/> para verificar
    /// se os alvos removidos por eventos de morte voltam após revive/reset.
    /// </summary>
    public class CompassPlanetLifecycleHUDTester : MonoBehaviour
    {
        [Header("HUD References")]
        public CompassHUD compassHUD;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                LogIconStatus();
            }
        }

        private void LogIconStatus()
        {
            var trackableCount = CompassRuntimeService.Trackables?.Count ?? 0;
            var iconCount = compassHUD?.EnumerateIcons().Count() ?? 0;

            DebugUtility.LogVerbose<CompassPlanetLifecycleHUDTester>(
                $"HUD Debug (H): Trackables={trackableCount}, Icons={iconCount}");
        }
    }
}
