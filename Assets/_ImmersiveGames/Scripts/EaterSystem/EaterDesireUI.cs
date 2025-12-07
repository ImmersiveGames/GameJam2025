using System.Collections.Generic;
using _ImmersiveGames.Scripts.EaterSystem.Events;
using _ImmersiveGames.Scripts.GameplaySystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [DefaultExecutionOrder(50)]
    [DisallowMultipleComponent]
    public sealed class EaterDesireUI : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private Image desireIcon;
        [SerializeField] private EaterBehavior eaterBehavior;
        [SerializeField, Tooltip("Sprite utilizada quando não houver desejo ativo ou quando o ícone do recurso estiver indisponível.")]
        private Sprite fallbackSprite;
        [SerializeField, Tooltip("Quando verdadeiro, oculta a imagem se não existir desejo ativo.")]
        private bool hideWhenNoDesire = true;

        [Inject] private IGameplayManager _gameplayManager;

        private bool _pendingIconResolve;
        private bool _warnedMissingIcon;
        private bool _warnedMissingBehavior;
        private bool _warnedMissingManager;
        private bool _syncedInitialInfo;
        private PlanetsManager _planetsManager;
        private EaterDesireInfo _currentInfo = EaterDesireInfo.Inactive;
        private bool _listeningPlanets;
        private bool _listeningDesires;
        private EventBinding<EaterDesireInfoChangedEvent> _desireChangedBinding;
        private EventBinding<PlanetsInitializationCompletedEvent> _planetsInitializedBinding;
        private EventBinding<PlanetCreatedEvent> _planetCreatedBinding;
        private readonly HashSet<PlanetResources> _missingDefinitionWarnings = new();
        private readonly HashSet<PlanetResources> _missingSpriteWarnings = new();

        private bool SharesGameObjectWithIcon =>
            desireIcon != null && desireIcon.gameObject == gameObject;

        private void Awake()
        {
            DependencyManager.Provider.InjectDependencies(this);

            if (desireIcon == null && TryGetComponent(out Image resolvedIcon))
            {
                desireIcon = resolvedIcon;
                DebugUtility.LogVerbose(
                    "Imagem de desejo resolvida automaticamente a partir do próprio GameObject.",
                    context: this,
                    instance: this);
            }
        }

        private void OnEnable()
        {
            RegisterPlanetEvents();
            RegisterDesireEvents();

            bool resolved = TryResolveBehavior();
            bool synced = resolved && SyncCurrentInfoFromBehavior();

            if (!synced)
            {
                ApplyCurrentInfo();
            }
        }

        private void OnDisable()
        {
            UnregisterPlanetEvents();
            UnregisterDesireEvents();
            ResetCurrentInfo();
            ShowNoDesireState();
            _planetsManager = null;
            _warnedMissingManager = false;
            _pendingIconResolve = false;
            ClearMissingResourceWarnings();
        }

        private void LateUpdate()
        {
            if (eaterBehavior == null)
            {
                TryResolveBehavior();
            }

            if (!_syncedInitialInfo)
            {
                SyncCurrentInfoFromBehavior();
            }

            if (_pendingIconResolve)
            {
                ApplyCurrentInfo();
            }
        }

        private void RegisterDesireEvents()
        {
            if (_listeningDesires)
            {
                return;
            }

            _desireChangedBinding ??= new EventBinding<EaterDesireInfoChangedEvent>(HandleDesireChanged);
            EventBus<EaterDesireInfoChangedEvent>.Register(_desireChangedBinding);
            _listeningDesires = true;

            DebugUtility.LogVerbose(
                "Escutando alterações globais de desejo do Eater via EventBus.",
                context: this,
                instance: this);
        }

        private void UnregisterDesireEvents()
        {
            if (!_listeningDesires)
            {
                return;
            }

            if (_desireChangedBinding != null)
            {
                EventBus<EaterDesireInfoChangedEvent>.Unregister(_desireChangedBinding);
            }

            _listeningDesires = false;
        }

        private void HandleDesireChanged(EaterDesireInfoChangedEvent evt)
        {
            if (!evt.HasBehavior)
            {
                if (eaterBehavior == null)
                {
                    DebugUtility.LogVerbose(
                        "Evento de desejo recebido sem referência ao comportamento. Aguardando resolução do Eater.",
                        context: this,
                        instance: this);
                    return;
                }
            }
            else if (eaterBehavior == null)
            {
                eaterBehavior = evt.Behavior;
                _warnedMissingBehavior = false;

                DebugUtility.LogVerbose(
                    $"EaterBehavior associado via EventBus: {eaterBehavior.name}.",
                    context: this,
                    instance: this);
            }
            else if (evt.Behavior != eaterBehavior)
            {
                return;
            }

            UpdateDesireInfo(evt.Info);
        }

        private void UpdateDesireInfo(EaterDesireInfo info)
        {
            if (_syncedInitialInfo && info.Equals(_currentInfo))
            {
                return;
            }

            _currentInfo = info;
            _syncedInitialInfo = true;
            ApplyCurrentInfo();
        }

        private bool SyncCurrentInfoFromBehavior()
        {
            if (eaterBehavior == null)
            {
                return false;
            }

            UpdateDesireInfo(eaterBehavior.GetCurrentDesireInfo());
            return true;
        }

        private void ResetCurrentInfo()
        {
            _currentInfo = EaterDesireInfo.Inactive;
            _syncedInitialInfo = false;
        }

        private void ApplyCurrentInfo()
        {
            if (desireIcon == null)
            {
                if (!_warnedMissingIcon)
                {
                    DebugUtility.LogWarning(
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

            DebugUtility.LogVerbose(
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

            DebugUtility.LogVerbose(
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

                if (_currentInfo.Resource != null)
                    DebugUtility.LogWarning(
                        _currentInfo.HasResource
                            ? $"Ícone específico para {_currentInfo.Resource.Value} indisponível. Utilizando fallback."
                            : "Ícone de desejo indisponível. Utilizando fallback.",
                        context: this,
                        instance: this);
            }
            else
            {
                SetIconVisibility(false);
                if (_currentInfo.Resource != null)
                    DebugUtility.LogWarning(
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

            var iconObject = desireIcon.gameObject;
            var canvasRenderer = desireIcon.canvasRenderer;

            // Determina estado desejado de ativação do GameObject
            bool shouldBeActive = visible || !(hideWhenNoDesire && !SharesGameObjectWithIcon);

            // Aplica ativação somente se necessário
            if (iconObject.activeSelf != shouldBeActive)
            {
                iconObject.SetActive(shouldBeActive);
            }

            // Se deve ficar inativo, não há mais nada a fazer
            if (!shouldBeActive)
            {
                return;
            }

            // Quando ativo, ajusta habilitação e alpha conforme visibilidade
            bool shouldBeEnabled = visible;
            float targetAlpha = visible ? 1f : 0f;

            if (desireIcon.enabled != shouldBeEnabled)
            {
                desireIcon.enabled = shouldBeEnabled;
            }

            if (canvasRenderer != null)
            {
                canvasRenderer.SetAlpha(targetAlpha);
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
                    DebugUtility.LogWarning(
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
                    DebugUtility.LogWarning(
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
                    DebugUtility.LogVerbose(
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

            DebugUtility.LogVerbose(
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

        private bool TryResolveBehavior()
        {
            if (eaterBehavior != null)
            {
                return true;
            }

            Transform eaterTransform = (_gameplayManager ?? GameplayManager.Instance)?.WorldEater;
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
                DebugUtility.LogVerbose(
                    $"EaterBehavior localizado para UI: {eaterBehavior.name}.",
                    context: this,
                    instance: this);
                _warnedMissingBehavior = false;
                return true;
            }

            if (!_warnedMissingBehavior)
            {
                DebugUtility.LogWarning(
                    "EaterBehavior não encontrado para atualizar o HUD de desejos.",
                    context: this,
                    instance: this);
                _warnedMissingBehavior = true;
            }

            return false;
        }
    }
}

