using _ImmersiveGames.Scripts.PlanetSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.UI.Compass
{
    /// <summary>
    /// Tester simples para validar estilos de cor por tipo de recurso na bússola.
    /// </summary>
    public class PlanetResourceCompassStyleTester : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Database de estilos de ícone por recurso.")]
        public PlanetResourceCompassStyleDatabase styleDatabase;

        [Header("Test Input")]
        public PlanetResources testResourceType = PlanetResources.Metal;
        public Color defaultColor = Color.white;

        [Header("Key Binding")]
        public KeyCode queryKey = KeyCode.C;

        private void Update()
        {
            if (styleDatabase == null)
            {
                return;
            }

            if (Input.GetKeyDown(queryKey))
            {
                Color finalColor = styleDatabase.GetColorForResource(testResourceType, defaultColor);
                Debug.Log($"[PlanetResourceCompassStyleTester] Resource: {testResourceType}, Color: {finalColor}");
            }
        }
    }
}
