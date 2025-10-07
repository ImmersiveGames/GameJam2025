using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.DetectionsSystems.Runtime;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DetectionsSystems.Mono
{
    [DebugLevel(DebugLevel.Logs)]
    public class SensorController : MonoBehaviour
    {
        [SerializeField] private SensorCollection collection;

        private DetectorService _service;
        private IDetector _detector;

        public DetectorService Service => _service;
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

            _service = new DetectorService(transform, _detector, collection);

            DebugUtility.LogVerbose<SensorController>($"Configurado com {collection.Sensors.Count} sensores em {gameObject.name}");
        }

        private void Start()
        {
            DebugUtility.LogVerbose<SensorController>($"Start em {gameObject.name}");
        }

        private void Update()
        {
            _service?.Update(Time.deltaTime);
        }
    }
}