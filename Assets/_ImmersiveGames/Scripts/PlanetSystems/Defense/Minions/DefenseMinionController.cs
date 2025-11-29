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

        [Header("Entrada visual (similar ao DefenseMinionEntryDebug)")]
        [SerializeField, Min(0.1f)]
        private float entryDurationSeconds = 0.75f;

        [SerializeField, Range(0.05f, 1f)]
        private float initialScaleFactor = 0.2f;

        [Header("Delay antes da perseguição fake")]
        [SerializeField, Min(0f)]
        private float idleDelayBeforeChase = 0.75f;

        [Header("Perseguição fake (similar ao DefenseMinionChaseDebug)")]
        [Tooltip("Duração aproximada da perseguição em segundos (apenas para debug).")]
        [SerializeField, Min(0.1f)]
        private float chaseDurationSeconds = 2f;

        [Tooltip("Velocidade base em unidades por segundo.")]
        [SerializeField, Min(0.1f)]
        private float chaseSpeed = 3f;

        [Tooltip("Curva de aceleração ao longo da perseguição (0 = início, 1 = fim).")]
        [SerializeField]
        private AnimationCurve speedOverLifetime = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        [Tooltip("Se true, sem alvo concreto o minion anda na direção do forward. Se false, fica parado em órbita.")]
        [SerializeField]
        private bool moveForwardWhenNoTarget = false;

        [Header("Alvo opcional (apenas para debug nesta fase)")]
        [Tooltip("Transform do alvo real (Player, Eater, etc). Opcional para debug.")]
        [SerializeField]
        private Transform targetTransform;

        [Tooltip("Rótulo do alvo apenas para logs (ex.: 'Player', 'Eater', 'PlanetDefenseDetector').")]
        [SerializeField]
        private string targetLabel = "AlvoDesconhecido";

        [Tooltip("Se verdadeiro, tenta adquirir automaticamente um alvo por tag ao habilitar.")]
        [SerializeField]
        private bool autoAcquireOnEnable;

        [Tooltip("Tag usada para auto-acquire se autoAcquireOnEnable estiver ativo.")]
        [SerializeField]
        private string autoTargetTag = "Player";

        private Vector3 _finalScale;
        private bool _finalScaleCaptured;

        private MinionState _state = MinionState.Inactive;

        // Tweens (no coroutines)
        private Sequence _entrySequence;
        private Tween _chaseTween;
        private float _chaseElapsed;

        #region Unity

        private void OnEnable()
        {
            KillTweens();
            _state = MinionState.Inactive;

            if (!_finalScaleCaptured)
            {
                _finalScale = transform.localScale;
                _finalScaleCaptured = true;
            }

            if (autoAcquireOnEnable)
            {
                TryAutoAcquireTarget();
            }
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

        #region API pública (chamada pelo WaveRunner)

        /// <summary>
        /// Chamado pelo RealPlanetDefenseWaveRunner logo após o spawn,
        /// com o centro do planeta e a posição de órbita. :contentReference[oaicite:2]{index=2}
        /// Nesta fase: Entry com DOTween → idle → Chase fake, tudo sem corrotina.
        /// </summary>
        public void BeginEntryPhase(Vector3 planetCenter, Vector3 orbitPosition, string label)
        {
            if (!string.IsNullOrWhiteSpace(label))
            {
                targetLabel = label;
            }

            KillTweens();
            StartEntryTween(planetCenter, orbitPosition, targetLabel);
        }

        #endregion

        #region Entry + OrbitWait (DOTween)

        private void StartEntryTween(Vector3 planetCenter, Vector3 orbitPosition, string label)
        {
            _state = MinionState.Entry;

            if (!_finalScaleCaptured)
            {
                _finalScale = transform.localScale;
                _finalScaleCaptured = true;
            }

            var tinyScale = _finalScale * initialScaleFactor;

            // Começa visualmente "dentro" do planeta.
            transform.position = planetCenter;
            transform.localScale = tinyScale;

            DebugUtility.LogVerbose<DefenseMinionController>(
                $"[Entry] {name} surgiu em {planetCenter} (pequeno) e vai orbitar em {orbitPosition} contra {label}.");

            _entrySequence = DOTween.Sequence();

            // Movimento de saída do planeta (centro → órbita) + escala tiny → final,
            // replicando o comportamento do DefenseMinionEntryDebug. :contentReference[oaicite:3]{index=3}
            _entrySequence.Append(
                transform.DOMove(orbitPosition, entryDurationSeconds)
                    .SetEase(Ease.OutQuad));

            _entrySequence.Join(
                transform.DOScale(_finalScale, entryDurationSeconds)
                    .From(tinyScale)
                    .SetEase(Ease.OutQuad));

            // Callback ao terminar a animação de saída.
            _entrySequence.AppendCallback(() => {
                transform.position = orbitPosition;
                transform.localScale = _finalScale;
                _state = MinionState.OrbitWait;

                DebugUtility.LogVerbose<DefenseMinionController>(
                    $"[Entry] {name} concluiu a saída do planeta e está em órbita. " +
                    $"Vai aguardar {idleDelayBeforeChase:0.00}s antes de 'perseguir'.");
            });

            // Idle em órbita antes da perseguição.
            if (idleDelayBeforeChase > 0f)
            {
                _entrySequence.AppendInterval(idleDelayBeforeChase);
            }

            // Ao completar tudo (entry + idle), inicia a perseguição fake.
            _entrySequence.OnComplete(() => {
                _entrySequence = null;

                if (!isActiveAndEnabled)
                    return;

                StartChaseTween();
            });

            _entrySequence.Play();
        }

        #endregion

        #region Chase fake (DOTween, sem coroutine)

        private void StartChaseTween()
        {
            _state = MinionState.Chase;

            if (targetTransform == null && autoAcquireOnEnable)
            {
                TryAutoAcquireTarget();
            }

            if (targetTransform == null)
            {
                DebugUtility.LogVerbose<DefenseMinionController>(
                    $"[Chase] {name} iniciou perseguição SEM alvo concreto. " +
                    $"Somente debug contra rótulo '{targetLabel}'.");
            }
            else
            {
                DebugUtility.LogVerbose<DefenseMinionController>(
                    $"[Chase] {name} iniciou perseguição a '{targetLabel}' em {targetTransform.position}.");
            }

            // Garante que não tem tween anterior de chase rodando
            if (_chaseTween != null && _chaseTween.IsActive())
            {
                _chaseTween.Kill();
                _chaseTween = null;
            }

            _chaseElapsed = 0f;

            // Tween temporal que substitui a coroutine de perseguição
            _chaseTween = DOVirtual.Float(
                0f,
                chaseDurationSeconds,
                chaseDurationSeconds,
                value => {
                    float deltaTime = value - _chaseElapsed;
                    _chaseElapsed = value;

                    float t = Mathf.Clamp01(_chaseElapsed / chaseDurationSeconds);

                    float speedFactor = speedOverLifetime != null
                        ? Mathf.Max(0f, speedOverLifetime.Evaluate(t))
                        : 1f;

                    float currentSpeed = chaseSpeed * speedFactor;

                    if (targetTransform != null)
                    {
                        Vector3 direction = (targetTransform.position - transform.position);
                        if (direction.sqrMagnitude > 0.0001f)
                        {
                            direction.Normalize();
                            transform.position += direction * (currentSpeed * deltaTime);

                            // Olha gradualmente para o alvo
                            transform.forward = Vector3.Lerp(transform.forward, direction, 0.2f);
                        }
                    }
                    else if (moveForwardWhenNoTarget)
                    {
                        // Agora só anda para frente se a flag estiver ligada
                        transform.position += transform.forward * (currentSpeed * deltaTime);
                    }
                    // Se não tiver alvo e moveForwardWhenNoTarget == false → fica parado
                });

            _chaseTween.OnComplete(() => {
                DebugUtility.LogVerbose<DefenseMinionController>(
                    targetTransform != null
                        ? $"[Chase] {name} concluiu perseguição fake a '{targetLabel}'. Posição final: {transform.position}."
                        : $"[Chase] {name} concluiu perseguição fake sem alvo concreto. Posição final: {transform.position}.");

                _state = MinionState.Inactive;
                _chaseTween = null;
            });
        }


        private void KillChaseTween()
        {
            if (_chaseTween != null && _chaseTween.IsActive())
            {
                _chaseTween.Kill();
                _chaseTween = null;
            }
        }

        private void TryAutoAcquireTarget()
        {
            if (targetTransform != null)
                return;

            if (string.IsNullOrWhiteSpace(autoTargetTag))
                return;

            var candidate = GameObject.FindWithTag(autoTargetTag);
            if (candidate != null)
            {
                targetTransform = candidate.transform;

                DebugUtility.LogVerbose<DefenseMinionController>(
                    $"[Chase] {name} adquiriu alvo automaticamente por tag '{autoTargetTag}': {candidate.name}");
            }
        }

        #endregion
    }
}