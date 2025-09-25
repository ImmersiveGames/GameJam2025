using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class ResourceThresholdMonitor : MonoBehaviour
    {
        [Header("Which resource to monitor")]
        [SerializeField] private ResourceType resourceType = ResourceType.Health;

        [Header("Thresholds config (SO)")]
        [SerializeField] private ResourceThresholdConfig thresholdConfig;

        [Header("Actor identification (optional)")]
        [Tooltip("Se vazio, tentamos pegar IActor.Name ou GameObject.name")]
        [SerializeField] private string actorId;

        [Header("Options")]
        [SerializeField] private bool autoDetectActorId = true;

        // runtime
        private float _lastPercentage = 1f; // último percentage conhecido
        private float[] _thresholds = Array.Empty<float>();
        private EventBinding<ResourceUpdateEvent> _updateBinding;

        private const float EPS = 1e-6f;

        private void Start()
        {
            // detect actor id
            if (autoDetectActorId)
            {
                var actor = GetComponent<ActorSystems.IActor>();
                actorId = actor?.Name ?? gameObject.name;
            }

            actorId = actorId?.Trim();

            // thresholds
            if (thresholdConfig != null)
            {
                _thresholds = thresholdConfig.GetNormalizedSortedThresholds();
            }
            else
            {
                // fallback: só 0 e 1
                _thresholds = new float[] { 0f, 1f };
                DebugUtility.LogWarning<ResourceThresholdMonitor>($"⚠️ ThresholdConfig não atribuído em {gameObject.name}. Usando apenas 0% e 100%.");
            }

            // tentar inicializar last percentage a partir do EntityResourceSystem (se existir)
            var ers = GetComponent<EntityResourceSystem>();
            if (ers != null && ers.HasResource(resourceType))
            {
                var res = ers.GetResource(resourceType);
                _lastPercentage = res != null ? res.GetPercentage() : 1f;
            }
            else
            {
                // default assume full
                _lastPercentage = 1f;
            }

            // registrar no EventBus para receber atualizações globais
            _updateBinding = new EventBinding<ResourceUpdateEvent>(OnResourceUpdated);
            EventBus<ResourceUpdateEvent>.Register(_updateBinding);

            DebugUtility.LogVerbose<ResourceThresholdMonitor>($"🔔 ThresholdMonitor iniciado para '{actorId}' {resourceType} com {_thresholds.Length} thresholds");
        }

        private void OnDestroy()
        {
            if (_updateBinding != null)
                EventBus<ResourceUpdateEvent>.Unregister(_updateBinding);
        }

        private void OnResourceUpdated(ResourceUpdateEvent evt)
        {
            // Filtrar por ator + tipo (comparação ignorando case)
            if (string.IsNullOrEmpty(actorId) || !string.Equals(evt.ActorId, actorId, StringComparison.OrdinalIgnoreCase))
                return;

            if (evt.ResourceType != resourceType) return;

            var newPct = evt.NewValue != null ? evt.NewValue.GetPercentage() : 0f;
            var oldPct = _lastPercentage;

            // nada mudou
            if (Mathf.Abs(newPct - oldPct) <= EPS) 
            {
                _lastPercentage = newPct;
                return;
            }

            bool ascending = newPct > oldPct;

            // detectar thresholds cruzados no intervalo (oldPct, newPct] para subida,
            // ou [newPct, oldPct) para descida
            List<float> crossed = new List<float>();

            if (ascending)
            {
                foreach (var t in _thresholds)
                {
                    // old < t <= new
                    if (oldPct + EPS < t && t <= newPct + EPS)
                        crossed.Add(t);
                }

                // ordenar em ordem crescente (na ordem em que foram cruzados)
                crossed.Sort((a, b) => a.CompareTo(b));
            }
            else
            {
                foreach (var t in _thresholds)
                {
                    // new <= t < old
                    if (newPct - EPS <= t && t < oldPct - EPS)
                        crossed.Add(t);
                }

                // ordenar em ordem decrescente (ordem do cruzamento)
                crossed.Sort((a, b) => b.CompareTo(a));
            }

            // disparar evento para cada threshold encontrado
            foreach (var threshold in crossed)
            {
                var evtOut = new ResourceThresholdEvent(evt.ActorId, evt.ResourceType, threshold, ascending, newPct);
                EventBus<ResourceThresholdEvent>.Raise(evtOut);

                DebugUtility.LogVerbose<ResourceThresholdMonitor>(
                    $"⚡ Threshold cruzado: Actor={evt.ActorId} Resource={evt.ResourceType} Threshold={threshold:P0} Direction={(ascending ? "↑" : "↓")} Current={newPct:P2}");
            }

            _lastPercentage = newPct;
        }

        // Utilitário para testes via inspector (dispara manualmente um ResourceUpdateEvent)
        [ContextMenu("Simulate 75%")]
        private void Simulate75()
        {
            var fake = new BasicResourceValue((int)(0.75f * 100), 100);
            var evt = new ResourceUpdateEvent(actorId, resourceType, fake);
            OnResourceUpdated(evt);
        }
    }
}
