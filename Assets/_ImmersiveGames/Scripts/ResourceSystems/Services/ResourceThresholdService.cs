using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;

namespace _ImmersiveGames.Scripts.ResourceSystems.Services
{
    /// <summary>
    /// Serviço puro que monitora thresholds por resourceType e dispara ResourceThresholdEvent via EventBus.
    /// </summary>
    public class ResourceThresholdService : IDisposable
    {
        private readonly ResourceSystem _resourceSystem;
        private readonly Dictionary<ResourceType, float[]> _thresholds = new();
        private readonly Dictionary<ResourceType, float> _lastPercentages = new();
        private const float Epsilon = 0.0001f; // Reduzido para maior precisão

        public ResourceThresholdService(ResourceSystem resourceSystem)
        {
            _resourceSystem = resourceSystem ?? throw new ArgumentNullException(nameof(resourceSystem));
            Initialize();
            _resourceSystem.ResourceUpdated += OnResourceUpdated;
        }

        private void Initialize()
        {
            foreach (var (resourceType, resourceValue) in _resourceSystem.GetAll())
            {
                var inst = _resourceSystem.GetInstanceConfig(resourceType);
                
                // CORREÇÃO: Sempre inicializa thresholds, mesmo quando não há config específica
                if (inst?.thresholdConfig != null)
                {
                    _thresholds[resourceType] = inst.thresholdConfig.GetNormalizedSortedThresholds();
                    Debug.Log($"[ThresholdService] Thresholds para {resourceType}: {string.Join(", ", _thresholds[resourceType])}");
                }
                else
                {
                    // Thresholds padrão quando não há configuração específica
                    _thresholds[resourceType] = new[] { 0f, 0.25f, 0.5f, 0.75f, 1f };
                    Debug.Log($"[ThresholdService] Usando thresholds padrão para {resourceType}");
                }

                _lastPercentages[resourceType] = resourceValue?.GetPercentage() ?? 0f;
                Debug.Log($"[ThresholdService] Percentual inicial para {resourceType}: {_lastPercentages[resourceType]}");
            }
        }

        private void OnResourceUpdated(ResourceUpdateEvent evt)
        {
            if (!_thresholds.ContainsKey(evt.ResourceType)) 
            {
                Debug.LogWarning($"[ThresholdService] Nenhum threshold configurado para {evt.ResourceType}");
                return;
            }

            float newPct = evt.NewValue?.GetPercentage() ?? 0f;
            float oldPct = _lastPercentages.GetValueOrDefault(evt.ResourceType, 0f);

            Debug.Log($"[ThresholdService] Recurso {evt.ResourceType} atualizado: {oldPct:F3} -> {newPct:F3}");

            if (Mathf.Abs(newPct - oldPct) < Epsilon)
            {
                _lastPercentages[evt.ResourceType] = newPct;
                return;
            }

            List<float> crossed = DetectCrossedThresholds(oldPct, newPct, _thresholds[evt.ResourceType]);
            
            if (crossed.Count > 0)
            {
                Debug.Log($"[ThresholdService] {crossed.Count} threshold(s) cruzado(s) para {evt.ResourceType}: {string.Join(", ", crossed)}");
                
                foreach (float thr in crossed)
                {
                    bool asc = newPct > oldPct;
                    var thrEvent = new ResourceThresholdEvent(evt.ActorId, evt.ResourceType, thr, asc, newPct);
                    Debug.Log($"[ThresholdService] Disparando ResourceThresholdEvent: {thrEvent.ResourceType} - Threshold {thr} - {(asc ? "SUBINDO" : "DESCENDO")}");
                    EventBus<ResourceThresholdEvent>.Raise(thrEvent);
                }
            }

            _lastPercentages[evt.ResourceType] = newPct;
        }

        private List<float> DetectCrossedThresholds(float oldVal, float newVal, float[] thresholds)
        {
            var crossed = new List<float>();
            bool ascending = newVal > oldVal;

            Debug.Log($"[ThresholdService] Detectando thresholds cruzados: {oldVal:F3} -> {newVal:F3} ({thresholds.Length} thresholds)");

            foreach (float threshold in thresholds)
            {
                // Para evitar disparos duplicados no mesmo threshold
                bool wasBelow = oldVal < threshold - Epsilon;
                bool wasAbove = oldVal > threshold + Epsilon;
                bool isBelow = newVal < threshold - Epsilon;
                bool isAbove = newVal > threshold + Epsilon;

                // Cruzou para cima: estava abaixo e agora está acima ou igual
                if (ascending && wasBelow && !isBelow)
                {
                    crossed.Add(threshold);
                    Debug.Log($"[ThresholdService] Cruzou para CIMA no threshold {threshold}");
                }
                // Cruzou para baixo: estava acima e agora está abaixo ou igual
                else if (!ascending && wasAbove && !isAbove)
                {
                    crossed.Add(threshold);
                    Debug.Log($"[ThresholdService] Cruzou para BAIXO no threshold {threshold}");
                }
            }

            return crossed;
        }

        public void ForceCheck()
        {
            Debug.Log("[ThresholdService] Forçando verificação de thresholds");
            
            foreach (KeyValuePair<ResourceType, float[]> kv in _thresholds)
            {
                var res = _resourceSystem.Get(kv.Key);
                float newPct = res?.GetPercentage() ?? 0f;
                float oldPct = _lastPercentages.GetValueOrDefault(kv.Key, 0f);
                
                List<float> crossed = DetectCrossedThresholds(oldPct, newPct, kv.Value);
                foreach (float t in crossed)
                {
                    EventBus<ResourceThresholdEvent>.Raise(new ResourceThresholdEvent(
                        _resourceSystem.EntityId, kv.Key, t, newPct > oldPct, newPct));
                }
                
                _lastPercentages[kv.Key] = newPct;
            }
        }

        public void Dispose()
        {
            _resourceSystem.ResourceUpdated -= OnResourceUpdated;
            _thresholds.Clear();
            _lastPercentages.Clear();
        }
    }
}