using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using _ImmersiveGames.Scripts.Utils;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SkinSystems.Runtime
{
    /// <summary>
    /// Acompanha um ActorSkinController e calcula o estado geométrico (Bounds reais) das skins.
    ///
    /// - Modo 1: usa instâncias criadas pelo sistema de skin (OnSkinInstancesCreated).
    /// - Modo 2 (fallback opcional): calcula a partir do root do ator (útil para planetas já prontos no prefab).
    /// </summary>
    [DisallowMultipleComponent]
    public class SkinRuntimeStateTracker : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Controller de skin dono das instâncias que serão medidas. Se não for atribuído, será buscado automaticamente no mesmo GameObject.")]
        [SerializeField] private ActorSkinController skinController;

        [Header("Dependency Injection")]
        [Tooltip("Se verdadeiro, também registra este tracker como serviço global além do registro por objeto.")]
        [SerializeField] private bool registerAsGlobalService;

        [Header("Fallback de Medição")]
        [Tooltip("Quando verdadeiro, se nenhuma skin tiver sido criada ainda, o tracker calcula um estado inicial a partir do root do ator (ex.: planeta já montado no prefab).")]
        [SerializeField] private bool computeInitialStateFromActorRoot = true;

        [Tooltip("ModelType usado para o estado inicial calculado a partir do root do ator.")]
        [SerializeField] private ModelType initialStateModelType = ModelType.ModelRoot; // ajuste no Inspector conforme seu enum

        /// <summary>
        /// Estados em runtime por ModelType.
        /// </summary>
        private readonly Dictionary<ModelType, SkinRuntimeState> _states = new();

        /// <summary>
        /// Últimas instâncias conhecidas por ModelType (caso queira depurar ou recalcular depois).
        /// </summary>
        private readonly Dictionary<ModelType, List<GameObject>> _instancesByType = new();

        public IReadOnlyDictionary<ModelType, SkinRuntimeState> States => _states;

        // Para debug de DI
        private string _objectId;
        private bool _initialStateComputedFromRoot;

        #region Unity Lifecycle

        private void Awake()
        {
            if (skinController == null)
            {
                skinController = GetComponent<ActorSkinController>();
                if (skinController == null)
                {
                    DebugUtility.LogError<SkinRuntimeStateTracker>(
                        $"SkinRuntimeStateTracker em {name} não encontrou ActorSkinController no mesmo GameObject.");
                }
            }

            RegisterWithDependencyManager();
        }

        private void OnEnable()
        {
            RegisterToControllerEvents();
        }

        private void Start()
        {
            // Se ainda não há estados e o fallback estiver habilitado, tenta medir a partir do root do ator
            TryComputeInitialStateFromActorRoot();
        }

        private void OnDisable()
        {
            UnregisterFromControllerEvents();
        }

        #endregion

        #region Dependency Manager Registration

        private void RegisterWithDependencyManager()
        {
            RegisterAsObjectServiceIfPossible();
            RegisterAsGlobalServiceIfRequested();
        }

        private void RegisterAsObjectServiceIfPossible()
        {
            if (skinController == null || skinController.OwnerActor == null)
            {
                DebugUtility.LogWarning<SkinRuntimeStateTracker>(
                    $"SkinRuntimeStateTracker em {name} não possui ActorSkinController/OwnerActor válido para registro por objeto.");
                return;
            }

            _objectId = skinController.OwnerActor.ActorId;

            if (string.IsNullOrEmpty(_objectId))
            {
                DebugUtility.LogWarning<SkinRuntimeStateTracker>(
                    $"SkinRuntimeStateTracker em {name} encontrou OwnerActor sem ActorId. Registro por objeto ignorado.");
                return;
            }

            TryRegisterForObject(_objectId);
        }

        private void RegisterAsGlobalServiceIfRequested()
        {
            if (!registerAsGlobalService) return;

            try
            {
                DependencyManager.Provider.RegisterGlobal(this);
                DebugUtility.LogVerbose<SkinRuntimeStateTracker>(
                    $"SkinRuntimeStateTracker registrado como serviço global no DependencyManager. GameObject={name}");
            }
            catch (Exception e)
            {
                DebugUtility.LogWarning<SkinRuntimeStateTracker>(
                    $"Falha ao registrar SkinRuntimeStateTracker como serviço global no DependencyManager: {e.Message}");
            }
        }

        private void TryRegisterForObject(string objectId)
        {
            try
            {
                DependencyManager.Provider.RegisterForObject(objectId, this);
                DebugUtility.LogVerbose<SkinRuntimeStateTracker>(
                    $"SkinRuntimeStateTracker registrado no DependencyManager como serviço de objeto. ActorId={objectId}, GameObject={name}");
            }
            catch (Exception e)
            {
                DebugUtility.LogWarning<SkinRuntimeStateTracker>(
                    $"Falha ao registrar SkinRuntimeStateTracker como serviço de objeto no DependencyManager: {e.Message}");
            }
        }

        #endregion

        #region Event Wiring

        private void RegisterToControllerEvents()
        {
            if (skinController == null) return;

            skinController.OnSkinInstancesCreated += HandleSkinInstancesCreated;
        }

        private void UnregisterFromControllerEvents()
        {
            if (skinController == null) return;

            skinController.OnSkinInstancesCreated -= HandleSkinInstancesCreated;
        }

        private void HandleSkinInstancesCreated(ModelType type, List<GameObject> instances)
        {
            if (instances == null || instances.Count == 0)
            {
                _states[type] = SkinRuntimeState.Empty(type);
                _instancesByType[type] = new List<GameObject>();
                return;
            }

            // Guarda instâncias para depuração/recalculo posterior
            if (!_instancesByType.TryGetValue(type, out var storedList))
            {
                storedList = new List<GameObject>();
                _instancesByType[type] = storedList;
            }
            storedList.Clear();
            storedList.AddRange(instances);

            // Calcula bounds reais usando CalculateRealLength
            var bounds = CalculateWorldBoundsForInstances(instances);

            var state = new SkinRuntimeState(type, bounds);
            _states[type] = state;

            DebugUtility.LogVerbose<SkinRuntimeStateTracker>(
                $"[{name}] Atualizado SkinRuntimeState para {type}: Center={state.Center}, Size={state.Size}, Radius≈{state.ApproxRadius:F2}");
        }

        #endregion

        #region Fallback: Actor Root

        /// <summary>
        /// Se ainda não houver estados calculados e o fallback estiver ativo,
        /// calcula um estado inicial a partir do root do ator (útil para planetas).
        /// </summary>
        private void TryComputeInitialStateFromActorRoot()
        {
            if (!computeInitialStateFromActorRoot)
                return;

            if (_initialStateComputedFromRoot)
                return;

            if (_states.Count > 0)
                return; // Já temos estados via sistema de skin

            if (skinController == null || skinController.OwnerActor == null)
                return;

            GameObject root = skinController.OwnerActor.Transform.gameObject;
            if (root == null)
                return;

            var instances = new List<GameObject> { root };
            var bounds = CalculateWorldBoundsForInstances(instances);
            var state = new SkinRuntimeState(initialStateModelType, bounds);

            _states[initialStateModelType] = state;
            _instancesByType[initialStateModelType] = instances;
            _initialStateComputedFromRoot = true;

            DebugUtility.LogVerbose<SkinRuntimeStateTracker>(
                $"[{name}] Estado inicial calculado a partir do root do ator. ModelType={initialStateModelType}, Center={state.Center}, Size={state.Size}, Radius≈{state.ApproxRadius:F2}");
        }

        #endregion

        #region Bounds Calculation

        /// <summary>
        /// Calcula os bounds globais englobando todas as instâncias,
        /// utilizando CalculateRealLength para cada raiz.
        /// </summary>
        private static Bounds CalculateWorldBoundsForInstances(IReadOnlyList<GameObject> instances)
        {
            bool hasBounds = false;
            var result = new Bounds(Vector3.zero, Vector3.zero);

            foreach (var instance in instances)
            {
                if (instance == null) continue;

                // Usa CalculateRealLength para lidar com objetos compostos e IgnoreBoundsFlag
                var instanceBounds = CalculateRealLength.GetBounds(instance);

                // Se o bounds retornado for zerado, ignoramos
                if (instanceBounds.size == Vector3.zero)
                    continue;

                if (!hasBounds)
                {
                    result = instanceBounds;
                    hasBounds = true;
                }
                else
                {
                    result.Encapsulate(instanceBounds);
                }
            }

            if (!hasBounds)
            {
                // Nenhuma instância com bounds válida encontrada
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            return result;
        }

        #endregion

        #region Public API

        public bool TryGetState(ModelType type, out SkinRuntimeState state)
        {
            return _states.TryGetValue(type, out state);
        }

        public SkinRuntimeState GetStateOrEmpty(ModelType type)
        {
            return _states.TryGetValue(type, out var state)
                ? state
                : SkinRuntimeState.Empty(type);
        }

        private void RecalculateState(ModelType type)
        {
            if (!_instancesByType.TryGetValue(type, out var instances) || instances == null || instances.Count == 0)
            {
                _states[type] = SkinRuntimeState.Empty(type);
                return;
            }

            var bounds = CalculateWorldBoundsForInstances(instances);
            _states[type] = new SkinRuntimeState(type, bounds);
        }

        public void RecalculateAllStates()
        {
            var keys = new List<ModelType>(_instancesByType.Keys);
            foreach (var t in keys)
            {
                RecalculateState(t);
            }
        }

        #endregion

        #region Debug / Editor Helpers

        public void LogAllStatesToConsole()
        {
            // Tenta computar o fallback se ainda não houver estados
            if (_states.Count == 0)
            {
                TryComputeInitialStateFromActorRoot();
            }

            if (_states.Count == 0)
            {
                DebugUtility.LogVerbose<SkinRuntimeStateTracker>(
                    $"[{name}] SkinRuntimeStateTracker não possui estados calculados ainda.");
                return;
            }

            foreach (var kvp in _states)
            {
                var state = kvp.Value;
                DebugUtility.LogVerbose<SkinRuntimeStateTracker>(
                    $"[{name}] ModelType={state.modelType} | Center={state.Center} | Size={state.Size} | Radius≈{state.ApproxRadius:F2} | HasValidBounds={state.HasValidBounds}");
            }
        }

        #endregion
    }
}
