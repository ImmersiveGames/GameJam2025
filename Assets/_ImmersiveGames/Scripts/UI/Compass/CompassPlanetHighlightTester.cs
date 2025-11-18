using _ImmersiveGames.Scripts.PlanetSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.UI.Compass
{
    /// <summary>
    /// Script simples para validar o destaque de planetas marcados na bússola via teclado.
    /// </summary>
    public class CompassPlanetHighlightTester : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Controlador de destaque da bússola.")]
        public CompassPlanetHighlightController highlightController;

        [Tooltip("Planetas de teste a serem marcados via teclado.")]
        public PlanetsMaster[] testPlanets;

        [Header("Key Bindings")]
        [Tooltip("Tecla para marcar o primeiro planeta da lista.")]
        public KeyCode markFirstKey = KeyCode.Alpha1;

        [Tooltip("Tecla para marcar o segundo planeta da lista.")]
        public KeyCode markSecondKey = KeyCode.Alpha2;

        [Tooltip("Tecla para limpar o destaque (nenhum planeta marcado).")]
        public KeyCode clearMarkKey = KeyCode.Alpha0;

        private void Update()
        {
            if (highlightController == null)
            {
                return;
            }

            if (Input.GetKeyDown(markFirstKey))
            {
                PlanetsMaster planet = testPlanets != null && testPlanets.Length > 0 ? testPlanets[0] : null;
                highlightController.SetMarkedPlanet(planet);
                LogMarkState("Alpha1", planet);
            }

            if (Input.GetKeyDown(markSecondKey))
            {
                PlanetsMaster planet = testPlanets != null && testPlanets.Length > 1 ? testPlanets[1] : null;
                highlightController.SetMarkedPlanet(planet);
                LogMarkState("Alpha2", planet);
            }

            if (Input.GetKeyDown(clearMarkKey))
            {
                highlightController.SetMarkedPlanet(null);
                LogMarkState("Alpha0", null);
            }
        }

        private void LogMarkState(string key, PlanetsMaster planet)
        {
            string planetName = planet != null ? planet.ActorName : "(nenhum)";
            Debug.Log($"[CompassPlanetHighlightTester] Tecla {key} -> planeta marcado: {planetName}");
        }
    }
}
