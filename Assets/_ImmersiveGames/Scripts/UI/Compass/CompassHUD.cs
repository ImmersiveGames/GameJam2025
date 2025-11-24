using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.World.Compass;
using UnityEngine;

namespace _ImmersiveGames.Scripts.UI.Compass
{
    /// <summary>
    /// HUD da bússola responsável por instanciar e manter ícones para alvos rastreáveis.
    /// Integra-se ao pipeline de canvas implementando <see cref="ICanvasBinder"/> e
    /// registrando-se no <see cref="CanvasPipelineManager"/> após a injeção de dependências.
    /// </summary>
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

        [Tooltip("Configurações gerais da bússola.")]
        public CompassSettings settings;

        [Tooltip("Banco de dados com as configurações visuais por tipo de alvo.")]
        public CompassVisualDatabase visualDatabase;

        [Tooltip("Prefab do ícone utilizado para cada alvo rastreável.")]
        public CompassIcon iconPrefab;

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

        public string GetObjectId() => CanvasId;

        private void Awake()
        {
            InjectionState = DependencyInjectionState.Pending;
            State = CanvasInitializationState.Pending;

            SetupCanvasId();

            // Registra para receber injeção via ResourceInitializationManager, replicando o padrão dos demais HUDs.
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

            DebugUtility.Log<CompassHUD>($"✅ CompassHUD registrado no pipeline com id '{CanvasId}'");
        }

        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(CanvasId) && CanvasPipelineManager.HasInstance)
            {
                CanvasPipelineManager.Instance.UnregisterCanvas(CanvasId);
            }
        }

        private void Update()
        {
            UpdateCompass();
        }

        private void SynchronizeIcons(IReadOnlyList<ICompassTrackable> trackables)
        {
            bool hasSnapshot = trackables != null;
            _activeTrackablesCache.Clear();

            if (hasSnapshot)
            {
                for (int i = 0; i < trackables.Count; i++)
                {
                    ICompassTrackable target = trackables[i];
                    if (target == null || !target.IsActive || target.Transform == null)
                    {
                        continue;
                    }

                    if (_activeTrackablesCache.Add(target))
                    {
                        EnsureIconForTarget(target);
                    }
                }
            }

            RemoveStaleIcons(hasSnapshot);
        }

        private void EnsureIconForTarget(ICompassTrackable target)
        {
            if (_iconsByTarget.ContainsKey(target))
            {
                return;
            }

            CompassTargetVisualConfig visualConfig = visualDatabase != null
                ? visualDatabase.GetConfig(target.TargetType)
                : null;

            CompassIcon iconInstance = Instantiate(iconPrefab, compassRectTransform);
            iconInstance.Initialize(target, visualConfig);

            _iconsByTarget[target] = iconInstance;
        }

        private void RemoveStaleIcons(bool enforceActiveSnapshot)
        {
            if (_iconsByTarget.Count == 0)
            {
                return;
            }

            _removalBuffer.Clear();

            foreach (KeyValuePair<ICompassTrackable, CompassIcon> pair in _iconsByTarget)
            {
                ICompassTrackable target = pair.Key;
                CompassIcon icon = pair.Value;

                bool missingTarget = target == null || target.Transform == null || !target.IsActive;
                bool missingIcon = icon == null;
                bool notTracked = enforceActiveSnapshot && !_activeTrackablesCache.Contains(target);

                if (missingTarget || missingIcon || notTracked)
                {
                    _removalBuffer.Add(target);
                }
            }

            for (int i = 0; i < _removalBuffer.Count; i++)
            {
                ICompassTrackable target = _removalBuffer[i];
                if (_iconsByTarget.TryGetValue(target, out CompassIcon icon))
                {
                    if (icon != null)
                    {
                        Destroy(icon.gameObject);
                    }

                    _iconsByTarget.Remove(target);
                }
            }

            _removalBuffer.Clear();
        }

        /// <summary>
        /// Atualiza a bússola calculando ângulo, posição e distância para cada alvo rastreável.
        /// </summary>
        private void UpdateCompass()
        {
            if (compassRectTransform == null || iconPrefab == null)
            {
                return;
            }

            if (!CompassRuntimeService.TryGet(out ICompassRuntimeService runtimeService))
            {
                return;
            }

            Transform playerTransform = runtimeService.PlayerTransform;
            IReadOnlyList<ICompassTrackable> trackables = runtimeService.Trackables;

            SynchronizeIcons(trackables);

            if (playerTransform == null || _iconsByTarget.Count == 0)
            {
                return;
            }

            Vector3 playerForward = playerTransform.forward;
            playerForward.y = 0f;
            if (playerForward.sqrMagnitude < 0.0001f)
            {
                playerForward = Vector3.forward;
            }

            float halfAngle = settings != null ? Mathf.Abs(settings.compassHalfAngleDegrees) : 180f;
            float width = compassRectTransform.rect.width;
            float halfWidth = width * 0.5f;
            bool clampIcons = settings != null && settings.clampIconsAtEdges;

            _removalBuffer.Clear();

            foreach (KeyValuePair<ICompassTrackable, CompassIcon> pair in _iconsByTarget)
            {
                ICompassTrackable target = pair.Key;
                CompassIcon icon = pair.Value;

                if (target == null || icon == null || target.Transform == null)
                {
                    _removalBuffer.Add(target);
                    continue;
                }

                Vector3 toTarget = target.Transform.position - playerTransform.position;
                toTarget.y = 0f;
                float distance = toTarget.magnitude;

                if (toTarget.sqrMagnitude < 0.0001f)
                {
                    icon.gameObject.SetActive(true);
                    SetIconPosition(icon, 0f, halfAngle, halfWidth);
                    icon.UpdateDistance(distance);
                    continue;
                }

                float angle = Vector3.SignedAngle(playerForward, toTarget, Vector3.up);

                if (halfAngle <= 0f)
                {
                    icon.gameObject.SetActive(false);
                    continue;
                }

                if (Mathf.Abs(angle) > halfAngle)
                {
                    if (!clampIcons)
                    {
                        icon.gameObject.SetActive(false);
                        continue;
                    }

                    angle = Mathf.Clamp(angle, -halfAngle, halfAngle);
                }

                icon.gameObject.SetActive(true);
                SetIconPosition(icon, angle, halfAngle, halfWidth);
                icon.UpdateDistance(distance);
            }

            if (_removalBuffer.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _removalBuffer.Count; i++)
            {
                ICompassTrackable target = _removalBuffer[i];
                if (_iconsByTarget.TryGetValue(target, out CompassIcon icon))
                {
                    if (icon != null)
                    {
                        Destroy(icon.gameObject);
                    }

                    _iconsByTarget.Remove(target);
                }
            }
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
            if (icon.rectTransform == null)
            {
                return;
            }

            float normalized = Mathf.Approximately(halfAngle, 0f) ? 0f : angle / halfAngle;
            float x = normalized * halfWidth;
            Vector2 anchoredPos = icon.rectTransform.anchoredPosition;
            anchoredPos.x = x;
            icon.rectTransform.anchoredPosition = anchoredPos;
        }

        /// <summary>
        /// Permite que outros componentes (ex.: highlight de planetas) inspecionem os ícones atualmente ativos.
        /// </summary>
        public IEnumerable<(ICompassTrackable target, CompassIcon icon)> EnumerateIcons()
        {
            if (_iconsByTarget == null || _iconsByTarget.Count == 0)
            {
                return Enumerable.Empty<(ICompassTrackable, CompassIcon)>();
            }

            foreach (KeyValuePair<ICompassTrackable, CompassIcon> pair in _iconsByTarget)
            {
                yield return (pair.Key, pair.Value);
            }
        }

        // As interfaces abaixo mantêm compatibilidade com o pipeline de Canvas, mesmo sem binds de recursos.
        public void ScheduleBind(string actorId, ResourceType resourceType, IResourceValue data)
        {
            // CompassHUD não utiliza binds de recursos; método mantido para aderir ao contrato do pipeline.
        }

        public bool CanAcceptBinds() => State == CanvasInitializationState.Ready;

        public IReadOnlyDictionary<string, Dictionary<ResourceType, ResourceUISlot>> GetActorSlots() => EmptyActorSlots;
    }
}
