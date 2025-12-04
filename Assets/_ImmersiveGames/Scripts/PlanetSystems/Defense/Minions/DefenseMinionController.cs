using System;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
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
        private string _targetLabel = "Unknown";
        private DefenseRole _targetRole = DefenseRole.Unknown;
        private bool _profileApplied;

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
            _state = MinionState.Inactive;
        }

        private void OnDisable()
        {
            CancelHandlers();
            _state = MinionState.Inactive;
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

        public void SetTarget(Transform target, string label, DefenseRole targetRole)
        {
            _targetTransform = target;
            if (!string.IsNullOrWhiteSpace(label))
            {
                _targetLabel = label;
            }

            _targetRole = targetRole != DefenseRole.Unknown
                ? targetRole
                : ResolveRoleFromLabel(_targetLabel);

            DebugUtility.LogVerbose<DefenseMinionController>(
                $"[Target] {name} recebeu alvo explícito: Transform=({_targetTransform?.name ?? "null"}), " +
                $"Label='{_targetLabel}', Role={_targetRole}.");
        }

        #endregion

        #region API pública

        public void BeginEntryPhase(Vector3 planetCenter, Vector3 orbitPosition, string targetLabel)
        {
            EnsureHandlers();

            if (!string.IsNullOrWhiteSpace(targetLabel))
            {
                _targetLabel = targetLabel;
            }

            if (_targetRole == DefenseRole.Unknown)
            {
                _targetRole = ResolveRoleFromLabel(_targetLabel);
            }

            BeginEntryPhase(planetCenter, orbitPosition);
        }

        public void BeginEntryPhase(Vector3 planetCenter, Vector3 orbitPosition)
        {
            _planetCenter = planetCenter;
            _orbitPosition = orbitPosition;

            StartEntry();
        }

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
                _state = MinionState.OrbitWait;
                DebugUtility.LogVerbose<DefenseMinionController>(
                    $"[Chase] {name} não iniciou perseguição porque o MinionChaseHandler está desabilitado ou ausente.");
                return;
            }

            _state = MinionState.Chase;

            // ❌ Não tentamos mais descobrir alvo por tag.
            // ✅ Só usamos o alvo que veio explicitamente do sistema de defesa
            //    (SetTarget) + roleConfig para interpretar o label.
            chaseHandler.BeginChase(
                _targetTransform,
                _targetLabel,
                _targetRole,
                chaseSpeed,
                chaseStrategy,
                () => _state = MinionState.Inactive);
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

                _profileApplied = true;

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

            _profileApplied = false;

            DebugUtility.LogWarning<DefenseMinionController>(
                $"[Profile] {name} recebeu profile nulo. Mantendo configurações atuais de prefab (uso NÃO recomendado).",
                this);
        }

    }
}
