using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.CompassSystems;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.UI.Compass
{
    /// <summary>
    /// HUD da bússola otimizada, responsiva e totalmente configurável.
    /// Funciona perfeitamente com cenas aditivas e multiplayer local.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CompassHUD : MonoBehaviour, ICanvasBinder
    {
        private static readonly IReadOnlyDictionary<string, Dictionary<ResourceType, ResourceUISlot>> EmptyActorSlots =
            new Dictionary<string, Dictionary<ResourceType, ResourceUISlot>>();

        private readonly Dictionary<ICompassTrackable, CompassIcon> _iconsByTarget = new();
        private readonly HashSet<ICompassTrackable> _activeTrackablesCache = new();
        private readonly List<ICompassTrackable> _removalBuffer = new();

        [Header("Compass UI")]
        [Tooltip("Área da UI onde os ícones da bússola serão posicionados.")]
        public RectTransform compassRectTransform;

        [Tooltip("Configurações gerais da bússola (ângulo, distância, etc.).")]
        public CompassSettings settings;

        [Tooltip("Banco de dados com as configurações visuais por tipo de alvo.")]
        public CompassVisualDatabase visualDatabase;

        [Tooltip("Prefab do ícone utilizado para cada alvo rastreável.")]
        public CompassIcon iconPrefab;

        [Header("Performance & Responsividade")]
        [Tooltip("Intervalo em segundos entre atualizações da bússola.\n" +
                 "Valores recomendados:\n" +
                 "• 0.016 → 60 FPS (muito fluido)\n" +
                 "• 0.033 → 30 FPS (equilíbrio)\n" +
                 "• 0.066 → 15 FPS (leve, mas perceptível)")]
        [Range(0.008f, 0.2f)]
        public float updateInterval = 0.016f; // ~60 FPS por padrão → super suave

        [Header("Canvas Pipeline")]
        [SerializeField] private string canvasId = "CompassHUD";
        [SerializeField] private bool autoGenerateCanvasId = true;
        [SerializeField] private CanvasType canvasType = CanvasType.Scene;
        [SerializeField] private bool registerInPipeline = true;

        [Inject] private IUniqueIdFactory _idFactory;

        public string CanvasId { get; private set; }
        public CanvasType Type => canvasType;
        public CanvasInitializationState State { get; private set; }
        public DependencyInjectionState InjectionState { get; set; }

        private float _updateTimer;

        private void Awake()
        {
            InjectionState = DependencyInjectionState.Pending;
            State = CanvasInitializationState.Pending;
            SetupCanvasId();
            ResourceInitializationManager.Instance.RegisterForInjection(this);
        }

        public void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Injecting;
            State = CanvasInitializationState.Injecting;

            if (registerInPipeline && CanvasPipelineManager.HasInstance)
            {
                CanvasPipelineManager.Instance.RegisterCanvas(this);
            }

            State = CanvasInitializationState.Ready;
            InjectionState = DependencyInjectionState.Ready;

            DebugUtility.Log<CompassHUD>($"CompassHUD registrado com sucesso (Update: {1f/updateInterval:F1} FPS)");
        }

        public string GetObjectId() => CanvasId;

        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(CanvasId) && CanvasPipelineManager.HasInstance)
            {
                CanvasPipelineManager.Instance.UnregisterCanvas(CanvasId);
            }
        }

        private void Update()
        {
            _updateTimer += Time.deltaTime;
            if (_updateTimer >= updateInterval)
            {
                _updateTimer -= updateInterval;
                UpdateCompass();
            }
        }

        private void UpdateCompass()
        {
            if (settings == null || compassRectTransform == null || iconPrefab == null || visualDatabase == null)
                return;

            if (!CompassRuntimeService.TryGet(out var service) || service.PlayerTransform == null)
                return;

            var trackables = service.Trackables;
            if (trackables == null) return;

            SynchronizeIcons(trackables);

            float halfAngle = settings.compassHalfAngleDegrees * 0.5f;
            float halfWidth = compassRectTransform.rect.width * 0.5f;
            bool clampAtEdges = settings.clampIconsAtEdges;
            var (minDistance, maxDistance) = GetDistanceLimits();

            var playerPos = service.PlayerTransform.position;
            var forward = service.PlayerTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.01f) forward.Normalize();

            foreach (var pair in _iconsByTarget)
            {
                var target = pair.Key;
                var icon = pair.Value;

                if (target == null || !target.IsActive || target.Transform == null || icon == null)
                {
                    icon.gameObject.SetActive(false);
                    continue;
                }

                var toTarget = target.Transform.position - playerPos;
                toTarget.y = 0f;
                float distance = toTarget.magnitude;
                float displayDistance = Mathf.Max(distance, minDistance);

                if (maxDistance > 0f && distance > maxDistance)
                {
                    icon.gameObject.SetActive(false);
                    continue;
                }

                if (toTarget.sqrMagnitude < 0.0001f)
                {
                    SetIconPosition(icon, 0f, halfAngle, halfWidth);
                    icon.UpdateDistance(displayDistance);
                    icon.gameObject.SetActive(true);
                    continue;
                }

                float angle = Vector3.SignedAngle(forward, toTarget, Vector3.up);

                bool shouldShow = halfAngle > 0f && (Mathf.Abs(angle) <= halfAngle || clampAtEdges);

                if (!shouldShow)
                {
                    icon.gameObject.SetActive(false);
                    continue;
                }

                if (clampAtEdges && Mathf.Abs(angle) > halfAngle)
                    angle = Mathf.Clamp(angle, -halfAngle, halfAngle);

                icon.gameObject.SetActive(true);
                SetIconPosition(icon, angle, halfAngle, halfWidth);
                icon.UpdateDistance(displayDistance);
            }
        }

        private void SynchronizeIcons(IReadOnlyList<ICompassTrackable> trackables)
        {
            _activeTrackablesCache.Clear();
            _removalBuffer.Clear();

            foreach (var trackable in trackables)
            {
                if (trackable == null || !trackable.IsActive || trackable.Transform == null)
                    continue;

                _activeTrackablesCache.Add(trackable);

                if (!_iconsByTarget.ContainsKey(trackable))
                {
                    CreateIconFor(trackable);
                }
            }

            foreach (var pair in _iconsByTarget)
            {
                if (!_activeTrackablesCache.Contains(pair.Key))
                    _removalBuffer.Add(pair.Key);
            }

            foreach (var toRemove in _removalBuffer)
            {
                if (_iconsByTarget.TryGetValue(toRemove, out var icon) && icon != null)
                {
                    Destroy(icon.gameObject);
                }
                _iconsByTarget.Remove(toRemove);
            }
        }

        private void CreateIconFor(ICompassTrackable trackable)
        {
            var config = visualDatabase.GetConfig(trackable.TargetType);
            if (config == null)
            {
                DebugUtility.LogWarning<CompassHUD>($"Sem config visual para alvo do tipo: {trackable.TargetType}");
                return;
            }

            var iconObj = Instantiate(iconPrefab, compassRectTransform);
            var icon = iconObj.GetComponent<CompassIcon>();
            if (icon != null)
            {
                icon.Initialize(trackable, config);
                _iconsByTarget[trackable] = icon;
            }
            else
            {
                Destroy(iconObj);
            }
        }

        private (float minDistance, float maxDistance) GetDistanceLimits()
        {
            float min = settings != null ? Mathf.Max(0f, settings.minDistance) : 0f;
            float max = settings != null ? Mathf.Max(0f, settings.maxDistance) : 0f;
            return (min, max);
        }

        private void SetupCanvasId()
        {
            if (autoGenerateCanvasId)
            {
                CanvasId = _idFactory?.GenerateId(gameObject, prefix: "CompassHUD") ?? gameObject.name;
            }
            else
            {
                CanvasId = string.IsNullOrWhiteSpace(canvasId) ? gameObject.name : canvasId;
            }
        }

        private static void SetIconPosition(CompassIcon icon, float angle, float halfAngle, float halfWidth)
        {
            if (icon.rectTransform == null) return;

            float normalized = Mathf.Approximately(halfAngle, 0f) ? 0f : angle / halfAngle;
            float x = normalized * halfWidth;
            var pos = icon.rectTransform.anchoredPosition;
            pos.x = x;
            icon.rectTransform.anchoredPosition = pos;
        }

        public void ForEachIcon(Action<ICompassTrackable, CompassIcon> action)
        {
            if (action == null || _iconsByTarget == null) return;

            foreach (var pair in _iconsByTarget)
            {
                if (pair.Key != null && pair.Value != null)
                    action(pair.Key, pair.Value);
            }
        }

        // ICanvasBinder
        public void ScheduleBind(string actorId, ResourceType resourceType, IResourceValue data) { }
        public bool CanAcceptBinds() => State == CanvasInitializationState.Ready;
        public IReadOnlyDictionary<string, Dictionary<ResourceType, ResourceUISlot>> GetActorSlots() => EmptyActorSlots;
    }
}