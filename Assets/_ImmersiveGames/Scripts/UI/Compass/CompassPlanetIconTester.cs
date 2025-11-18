using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.World.Compass;
using UnityEngine;

namespace _ImmersiveGames.Scripts.UI.Compass
{
    /// <summary>
    /// Componente auxiliar para testar ícones dinâmicos de planetas na bússola sem depender da HUD completa.
    /// Permite atribuir recursos e revelar/ocultar descoberta via teclas.
    /// </summary>
    public class CompassPlanetIconTester : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Planeta que possui PlanetsMaster e será usado como alvo de teste.")]
        public PlanetsMaster planetMaster;

        [Tooltip("Config visual de planeta (dynamicMode = PlanetResourceIcon).")]
        public CompassTargetVisualConfig planetVisualConfig;

        [Tooltip("Instância de CompassIcon em cena para ser inicializada durante o teste.")]
        public CompassIcon compassIconInstance;

        [Header("Test Data")]
        [Tooltip("Recurso de planeta que será atribuído quando a tecla de atribuição for pressionada.")]
        public PlanetResourcesSo resourceToAssign;

        [Header("Key Bindings")]
        [Tooltip("Tecla para atribuir o recurso ao planeta (aciona ResourceAssigned).")]
        public KeyCode assignResourceKey = KeyCode.Alpha1;

        [Tooltip("Tecla para revelar o recurso do planeta (aciona ResourceDiscoveryChanged true).")]
        public KeyCode revealResourceKey = KeyCode.Alpha2;

        [Tooltip("Tecla para esconder o recurso novamente (aciona ResourceDiscoveryChanged false).")]
        public KeyCode hideResourceKey = KeyCode.Alpha3;

        private ICompassTrackable _fakeTarget;

        private void Start()
        {
            if (planetMaster == null)
            {
                Debug.LogWarning("[CompassPlanetIconTester] Atribua um PlanetsMaster para testar o ícone de planeta.");
                return;
            }

            if (compassIconInstance == null)
            {
                Debug.LogWarning("[CompassPlanetIconTester] Atribua uma instância de CompassIcon para inicializar o teste.");
                return;
            }

            if (planetVisualConfig == null)
            {
                Debug.LogWarning("[CompassPlanetIconTester] Atribua uma CompassTargetVisualConfig configurada para planetas.");
            }

            _fakeTarget = new PlanetFakeTrackable(planetMaster.transform);
            compassIconInstance.Initialize(_fakeTarget, planetVisualConfig);
            LogIconState("Inicializado");
        }

        private void Update()
        {
            if (planetMaster == null)
            {
                return;
            }

            if (Input.GetKeyDown(assignResourceKey))
            {
                if (resourceToAssign != null)
                {
                    planetMaster.AssignResource(resourceToAssign);
                    LogIconState("ResourceAssigned");
                }
                else
                {
                    Debug.LogWarning("[CompassPlanetIconTester] resourceToAssign não definido no inspector.");
                }
            }

            if (Input.GetKeyDown(revealResourceKey))
            {
                planetMaster.RevealResource();
                LogIconState("ResourceDiscovered");
            }

            if (Input.GetKeyDown(hideResourceKey))
            {
                planetMaster.HideResource();
                LogIconState("ResourceHidden");
            }
        }

        private void LogIconState(string context)
        {
            string spriteName = compassIconInstance != null && compassIconInstance.iconImage != null && compassIconInstance.iconImage.sprite != null
                ? compassIconInstance.iconImage.sprite.name
                : "null";

            bool iconEnabled = compassIconInstance != null && compassIconInstance.iconImage != null && compassIconInstance.iconImage.enabled;
            Debug.Log($"[CompassPlanetIconTester] {context} -> discovered: {planetMaster.IsResourceDiscovered}, hasResource: {planetMaster.AssignedResource != null}, sprite: {spriteName}, iconEnabled: {iconEnabled}");
        }

        private sealed class PlanetFakeTrackable : ICompassTrackable
        {
            private readonly Transform _transform;

            public PlanetFakeTrackable(Transform transform)
            {
                _transform = transform;
            }

            public Transform Transform => _transform;
            public CompassTargetType TargetType => CompassTargetType.Planet;
            public bool IsActive => true;
        }
    }
}
