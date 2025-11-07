using System.Collections.Generic;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [DefaultExecutionOrder(50)]
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class EaterDesireUI : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private Image desireIcon;
        [SerializeField] private EaterBehavior eaterBehavior;
        [SerializeField, Tooltip("Sprite utilizada quando não houver desejo ativo ou quando o ícone do recurso estiver indisponível.")]
        private Sprite fallbackSprite;
        [SerializeField, Tooltip("Quando verdadeiro, oculta a imagem se não existir desejo ativo.")]
        private bool hideWhenNoDesire = true;

        private bool _subscribed;
        private bool _pendingIconResolve;
        private bool _warnedMissingIcon;
        private bool _warnedMissingBehavior;
        private bool _warnedMissingManager;
        private PlanetsManager _planetsManager;
        private EaterBehavior _activeBehavior;
        private EaterDesireInfo _currentInfo = EaterDesireInfo.Inactive;
        private bool _listeningPlanets;
        private EventBinding<PlanetsInitializationCompletedEvent> _planetsInitializedBinding;
        private EventBinding<PlanetCreatedEvent> _planetCreatedBinding;
        private readonly HashSet<PlanetResources> _missingDefinitionWarnings = new();
        private readonly HashSet<PlanetResources> _missingSpriteWarnings = new();

        private bool SharesGameObjectWithIcon =>
            desireIcon != null && desireIcon.gameObject == gameObject;

        private void Awake()
        {
            if (desireIcon == null && TryGetComponent(out Image resolvedIcon))
            {
                desireIcon = resolvedIcon;
                DebugUtility.LogVerbose<EaterDesireUI>(
                    "Imagem de desejo resolvida automaticamente a partir do próprio GameObject.",
                    context: this,
                    instance: this);
            }
        }

        private void OnEnable()
        {
            RegisterPlanetEvents();
            TryResolveBehavior();
            SubscribeToBehavior();
            ApplyCurrentInfo();
        }

        private void OnDisable()
        {
            UnregisterPlanetEvents();
            UnsubscribeFromBehavior();
            ShowNoDesireState();
            _planetsManager = null;
            _warnedMissingManager = false;
            _pendingIconResolve = false;
            ClearMissingResourceWarnings();
        }

        private void LateUpdate()
        {
            if (!_subscribed)
            {
                TryResolveBehavior();
                SubscribeToBehavior();
                if (_subscribed)
                {
                    ApplyCurrentInfo();
                }
            }

            if (_pendingIconResolve)
            {
                ApplyCurrentInfo();
            }
        }

        private void SubscribeToBehavior()
        {
            if (_subscribed && eaterBehavior != _activeBehavior)
            {
                UnsubscribeFromBehavior();
            }

            if (eaterBehavior == null)
            {
                if (!_warnedMissingBehavior)
                {
                    DebugUtility.LogWarning<EaterDesireUI>(
                        "EaterBehavior não encontrado para atualizar o HUD de desejos.",
                        context: this,
                        instance: this);
                    _warnedMissingBehavior = true;
                }
                return;
            }

            if (_subscribed)
            {
                return;
            }

            eaterBehavior.EventDesireChanged += HandleDesireChanged;
            _currentInfo = eaterBehavior.GetCurrentDesireInfo();
            _subscribed = true;
            _warnedMissingBehavior = false;
            _activeBehavior = eaterBehavior;

            DebugUtility.LogVerbose<EaterDesireUI>(
                $"Assinatura estabelecida com {eaterBehavior.name}.",
                context: this,
                instance: this);
        }

        private void UnsubscribeFromBehavior()
        {
            if (_subscribed && _activeBehavior != null)
            {
                _activeBehavior.EventDesireChanged -= HandleDesireChanged;
            }

            _subscribed = false;
            _activeBehavior = null;
            _currentInfo = EaterDesireInfo.Inactive;
        }

        private void HandleDesireChanged(EaterDesireInfo info)
        {
            _currentInfo = info;
            ApplyCurrentInfo();
        }

        private void ApplyCurrentInfo()
        {
            if (desireIcon == null)
            {
                if (!_warnedMissingIcon)
                {
                    DebugUtility.LogWarning<EaterDesireUI>(
                        "Componente Image do ícone de desejo não configurado.",
                        context: this,
                        instance: this);
                    _warnedMissingIcon = true;
                }
                return;
            }

            if (!_currentInfo.ServiceActive || !_currentInfo.HasDesire || !_currentInfo.HasResource)
            {
                ShowNoDesireState();
                return;
            }

            if (!TryGetResourceIcon(_currentInfo.Resource!.Value, out Sprite icon))
            {
                if (_pendingIconResolve)
                {
                    return;
                }

                UseFallbackIcon();
                return;
            }

            desireIcon.sprite = icon;
            desireIcon.overrideSprite = icon;
            SetIconVisibility(true);
            desireIcon.SetAllDirty();

            _pendingIconResolve = false;

            DebugUtility.LogVerbose<EaterDesireUI>(
                $"Ícone de desejo atualizado para {_currentInfo.Resource.Value} (disp={_currentInfo.IsAvailable}, planetas={_currentInfo.AvailableCount}).",
                context: this,
                instance: this);
        }

        private void ShowNoDesireState()
        {
            _pendingIconResolve = false;

            if (desireIcon == null)
            {
                return;
            }

            if (hideWhenNoDesire)
            {
                SetIconVisibility(false);
            }
            else if (fallbackSprite != null)
            {
                desireIcon.sprite = fallbackSprite;
                desireIcon.overrideSprite = fallbackSprite;
                SetIconVisibility(true);
            }
            else
            {
                SetIconVisibility(false);
            }

            DebugUtility.LogVerbose<EaterDesireUI>(
                "Nenhum desejo ativo para exibir na UI.",
                context: this,
                instance: this);
        }

        private void UseFallbackIcon()
        {
            if (desireIcon == null)
            {
                return;
            }

            if (fallbackSprite != null)
            {
                desireIcon.sprite = fallbackSprite;
                desireIcon.overrideSprite = fallbackSprite;
                SetIconVisibility(true);

                DebugUtility.LogWarning<EaterDesireUI>(
                    _currentInfo.HasResource
                        ? $"Ícone específico para {_currentInfo.Resource.Value} indisponível. Utilizando fallback."
                        : "Ícone de desejo indisponível. Utilizando fallback.",
                    context: this,
                    instance: this);
            }
            else
            {
                SetIconVisibility(false);
                DebugUtility.LogWarning<EaterDesireUI>(
                    _currentInfo.HasResource
                        ? $"Ícone específico para {_currentInfo.Resource.Value} não encontrado e nenhum fallback foi configurado."
                        : "Ícone de desejo não encontrado e nenhum fallback foi configurado.",
                    context: this,
                    instance: this);
            }
        }

        private void SetIconVisibility(bool visible)
        {
            if (desireIcon == null)
            {
                return;
            }

            GameObject iconObject = desireIcon.gameObject;
            CanvasRenderer renderer = desireIcon.canvasRenderer;

            if (visible)
            {
                if (!iconObject.activeSelf)
                {
                    iconObject.SetActive(true);
                }

                if (!desireIcon.enabled)
                {
                    desireIcon.enabled = true;
                }

                if (renderer != null)
                {
                    renderer.SetAlpha(1f);
                }

                return;
            }

            if (hideWhenNoDesire && !SharesGameObjectWithIcon)
            {
                if (iconObject.activeSelf)
                {
                    iconObject.SetActive(false);
                }

                return;
            }

            if (!iconObject.activeSelf)
            {
                iconObject.SetActive(true);
            }

            if (desireIcon.enabled)
            {
                desireIcon.enabled = false;
            }

            if (renderer != null)
            {
                renderer.SetAlpha(0f);
            }
        }

        private bool TryGetResourceIcon(PlanetResources resource, out Sprite icon)
        {
            icon = null;

            if (!TryGetPlanetsManager(out PlanetsManager manager))
            {
                return false;
            }

            if (!manager.TryGetResourceDefinition(resource, out PlanetResourcesSo definition) || definition == null)
            {
                if (_missingDefinitionWarnings.Add(resource))
                {
                    DebugUtility.LogWarning<EaterDesireUI>(
                        $"Nenhuma definição encontrada para o recurso {resource}.",
                        context: this,
                        instance: this);
                }

                _pendingIconResolve = true;
                return false;
            }

            icon = definition.ResourceIcon;
            if (icon == null)
            {
                if (_missingSpriteWarnings.Add(resource))
                {
                    DebugUtility.LogWarning<EaterDesireUI>(
                        $"A definição do recurso {resource} não possui sprite configurado.",
                        context: this,
                        instance: this);
                }

                _pendingIconResolve = true;
                return false;
            }

            _missingDefinitionWarnings.Remove(resource);
            _missingSpriteWarnings.Remove(resource);
            return true;
        }

        private bool TryGetPlanetsManager(out PlanetsManager manager)
        {
            if (_planetsManager != null)
            {
                manager = _planetsManager;
                if (manager != null)
                {
                    return true;
                }

                _planetsManager = null;
            }

            manager = PlanetsManager.Instance;
            if (manager == null)
            {
                if (!_warnedMissingManager)
                {
                    DebugUtility.LogVerbose<EaterDesireUI>(
                        "PlanetsManager ainda não está disponível. Aguardando para resolver ícone de desejo.",
                        context: this,
                        instance: this);
                    _warnedMissingManager = true;
                }

                _pendingIconResolve = true;
                return false;
            }

            _planetsManager = manager;
            _warnedMissingManager = false;
            _pendingIconResolve = false;
            ClearMissingResourceWarnings();

            DebugUtility.LogVerbose<EaterDesireUI>(
                "PlanetsManager localizado para resolução de ícones de desejo.",
                context: this,
                instance: this);
            return true;
        }

        private void RegisterPlanetEvents()
        {
            if (_listeningPlanets)
            {
                return;
            }

            _planetsInitializedBinding ??= new EventBinding<PlanetsInitializationCompletedEvent>(HandlePlanetsInitialized);
            EventBus<PlanetsInitializationCompletedEvent>.Register(_planetsInitializedBinding);

            _planetCreatedBinding ??= new EventBinding<PlanetCreatedEvent>(HandlePlanetCreated);
            EventBus<PlanetCreatedEvent>.Register(_planetCreatedBinding);

            _listeningPlanets = true;
        }

        private void UnregisterPlanetEvents()
        {
            if (!_listeningPlanets)
            {
                return;
            }

            if (_planetsInitializedBinding != null)
            {
                EventBus<PlanetsInitializationCompletedEvent>.Unregister(_planetsInitializedBinding);
            }

            if (_planetCreatedBinding != null)
            {
                EventBus<PlanetCreatedEvent>.Unregister(_planetCreatedBinding);
            }

            _listeningPlanets = false;
        }

        private void HandlePlanetsInitialized(PlanetsInitializationCompletedEvent _)
        {
            _planetsManager = null;
            _warnedMissingManager = false;
            ClearMissingResourceWarnings();
            _pendingIconResolve = true;
            ApplyCurrentInfo();
        }

        private void HandlePlanetCreated(PlanetCreatedEvent _)
        {
            if (!_currentInfo.ServiceActive || !_currentInfo.HasResource)
            {
                return;
            }

            ClearMissingResourceWarnings();
            _pendingIconResolve = true;
            ApplyCurrentInfo();
        }

        private void ClearMissingResourceWarnings()
        {
            _missingDefinitionWarnings.Clear();
            _missingSpriteWarnings.Clear();
        }

        private void TryResolveBehavior()
        {
            if (eaterBehavior != null)
            {
                return;
            }

            Transform eaterTransform = GameManager.Instance != null ? GameManager.Instance.WorldEater : null;
            if (eaterTransform != null)
            {
                if (!eaterTransform.TryGetComponent(out eaterBehavior))
                {
                    eaterBehavior = eaterTransform.GetComponentInChildren<EaterBehavior>(true);
                }
            }

            if (eaterBehavior == null)
            {
                eaterBehavior = FindFirstObjectByType<EaterBehavior>();
            }

            if (eaterBehavior != null)
            {
                DebugUtility.LogVerbose<EaterDesireUI>(
                    $"EaterBehavior localizado para UI: {eaterBehavior.name}.",
                    context: this,
                    instance: this);
            }
        }
    }
}

