using System;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using DG.Tweening;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    [DebugLevel(level: DebugLevel.Verbose)]
    public sealed class DefenseMinionController : MonoBehaviour
    {
        private enum MinionState
        {
            Inactive,
            Entry,
            OrbitWait,
            Chase
        }

        [Header("Entrada visual (controlado por Profile)")]
        [HideInInspector]
        [SerializeField, Min(0.1f)]
        private float entryDurationSeconds = 0.75f;

        [HideInInspector]
        [SerializeField, Range(0.05f, 1f)]
        private float initialScaleFactor = 0.2f;

        [Tooltip("Estratégia de entrada (como sai do planeta e chega na órbita).")]
        [SerializeField]
        private MinionEntryStrategySo entryStrategy;

        [Header("Orbit / Idle antes da perseguição (controlado por Profile)")]
        [HideInInspector]
        [SerializeField, Min(0f)]
        private float orbitIdleDelaySeconds = 0.75f;

        [Header("Perseguição (controlado por Profile)")]
        [HideInInspector]
        [SerializeField, Min(0.1f)]
        private float chaseSpeed = 3f;

        [SerializeField]
        private MinionChaseStrategySo chaseStrategy;

        [Header("Resolução de alvo / Role (fallback)")]
        [SerializeField]
        private DefenseRoleConfig roleConfig;

        private Vector3 _finalScale;
        private bool _finalScaleCaptured;

        private MinionState _state = MinionState.Inactive;

        private Sequence _entrySequence;
        private Tween _chaseTween;

        private Vector3 _planetCenter;
        private Vector3 _orbitPosition;

        private Transform _targetTransform;
        private string _targetLabel = "Unknown";
        private DefenseRole _targetRole = DefenseRole.Unknown;
        private bool _profileApplied;

        #region Unity lifecycle

        private void OnEnable()
        {
            KillTweens();

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
            KillTweens();
            _state = MinionState.Inactive;
        }

        private void KillTweens()
        {
            if (_entrySequence != null && _entrySequence.IsActive())
            {
                _entrySequence.Kill();
                _entrySequence = null;
            }

            if (_chaseTween != null && _chaseTween.IsActive())
            {
                _chaseTween.Kill();
                _chaseTween = null;
            }
        }

        #endregion

        #region Configuração do alvo

        public void ConfigureTarget(Transform target, string label, DefenseRole role)
        {
            _targetTransform = target;
            if (!string.IsNullOrWhiteSpace(label))
            {
                _targetLabel = label;
            }

            _targetRole = role != DefenseRole.Unknown
                ? role
                : ResolveRoleFromLabel(_targetLabel);

            DebugUtility.LogVerbose<DefenseMinionController>(
                $"[Target] {name} recebeu alvo explícito: Transform=({_targetTransform?.name ?? "null"}), " +
                $"Label='{_targetLabel}', Role={_targetRole}.");
        }

        #endregion

        #region API pública

        public void BeginEntryPhase(Vector3 planetCenter, Vector3 orbitPosition, string targetLabel)
        {
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
            KillTweens();
            _state = MinionState.Entry;
            
            if (!_profileApplied)
            {
                DebugUtility.LogWarning<DefenseMinionController>(
                    $"[Entry] {name} iniciando entrada SEM Profile aplicado. " +
                    $"Valores de comportamento estão vindo do prefab (estado não ideal). " +
                    $"Verifique se DefensesMinionData.DefaultProfile está configurado.",
                    this);
            }

            DebugUtility.LogVerbose<DefenseMinionController>(
                $"[Entry] {name} iniciando entrada com estratégia '{entryStrategy?.name ?? "DEFAULT"}' " +
                $"do centro {_planetCenter} para órbita {_orbitPosition} " +
                $"contra alvo '{_targetLabel}' (Role: {_targetRole}, TargetTF: {_targetTransform?.name ?? "null"}).");

            void OnEntryCompleted()
            {
                if (!isActiveAndEnabled)
                {
                    return;
                }

                // Garante estado e posição finais estáveis
                transform.position = _orbitPosition;
                transform.localScale = _finalScale;
                _state = MinionState.OrbitWait;

                DebugUtility.LogVerbose<DefenseMinionController>(
                    $"[Entry] {name} concluiu entrada+idle em órbita. Iniciando perseguição.");

                StartChase();
            }

            if (entryStrategy != null)
            {
                _entrySequence = entryStrategy.BuildEntrySequence(
                    transform,
                    _planetCenter,
                    _orbitPosition,
                    _finalScale,
                    entryDurationSeconds,
                    initialScaleFactor,
                    orbitIdleDelaySeconds,
                    OnEntryCompleted);

                if (_entrySequence == null)
                {
                    DebugUtility.LogWarning<DefenseMinionController>(
                        $"[Entry] Estratégia '{entryStrategy.name}' retornou Sequence nula. Usando fallback DEFAULT.");
                    BuildDefaultEntrySequence(OnEntryCompleted);
                }
            }
            else
            {
                BuildDefaultEntrySequence(OnEntryCompleted);
            }
        }

        private void BuildDefaultEntrySequence(Action onCompleted)
        {
            Vector3 tinyScale = _finalScaleCaptured
                ? _finalScale * initialScaleFactor
                : transform.localScale * initialScaleFactor;

            transform.position = _planetCenter;
            transform.localScale = tinyScale;

            _entrySequence = DOTween.Sequence();

            _entrySequence.Append(
                transform.DOMove(_orbitPosition, entryDurationSeconds)
                         .SetEase(Ease.OutQuad));

            _entrySequence.Join(
                transform.DOScale(_finalScale, entryDurationSeconds)
                         .From(tinyScale));

            if (orbitIdleDelaySeconds > 0f)
            {
                _entrySequence.AppendInterval(orbitIdleDelaySeconds);
            }

            _entrySequence.OnComplete(() => { onCompleted?.Invoke(); });
        }

        #endregion

                #region Chase / Estratégia

        private void StartChase()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            _state = MinionState.Chase;

            // ❌ Não tentamos mais descobrir alvo por tag.
            // ✅ Só usamos o alvo que veio explicitamente do sistema de defesa
            //    (ConfigureTarget) + roleConfig para interpretar o label.
            if (_targetTransform == null)
            {
                DebugUtility.LogVerbose<DefenseMinionController>(
                    $"[Chase] {name} iniciou perseguição SEM alvo concreto (_targetTransform nulo). " +
                    $"Role: {_targetRole} | Label: '{_targetLabel}'. " +
                    $"Nenhuma busca por tag será feita (apenas alvo explícito + RoleConfig são usados).");

                // Mantemos um movimento fake simples apenas para deixar
                // visualmente claro que o minion "faria algo" nesse caso.
                _chaseTween = transform.DOMove(transform.position + transform.forward * 5f, 2f)
                                       .SetEase(Ease.Linear)
                                       .OnComplete(() =>
                                       {
                                           DebugUtility.LogVerbose<DefenseMinionController>(
                                               $"[Chase] {name} concluiu perseguição fake sem alvo concreto.");
                                           _state = MinionState.Inactive;
                                           _chaseTween = null;
                                       });

                return;
            }

            DebugUtility.LogVerbose<DefenseMinionController>(
                $"[Chase] {name} iniciou perseguição real a '{_targetLabel}' " +
                $"(Role: {_targetRole}) em posição {_targetTransform.position}.");

            if (chaseStrategy != null)
            {
                _chaseTween = chaseStrategy.CreateChaseTween(
                    transform,
                    _targetTransform,
                    chaseSpeed,
                    _targetLabel);
            }
            else
            {
                float distance = Vector3.Distance(transform.position, _targetTransform.position);
                float duration = distance / Mathf.Max(0.01f, chaseSpeed);

                _chaseTween = transform.DOMove(_targetTransform.position, duration)
                                       .SetEase(Ease.Linear);
            }

            if (_chaseTween != null)
            {
                _chaseTween.OnUpdate(() =>
                {
                    var dir = (_targetTransform.position - transform.position);
                    if (dir.sqrMagnitude > 0.0001f)
                    {
                        dir.Normalize();
                        transform.forward = Vector3.Lerp(transform.forward, dir, 0.2f);
                    }
                });

                _chaseTween.OnComplete(() =>
                {
                    DebugUtility.LogVerbose<DefenseMinionController>(
                        $"[Chase] {name} concluiu Tween de perseguição a '{_targetLabel}'. " +
                        $"Posição final: {transform.position}.");

                    _state = MinionState.Inactive;
                    _chaseTween = null;
                });
            }
            else
            {
                DebugUtility.LogVerbose<DefenseMinionController>(
                    $"[Chase] {name} não conseguiu criar Tween de perseguição para alvo '{_targetLabel}'.");
                _state = MinionState.Inactive;
            }
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
        public void ApplyProfile(DefenseMinionBehaviorProfile profile)
        {
            if (profile == null)
            {
                DebugUtility.LogWarning<DefenseMinionController>(
                    $"[Profile] {name} recebeu profile nulo. Mantendo configurações atuais de prefab (uso NÃO recomendado).",
                    this);
                _profileApplied = false;
                return;
            }

            // Entrada / órbita
            entryDurationSeconds  = Mathf.Max(0.1f, profile.EntryDuration);
            initialScaleFactor    = Mathf.Clamp(profile.InitialScaleFactor, 0.05f, 1f);
            orbitIdleDelaySeconds = Mathf.Max(0f, profile.OrbitIdleSeconds);

            // Perseguição
            chaseSpeed = Mathf.Max(0.1f, profile.ChaseSpeed);

            _profileApplied = true;

            DebugUtility.LogVerbose<DefenseMinionController>(
                $"[Profile] {name} aplicou profile '{profile.VariantId}': " +
                $"Entry={entryDurationSeconds:0.00}s, " +
                $"ScaleFactor={initialScaleFactor:0.00}, " +
                $"OrbitIdle={orbitIdleDelaySeconds:0.00}s, " +
                $"ChaseSpeed={chaseSpeed:0.00}.",
                null,this);
        }

    }
}
