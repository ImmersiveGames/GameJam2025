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
                foreach (var target in trackables)
                {
                    if (target is not { IsActive: true } || target.Transform == null)
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

            var visualConfig = visualDatabase != null
                ? visualDatabase.GetConfig(target.TargetType)
                : null;

            var iconInstance = Instantiate(iconPrefab, compassRectTransform);
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

            foreach (var pair in _iconsByTarget)
            {
                var target = pair.Key;
                var icon = pair.Value;

                if (ShouldRemove(target, icon, enforceActiveSnapshot, _activeTrackablesCache))
                {
                    _removalBuffer.Add(target);
                }
            }

            foreach (var target in _removalBuffer)
            {
                DestroyIconForTarget(target);
            }

            _removalBuffer.Clear();
        }

        private static bool ShouldRemove(
            ICompassTrackable target,
            CompassIcon icon,
            bool enforceActiveSnapshot,
            HashSet<ICompassTrackable> activeSnapshot)
        {
            if (target == null || icon == null)
            {
                return true;
            }

            if (target.Transform == null || !target.IsActive)
            {
                return true;
            }

            if (enforceActiveSnapshot && (activeSnapshot == null || !activeSnapshot.Contains(target)))
            {
                return true;
            }

            return false;
        }

        private void DestroyIconForTarget(ICompassTrackable target)
        {
            if (!_iconsByTarget.TryGetValue(target, out var icon))
            {
                return;
            }

            if (icon != null)
            {
                Destroy(icon.gameObject);
            }

            _iconsByTarget.Remove(target);
        }

        /// <summary>
        /// Atualiza a bússola calculando ângulo, posição e distância para cada alvo rastreável.
        /// Refatorado para reduzir a complexidade e melhorar legibilidade, extraindo helpers reutilizáveis.
        /// </summary>
        private void UpdateCompass()
        {
            // Guard clauses de configuração básica
            if (compassRectTransform == null || iconPrefab == null)
            {
                return;
            }

            // Serviço de runtime
            if (!CompassRuntimeService.TryGet(out var runtimeService))
            {
                return;
            }

            var playerTransform = runtimeService.PlayerTransform;
            IReadOnlyList<ICompassTrackable> trackables = runtimeService.Trackables;

            // Sincroniza alvos e ícones existentes
            SynchronizeIcons(trackables);

            // Sem player ou sem ícones, nada para atualizar
            if (playerTransform == null || _iconsByTarget.Count == 0)
            {
                return;
            }

            // Parâmetros comuns de cálculo
            var playerForward = GetHorizontalForward(playerTransform);
            float halfAngle = settings != null ? Mathf.Abs(settings.compassHalfAngleDegrees) : 180f;
            float halfWidth = compassRectTransform.rect.width * 0.5f;
            bool clampIcons = settings != null && settings.clampIconsAtEdges;

            // Atualiza cada ícone
            foreach (var pair in _iconsByTarget)
            {
                UpdateIconForTarget(pair.Key, pair.Value, playerTransform.position, playerForward, halfAngle, halfWidth, clampIcons);
            }

            // Limpa quaisquer ícones/targets inválidos detectados durante o ciclo
            RemoveStaleIcons(false);
        }

        private static Vector3 GetHorizontalForward(Transform t)
        {
            var fwd = t.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f)
            {
                return Vector3.forward;
            }
            return fwd;
        }

        private void UpdateIconForTarget(
            ICompassTrackable target,
            CompassIcon icon,
            Vector3 playerPos,
            Vector3 horizontalForward,
            float halfAngle,
            float halfWidth,
            bool clampAtEdges)
        {
            if (target == null || icon == null || target.Transform == null || !target.IsActive)
            {
                // Deixa a remoção para RemoveStaleIcons(false)
                return;
            }

            var toTarget = target.Transform.position - playerPos;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            // Se praticamente em cima do player
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                icon.gameObject.SetActive(true);
                SetIconPosition(icon, 0f, halfAngle, halfWidth);
                icon.UpdateDistance(distance);
                return;
            }

            float angle = Vector3.SignedAngle(horizontalForward, toTarget, Vector3.up);

            // Sem ângulo útil, apenas oculta
            if (halfAngle <= 0f)
            {
                icon.gameObject.SetActive(false);
                return;
            }

            // Fora do FOV da bússola
            if (Mathf.Abs(angle) > halfAngle)
            {
                if (!clampAtEdges)
                {
                    icon.gameObject.SetActive(false);
                    return;
                }

                angle = Mathf.Clamp(angle, -halfAngle, halfAngle);
            }

            icon.gameObject.SetActive(true);
            SetIconPosition(icon, angle, halfAngle, halfWidth);
            icon.UpdateDistance(distance);
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
            var anchoredPos = icon.rectTransform.anchoredPosition;
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
                yield return default;
            }

            if (_iconsByTarget == null) yield break;
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
