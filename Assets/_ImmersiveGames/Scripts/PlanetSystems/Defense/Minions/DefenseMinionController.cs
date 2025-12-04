using System;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    [DebugLevel(level: DebugLevel.Verbose)]
    [RequireComponent(typeof(MinionEntryHandler))]
    [RequireComponent(typeof(MinionOrbitWaitHandler))]
    [RequireComponent(typeof(MinionChaseHandler))]
    public sealed class DefenseMinionController : MonoBehaviour
    {
        private enum MinionState
        {
            Inactive,
            Entry,
            OrbitWait,
            Chase
        }

        private const float DefaultEntryDurationSeconds = 0.75f;
        private const float DefaultInitialScaleFactor = 0.2f;
        private const float DefaultOrbitIdleDelaySeconds = 0.75f;
        private const float DefaultChaseSpeed = 3f;
        private const string DefaultTargetLabel = "Unknown";

        // Campos de runtime, sempre preenchidos via profile para evitar dados duplicados em prefabs.
        private float entryDurationSeconds = DefaultEntryDurationSeconds;
        private float initialScaleFactor   = DefaultInitialScaleFactor;
        private float orbitIdleDelaySeconds = DefaultOrbitIdleDelaySeconds;
        private float chaseSpeed = DefaultChaseSpeed;
        private MinionEntryStrategySo entryStrategy;
        private MinionChaseStrategySo chaseStrategy;

        [Header("Resolução de alvo / Role (fallback)")]
        [SerializeField]
        private DefenseRoleConfig roleConfig;

        private Vector3 _finalScale;
        private bool _finalScaleCaptured;

        private MinionState _state = MinionState.Inactive;

        private Vector3 _planetCenter;
        private Vector3 _orbitPosition;

        private Transform _targetTransform;
        private string _targetLabel = DefaultTargetLabel;
        private DefenseRole _targetRole = DefenseRole.Unknown;
        private DetectionType _detectionType;
        private IPoolable _poolable;

        [SerializeField]
        private MinionEntryHandler entryHandler;

        [SerializeField]
        private MinionOrbitWaitHandler orbitWaitHandler;

        [SerializeField]
        private MinionChaseHandler chaseHandler;

        #region Unity lifecycle

        private void OnEnable()
        {
            CancelHandlers();

            if (!_finalScaleCaptured)
            {
                _finalScale = transform.localScale;
                _finalScaleCaptured = true;
            }

            transform.localScale = _finalScale;
            _poolable ??= GetComponent<IPoolable>();
            ResetRuntimeState();
        }

        private void OnDisable()
        {
            CancelHandlers();
            ResetRuntimeState();
        }

        private void CancelHandlers()
        {
            EnsureHandlers();

            entryHandler?.CancelEntry();
            orbitWaitHandler?.CancelOrbitWait();
            chaseHandler?.CancelChase();
        }

        private void EnsureHandlers()
        {
            entryHandler ??= GetComponent<MinionEntryHandler>();
            orbitWaitHandler ??= GetComponent<MinionOrbitWaitHandler>();
            chaseHandler ??= GetComponent<MinionChaseHandler>();
        }

        #endregion

        #region Configuração do alvo

        public void OnSpawned(MinionSpawnContext context)
        {
            EnsureHandlers();

            ResetRuntimeState();

            _planetCenter = context.Planet != null
                ? context.Planet.transform.position
                : context.SpawnPosition;
            _orbitPosition = context.OrbitPosition;
            _detectionType = context.DetectionType;

            _targetLabel = string.IsNullOrWhiteSpace(context.TargetLabel)
                ? DefaultTargetLabel
                : context.TargetLabel;

            _targetRole = context.TargetRole != DefenseRole.Unknown
                ? context.TargetRole
                : ResolveRoleFromLabel(_targetLabel);

            _targetTransform = ResolveTargetTransform();

            transform.position = context.SpawnPosition;
            if (context.SpawnDirection.sqrMagnitude > 0.0001f)
            {
                transform.forward = context.SpawnDirection.normalized;
            }

            DebugUtility.LogVerbose<DefenseMinionController>(
                $"[Spawn] {name} recebeu contexto: Role={_targetRole}, Label='{_targetLabel}', Detection={_detectionType?.TypeName ?? "null"}. " +
                $"SpawnPos={context.SpawnPosition}, OrbitPos={_orbitPosition}, TargetTF=({_targetTransform?.name ?? "null"}).");

            StartEntry();
        }

        #endregion

        #region API pública

        #endregion

        #region Entry + OrbitWait (com estratégia)

        private void StartEntry()
        {
            CancelHandlers();
            _state = MinionState.Entry;

            DebugUtility.LogVerbose<DefenseMinionController>(
                $"[Entry] {name} iniciando entrada com estratégia '{entryStrategy?.name ?? "DEFAULT"}' " +
                $"do centro {_planetCenter} para órbita {_orbitPosition} " +
                $"contra alvo '{_targetLabel}' (Role: {_targetRole}, TargetTF: {_targetTransform?.name ?? "null"}).");

            entryHandler?.BeginEntry(
                _planetCenter,
                _orbitPosition,
                _finalScale,
                entryDurationSeconds,
                initialScaleFactor,
                entryStrategy,
                OnEntryCompleted);

            void OnEntryCompleted()
            {
                if (!isActiveAndEnabled)
                {
                    return;
                }

                _state = MinionState.OrbitWait;

                DebugUtility.LogVerbose<DefenseMinionController>(
                    $"[Entry] {name} concluiu entrada. Iniciando idle em órbita antes da perseguição.");

                if (orbitWaitHandler != null && orbitWaitHandler.isActiveAndEnabled)
                {
                    orbitWaitHandler.BeginOrbitWait(orbitIdleDelaySeconds, StartChase);
                }
                else
                {
                    DebugUtility.LogVerbose<DefenseMinionController>(
                        $"[Entry] {name} não possui OrbitWaitHandler ativo; iniciando perseguição imediatamente.");
                    StartChase();
                }
            }
        }

        #endregion

        #region Chase / Estratégia

        private void StartChase()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (chaseHandler == null || !chaseHandler.isActiveAndEnabled)
            {
                DebugUtility.LogVerbose<DefenseMinionController>(
                    $"[Chase] {name} não possui MinionChaseHandler ativo; retornando ao pool para evitar estado inconsistente.");
                ReturnToPool();
                return;
            }

            _state = MinionState.Chase;

            chaseHandler.BeginChase(
                _targetTransform,
                _targetLabel,
                _targetRole,
                chaseSpeed,
                chaseStrategy,
                ResolveTargetTransform,
                HandleChaseStopped);
        }

        #endregion

        #region Pool helpers

        private void HandleChaseStopped(MinionChaseHandler.ChaseStopReason reason)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            DebugUtility.LogVerbose<DefenseMinionController>(
                $"[Chase] {name} sinalizou parada ({reason}). Encerrando atividade para evitar minion ocioso.");

            ReturnToPool();
        }

        private bool ReturnToPool()
        {
            if (_poolable == null)
            {
                _poolable = GetComponent<IPoolable>();
            }

            if (_poolable == null)
            {
                DebugUtility.LogWarning<DefenseMinionController>(
                    $"[Pool] {name} não possui IPoolable; desativando GameObject para evitar estado zombie.",
                    this);
                gameObject.SetActive(false);
                return false;
            }

            if (_poolable is PooledObject pooled && pooled.GetPool != null)
            {
                pooled.GetPool.ReturnObject(_poolable);
                return true;
            }

            _poolable.Deactivate();
            return true;
        }

        private void ResetRuntimeState()
        {
            _state = MinionState.Inactive;
            _targetTransform = null;
            _targetLabel = DefaultTargetLabel;
            _targetRole = DefenseRole.Unknown;
            _detectionType = null;
            _planetCenter = Vector3.zero;
            _orbitPosition = Vector3.zero;
        }

        #endregion


        #region Role helpers

        private DefenseRole ResolveRoleFromLabel(string label)
        {
            if (roleConfig == null)
            {
                return DefenseRole.Unknown;
            }

            return roleConfig.ResolveRole(label);
        }

        private Transform ResolveTargetTransform()
        {
            Transform fallback = null;
            var desiredRole = _targetRole;

            foreach (var behaviour in FindObjectsOfType<MonoBehaviour>(includeInactive: false))
            {
                if (behaviour is not IDefenseRoleProvider roleProvider)
                {
                    continue;
                }

                var resolvedRole = roleProvider.GetDefenseRole();
                if (resolvedRole == DefenseRole.Unknown)
                {
                    continue;
                }

                if (desiredRole == DefenseRole.Unknown)
                {
                    _targetRole = resolvedRole;
                    return behaviour.transform;
                }

                if (resolvedRole == desiredRole)
                {
                    return behaviour.transform;
                }

                fallback ??= behaviour.transform;
            }

            return fallback;
        }

        #endregion

        public void ApplyProfile(DefenseMinionBehaviorProfileSO profileV2)
        {
            if (profileV2 != null)
            {
                // Entrada / órbita
                entryDurationSeconds  = Mathf.Max(0.1f, profileV2.EntryDuration);
                initialScaleFactor    = Mathf.Clamp(profileV2.InitialScaleFactor, 0.05f, 1f);
                orbitIdleDelaySeconds = Mathf.Max(0f, profileV2.OrbitIdleSeconds);

                // Estratégias
                entryStrategy = profileV2.EntryStrategy;
                chaseStrategy = profileV2.ChaseStrategy;

                // Perseguição
                chaseSpeed = Mathf.Max(0.1f, profileV2.ChaseSpeed);

                DebugUtility.LogVerbose<DefenseMinionController>(
                    $"[Profile] {name} aplicou profile v2 '{profileV2.VariantId}': " +
                    $"Entry={entryDurationSeconds:0.00}s, " +
                    $"ScaleFactor={initialScaleFactor:0.00}, " +
                    $"OrbitIdle={orbitIdleDelaySeconds:0.00}s, " +
                    $"ChaseSpeed={chaseSpeed:0.00}, " +
                    $"EntryStrategy={(entryStrategy != null ? entryStrategy.name : "NONE")}, " +
                    $"ChaseStrategy={(chaseStrategy != null ? chaseStrategy.name : "NONE")}",
                    null,this);

                return;
            }

            DebugUtility.LogWarning<DefenseMinionController>(
                $"[Profile] {name} recebeu profile nulo. Mantendo configurações atuais de prefab (uso NÃO recomendado).",
                this);
        }

    }
}
