using System;
using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.ResourceSystems.Services
{
    /// <summary>
    /// Serviço puro que monitora thresholds por resourceType e dispara ResourceThresholdEvent via EventBus.
    /// </summary>
    [DebugLevel(DebugLevel.Warning)]
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
                    DebugUtility.LogVerbose<ResourceThresholdService>($"Thresholds para {resourceType}: {string.Join(", ", _thresholds[resourceType])}");
                }
                else
                {
                    // Thresholds padrão quando não há configuração específica
                    _thresholds[resourceType] = new[] { 0f, 0.25f, 0.5f, 0.75f, 1f };
                    DebugUtility.LogVerbose<ResourceThresholdService>($"Usando thresholds padrão para {resourceType}");
                }

                _lastPercentages[resourceType] = resourceValue?.GetPercentage() ?? 0f;
                DebugUtility.LogVerbose<ResourceThresholdService>($"Percentual inicial para {resourceType}: {_lastPercentages[resourceType]}");
            }
        }

        private void OnResourceUpdated(ResourceUpdateEvent evt)
        {
            if (!_thresholds.ContainsKey(evt.ResourceType)) 
            {
                DebugUtility.LogWarning<ResourceThresholdService>($"Nenhum threshold configurado para {evt.ResourceType}");
                return;
            }

            float newPct = evt.NewValue?.GetPercentage() ?? 0f;
            float oldPct = _lastPercentages.GetValueOrDefault(evt.ResourceType, 0f);

            DebugUtility.LogVerbose<ResourceThresholdService>($"Recurso {evt.ResourceType} atualizado: {oldPct:F3} -> {newPct:F3}");

            if (Mathf.Abs(newPct - oldPct) < Epsilon)
            {
                _lastPercentages[evt.ResourceType] = newPct;
                return;
            }

            List<float> crossed = DetectCrossedThresholds(oldPct, newPct, _thresholds[evt.ResourceType]);
            
            if (crossed.Count > 0)
            {
                DebugUtility.LogVerbose<ResourceThresholdService>($"{crossed.Count} threshold(s) cruzado(s) para {evt.ResourceType}: {string.Join(", ", crossed)}");
                
                foreach (float thr in crossed)
                {
                    bool asc = newPct > oldPct;
                    var thrEvent = new ResourceThresholdEvent(evt.ActorId, evt.ResourceType, thr, asc, newPct);
                    DebugUtility.LogVerbose<ResourceThresholdService>($"Disparando ResourceThresholdEvent: {thrEvent.ResourceType} - Threshold {thr} - {(asc ? "SUBINDO" : "DESCENDO")}");
                    EventBus<ResourceThresholdEvent>.Raise(thrEvent);
                }
            }

            _lastPercentages[evt.ResourceType] = newPct;
        }

        private List<float> DetectCrossedThresholds(float oldVal, float newVal, float[] thresholds)
        {
            var crossed = new List<float>();
            bool ascending = newVal > oldVal;

            DebugUtility.LogVerbose<ResourceThresholdService>($"Detectando thresholds cruzados: {oldVal:F3} -> {newVal:F3} ({thresholds.Length} thresholds)");

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
                    DebugUtility.LogVerbose<ResourceThresholdService>($"Cruzou para CIMA no threshold {threshold}");
                }
                // Cruzou para baixo: estava acima e agora está abaixo ou igual
                else if (!ascending && wasAbove && !isAbove)
                {
                    crossed.Add(threshold);
                    DebugUtility.LogVerbose<ResourceThresholdService>($"Cruzou para BAIXO no threshold {threshold}");
                }
            }

            return crossed;
        }

        public void ForceCheck()
        {
            DebugUtility.LogVerbose<ResourceThresholdService>($"Forçando verificação de thresholds");
            
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