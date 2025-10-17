using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Bind
{
    /// <summary>
    /// Bridge genérica para sincronizar uma definição de recurso visual com o ResourceSystem do ator.
    /// Mantém o ícone atualizado nos binds de UI sem misturar regras dos recursos numéricos.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/Resources/Injectable Entity Visual Bridge")]
    public sealed class InjectableEntityVisualBridge : ResourceBridgeBase
    {
        [Header("Visual Resource Settings")]
        [SerializeField] private VisualResourceDefinition defaultDefinition;
        [SerializeField] private ResourceInstanceConfig resourceConfig;

        private VisualResourceDefinition _currentDefinition;
        private VisualResourceValue _visualValue;
        private ResourceInstanceConfig _runtimeConfig;
        private ResourceType _currentType = ResourceType.None;
        private bool _isDirty;
        private bool _hasAppliedState;

        public VisualResourceDefinition CurrentDefinition => _currentDefinition;
        public bool HasDefinition => _currentDefinition != null;
        public ResourceType CurrentResourceType => _currentType;

        protected override void Awake()
        {
            base.Awake();

            if (!enabled)
            {
                return;
            }

            if (actor == null)
            {
                DebugUtility.LogError<InjectableEntityVisualBridge>($"{name} requer um componente que implemente {nameof(IActor)}.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (!enabled)
            {
                return;
            }

            if (_currentDefinition == null && defaultDefinition != null)
            {
                AssignDefinition(defaultDefinition, true);
            }
            else if (_currentDefinition != null)
            {
                MarkDirtyAndTrySync();
            }
        }

        protected override void OnServiceInitialized()
        {
            MarkDirtyAndTrySync();
        }

        protected override void OnServiceDispose()
        {
            _visualValue = null;
            _runtimeConfig = null;
            _currentType = ResourceType.None;
            _isDirty = false;
            _hasAppliedState = false;
        }

        /// <summary>
        /// Atribui uma nova definição visual para o ator.
        /// </summary>
        public bool AssignDefinition(VisualResourceDefinition definition, bool forceUpdate = false)
        {
            if (!enabled || definition == null)
            {
                return false;
            }

            if (!forceUpdate && _currentDefinition == definition && _hasAppliedState)
            {
                return false;
            }

            var resolvedType = ResolveResourceType(definition);
            if (resolvedType == ResourceType.None)
            {
                DebugUtility.LogError<InjectableEntityVisualBridge>($"{actor?.ActorName ?? name} tentou registrar um recurso visual sem ResourceType definido.", this);
                return false;
            }

            _currentDefinition = definition;
            _currentType = resolvedType;
            _hasAppliedState = false;

            MarkDirtyAndTrySync();
            return true;
        }

        /// <summary>
        /// Remove a definição visual atual do ator.
        /// </summary>
        public bool ClearDefinition()
        {
            if (!enabled)
            {
                return false;
            }

            bool hadDefinition = _currentDefinition != null || _hasAppliedState;

            _currentDefinition = null;
            _hasAppliedState = false;

            if (_currentType == ResourceType.None)
            {
                _currentType = ResolveResourceType(defaultDefinition);
            }

            if (_currentType == ResourceType.None)
            {
                DebugUtility.LogVerbose<InjectableEntityVisualBridge>($"{actor?.ActorName ?? name} não possui ResourceType válido para limpar.", this);
                return hadDefinition;
            }

            MarkDirtyAndTrySync();
            return hadDefinition;
        }

        private void MarkDirtyAndTrySync()
        {
            _isDirty = true;
            TryApplyState();
        }

        private void TryApplyState()
        {
            if (!_isDirty || !IsInitialized || resourceSystem == null)
            {
                return;
            }

            ApplyState();
        }

        private void ApplyState()
        {
            if (!_isDirty || resourceSystem == null)
            {
                return;
            }

            var targetType = _currentType;
            if (targetType == ResourceType.None)
            {
                targetType = ResolveResourceType(_currentDefinition) != ResourceType.None
                    ? ResolveResourceType(_currentDefinition)
                    : ResolveResourceType(defaultDefinition);
            }

            if (targetType == ResourceType.None)
            {
                DebugUtility.LogError<InjectableEntityVisualBridge>($"{actor?.ActorName ?? name} não possui ResourceType configurado para publicar visual.", this);
                _isDirty = false;
                return;
            }

            _currentType = targetType;

            var value = EnsureVisualValue();
            var config = ResolveInstanceConfig();

            if (_currentDefinition != null)
            {
                _currentDefinition.ApplyTo(value);
            }

            resourceSystem.RegisterOrUpdateResource(targetType, value, config);

            _hasAppliedState = true;
            _isDirty = false;
        }

        private VisualResourceValue EnsureVisualValue()
        {
            if (_visualValue == null)
            {
                _visualValue = CreateValueFromDefinition(_currentDefinition) ?? new VisualResourceValue();
            }

            if (_currentDefinition != null)
            {
                var maxValue = _visualValue.GetMaxValue();
                if (Mathf.Approximately(maxValue, 0f))
                {
                    maxValue = 1f;
                }

                _visualValue.SetCurrentValue(maxValue);
                _visualValue.SetIcon(_currentDefinition.GetIcon());
            }
            else
            {
                _visualValue.SetCurrentValue(0f);
                _visualValue.SetIcon(null);
            }

            return _visualValue;
        }

        private static VisualResourceValue CreateValueFromDefinition(VisualResourceDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            var created = definition.CreateInitialValue();
            if (created is VisualResourceValue visual)
            {
                return visual;
            }

            return new VisualResourceValue(created, definition.GetIcon());
        }

        private ResourceInstanceConfig ResolveInstanceConfig()
        {
            if (resourceConfig != null)
            {
                if (resourceConfig.resourceDefinition == null && _currentDefinition != null)
                {
                    resourceConfig.resourceDefinition = _currentDefinition;
                }

                return resourceConfig;
            }

            if (_currentDefinition == null)
            {
                return null;
            }

            _runtimeConfig ??= new ResourceInstanceConfig();
            _runtimeConfig.resourceDefinition = _currentDefinition;
            return _runtimeConfig;
        }

        private ResourceType ResolveResourceType(VisualResourceDefinition definition)
        {
            if (definition != null && definition.ResourceCategory != ResourceType.None)
            {
                return definition.ResourceCategory;
            }

            if (resourceConfig?.resourceDefinition != null && resourceConfig.resourceDefinition.ResourceCategory != ResourceType.None)
            {
                return resourceConfig.resourceDefinition.ResourceCategory;
            }

            if (defaultDefinition != null && defaultDefinition.ResourceCategory != ResourceType.None)
            {
                return defaultDefinition.ResourceCategory;
            }

            return _currentType;
        }
    }
}
