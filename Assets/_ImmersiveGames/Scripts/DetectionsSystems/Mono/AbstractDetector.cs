using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace _ImmersiveGames.Scripts.DetectionsSystems.Mono
{
    
    public abstract class AbstractDetector : MonoBehaviour, IDetector
    {
        private IActor _owner;
        private EventBinding<DetectionEnterEvent> _enterBinding;
        private EventBinding<DetectionExitEvent> _exitBinding;
        private readonly HashSet<IDetectable> _detectedItems = new();
        
        // Cache por evento por frame para evitar duplicação
        private readonly Dictionary<string, int> _processedEvents = new();

        protected virtual void Awake()
        {
            _owner = GetComponent<IActor>();
            if (_owner == null)
            {
                DebugUtility.LogError<AbstractDetector>($"Componente IActor não encontrado em {gameObject.name}");
                return;
            }

            // Registrar eventos de detecção
            _enterBinding = new EventBinding<DetectionEnterEvent>(OnDetectionEnterEvent);
            _exitBinding = new EventBinding<DetectionExitEvent>(OnDetectionExitEvent);

            EventBus<DetectionEnterEvent>.Register(_enterBinding);
            EventBus<DetectionExitEvent>.Register(_exitBinding);

            DebugUtility.Log<AbstractDetector>($"Inicializado em {gameObject.name}");
        }

        protected virtual void OnDestroy()
        {
            EventBus<DetectionEnterEvent>.Unregister(_enterBinding);
            EventBus<DetectionExitEvent>.Unregister(_exitBinding);
            ClearCache();
        }

        public IActor Owner => _owner;

        // Métodos abstratos para classes concretas implementarem
        public abstract void OnDetected(IDetectable detectable, DetectionType detectionType);
        public abstract void OnLost(IDetectable detectable, DetectionType detectionType);

        // Manipulação de eventos
        protected virtual void OnDetectionEnterEvent(DetectionEnterEvent enterEvent)
        {
            if (!ReferenceEquals(enterEvent.Detector, this) || enterEvent.DetectionType == null) return;

            // Criar chave única para este evento
            string eventKey = $"ENTER_{enterEvent.Detectable.GetHashCode()}_{enterEvent.DetectionType.GetHashCode()}";
            
            // Verificar se já processamos este evento neste frame
            if (_processedEvents.TryGetValue(eventKey, out int lastFrame) && lastFrame == Time.frameCount)
                return;
            
            _processedEvents[eventKey] = Time.frameCount;

            // Adiciona ao cache de detecções
            bool added = _detectedItems.Add(enterEvent.Detectable);

            DebugUtility.LogVerbose<AbstractDetector>($"EVENTO ENTRADA [Frame {Time.frameCount}]: Detectado {GetName(enterEvent.Detectable)} " +
                $"como {enterEvent.DetectionType.TypeName}, Novo: {added}, Cache: {_detectedItems.Count} em {gameObject.name}");

            // Chama método abstrato apenas se foi uma nova detecção
            if (added) 
            {
                OnDetected(enterEvent.Detectable, enterEvent.DetectionType);
            }

            // Limpar eventos antigos do cache
            CleanupEventCache();
        }

        protected virtual void OnDetectionExitEvent(DetectionExitEvent exitEvent)
        {
            if (!ReferenceEquals(exitEvent.Detector, this) || exitEvent.DetectionType == null) return;

            // Criar chave única para este evento
            string eventKey = $"EXIT_{exitEvent.Detectable.GetHashCode()}_{exitEvent.DetectionType.GetHashCode()}";
            
            // Verificar se já processamos este evento neste frame
            if (_processedEvents.TryGetValue(eventKey, out int lastFrame) && lastFrame == Time.frameCount)
                return;
            
            _processedEvents[eventKey] = Time.frameCount;

            // Remove do cache de detecções
            bool removed = _detectedItems.Remove(exitEvent.Detectable);

            DebugUtility.LogVerbose<AbstractDetector>($"EVENTO SAÍDA [Frame {Time.frameCount}]: Perdeu {GetName(exitEvent.Detectable)} como " +
                $"{exitEvent.DetectionType.TypeName}, Removido: {removed}, Cache: {_detectedItems.Count} em {gameObject.name}");

            // Chama método abstrato apenas se o item estava no cache
            if (removed) 
            {
                OnLost(exitEvent.Detectable, exitEvent.DetectionType);
            }

            // Limpar eventos antigos do cache
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

        protected static string GetName(IDetectable detectable)
        {
            return detectable.Owner?.ActorName ?? detectable.ToString();
        }

        // Método para classes concretas acessarem o cache
        protected IReadOnlyCollection<IDetectable> GetDetectedItems()
        {
            // Limpar referências nulas
            _detectedItems.RemoveWhere(item => item == null || (item as MonoBehaviour)?.gameObject == null);
            return _detectedItems;
        }

        // Método para limpar o cache manualmente
        private void ClearCache()
        {
            _detectedItems.Clear();
            _processedEvents.Clear();
            OnCacheCleared();
        }

        // Método virtual para classes concretas reagirem à limpeza do cache
        protected virtual void OnCacheCleared() { }
    }
}