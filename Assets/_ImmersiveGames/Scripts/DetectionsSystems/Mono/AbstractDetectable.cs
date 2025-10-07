using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace _ImmersiveGames.Scripts.DetectionsSystems.Mono
{
    [DebugLevel(DebugLevel.Warning)]
    public abstract class AbstractDetectable : MonoBehaviour, IDetectable
    {
        [SerializeField] protected DetectionType myDetectionType;

        private IActor _owner;
        private EventBinding<DetectionEnterEvent> _enterBinding;
        private EventBinding<DetectionExitEvent> _exitBinding;
        
        // Cache por evento por frame
        private readonly Dictionary<string, int> _processedEvents = new();

        protected virtual void Awake()
        {
            _owner = GetComponent<IActor>();
            if (_owner == null)
            {
                DebugUtility.LogError<AbstractDetectable>($"Componente IActor não encontrado em {gameObject.name}");
                return;
            }

            if (myDetectionType == null)
            {
                DebugUtility.LogError<AbstractDetectable>($"DetectionType não configurado em {gameObject.name}");
                return;
            }

            // Registrar eventos de detecção
            _enterBinding = new EventBinding<DetectionEnterEvent>(OnDetectionEnterEvent);
            _exitBinding = new EventBinding<DetectionExitEvent>(OnDetectionExitEvent);

            EventBus<DetectionEnterEvent>.Register(_enterBinding);
            EventBus<DetectionExitEvent>.Register(_exitBinding);

            DebugUtility.LogVerbose<AbstractDetectable>($"Inicializado em {gameObject.name} com tipo: {myDetectionType.TypeName}");
        }

        protected virtual void OnDestroy()
        {
            EventBus<DetectionEnterEvent>.Unregister(_enterBinding);
            EventBus<DetectionExitEvent>.Unregister(_exitBinding);
        }

        public IActor Owner => _owner;

        // Métodos abstratos para classes concretas implementarem
        public abstract void OnEnterDetection(IDetector detector, DetectionType detectionType);
        public abstract void OnExitDetection(IDetector detector, DetectionType detectionType);

        // Manipulação de eventos (filtro por myDetectionType aqui)
        protected virtual void OnDetectionEnterEvent(DetectionEnterEvent enterEvent)
        {
            if (!ReferenceEquals(enterEvent.Detectable, this) || enterEvent.DetectionType != myDetectionType) return;

            // Criar chave única para este evento
            string eventKey = $"ENTER_{enterEvent.Detector.GetHashCode()}_{enterEvent.DetectionType.GetHashCode()}";
            
            // Verificar se já processamos este evento neste frame
            if (_processedEvents.TryGetValue(eventKey, out int lastFrame) && lastFrame == Time.frameCount)
                return;
            
            _processedEvents[eventKey] = Time.frameCount;

            DebugUtility.LogVerbose<AbstractDetectable>($"EVENTO ENTRADA [Frame {Time.frameCount}]: Detectado por {GetName(enterEvent.Detector)} em {gameObject.name}");

            OnEnterDetection(enterEvent.Detector, enterEvent.DetectionType);

            // Limpar eventos antigos
            CleanupEventCache();
        }

        protected virtual void OnDetectionExitEvent(DetectionExitEvent exitEvent)
        {
            if (!ReferenceEquals(exitEvent.Detectable, this) || exitEvent.DetectionType != myDetectionType) return;

            // Criar chave única para este evento
            string eventKey = $"EXIT_{exitEvent.Detector.GetHashCode()}_{exitEvent.DetectionType.GetHashCode()}";
            
            // Verificar se já processamos este evento neste frame
            if (_processedEvents.TryGetValue(eventKey, out int lastFrame) && lastFrame == Time.frameCount)
                return;
            
            _processedEvents[eventKey] = Time.frameCount;

            DebugUtility.LogVerbose<AbstractDetectable>($"EVENTO SAÍDA [Frame {Time.frameCount}]: Perdeu detecção por {GetName(exitEvent.Detector)} em {gameObject.name}");

            OnExitDetection(exitEvent.Detector, exitEvent.DetectionType);

            // Limpar eventos antigos
            CleanupEventCache();
        }

        private void CleanupEventCache()
        {
            // Remove eventos com mais de 1 frame de idade
            var oldEvents = _processedEvents.Where(kvp => kvp.Value < Time.frameCount - 1)
                                          .Select(kvp => kvp.Key).ToList();
            
            foreach (string key in oldEvents)
                _processedEvents.Remove(key);
        }

        protected static string GetName(IDetector detector)
        {
            return detector.Owner?.ActorName ?? detector.ToString();
        }
    }
}