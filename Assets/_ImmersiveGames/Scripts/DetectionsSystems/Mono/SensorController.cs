using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.DetectionsSystems.Runtime;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DetectionsSystems.Mono
{
    
    public class SensorController : MonoBehaviour
    {
        [SerializeField] private SensorCollection collection;

        private IDetector _detector;

        public DetectorService Service { get; private set; }
        public SensorCollection Collection => collection;

        private void Awake()
        {
            DebugUtility.LogVerbose<SensorController>($"Awake em {gameObject.name}");

            _detector = GetComponent<IDetector>();
            if (_detector == null)
            {
                DebugUtility.LogError<SensorController>($"Nenhum IDetector encontrado em {gameObject.name}");
                enabled = false;
                return;
            }

            if (collection == null)
            {
                DebugUtility.LogError<SensorController>($"SensorCollection não atribuído em {gameObject.name}");
                enabled = false;
                return;
            }

            if (collection.Sensors.Count == 0)
            {
                DebugUtility.LogError<SensorController>($"Nenhum SensorConfig na collection em {gameObject.name}");
                enabled = false;
                return;
            }

            Service = new DetectorService(transform, _detector, collection);

            DebugUtility.Log<SensorController>($"Configurado com {collection.Sensors.Count} sensores em {gameObject.name}");
        }

        private void Start()
        {
            DebugUtility.LogVerbose<SensorController>($"Start em {gameObject.name}");
        }

        private void Update()
        {
            Service?.Update(Time.deltaTime);
        }
    }
}