using System.Collections;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Versão inicial (Fase 1) do controlador real de minions de defesa.
    ///
    /// Objetivo nesta fase:
    /// - Reproduzir o fluxo básico:
    ///   Entry (nasce pequeno no centro) → OrbitWait → Chase fake
    /// - Sem estratégias ainda, sem DefenseRole, sem DOTween.
    /// - Apenas corrotinas e logs para validar arquitetura e integração futura.
    ///
    /// Futuro:
    /// - Substituir pelos sistemas baseados em DOTween e estratégias (Entry/Chase).
    /// - Integrar com DefenseRoleConfig para entender Player/Eater.
    /// - Integrar com sistema de dano.
    /// </summary>
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

        private Coroutine _entryRoutine;
        private Coroutine _chaseRoutine;

        private MinionState _state = MinionState.Inactive;

        #region Unity

        private void OnEnable()
        {
            StopAllRunningCoroutines();
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
            StopAllRunningCoroutines();
            _state = MinionState.Inactive;
        }

        private void StopAllRunningCoroutines()
        {
            if (_entryRoutine != null)
            {
                StopCoroutine(_entryRoutine);
                _entryRoutine = null;
            }

            if (_chaseRoutine != null)
            {
                StopCoroutine(_chaseRoutine);
                _chaseRoutine = null;
            }
        }

        #endregion

        #region API pública (será chamada pelo WaveRunner na Fase 2)

        /// <summary>
        /// Chamado logo após o spawn, com o centro do planeta e a posição de órbita.
        /// Nesta fase, ele apenas inicia a animação de Entry e depois um chase fake.
        /// </summary>
        public void BeginEntryPhase(Vector3 planetCenter, Vector3 orbitPosition, string label)
        {
            if (!string.IsNullOrWhiteSpace(label))
            {
                targetLabel = label;
            }

            StopAllRunningCoroutines();

            _entryRoutine = StartCoroutine(EntryRoutine(planetCenter, orbitPosition, targetLabel));
        }

        #endregion

        #region Entry + OrbitWait

        private IEnumerator EntryRoutine(Vector3 planetCenter, Vector3 orbitPosition, string label)
        {
            _state = MinionState.Entry;

            var tinyScale = _finalScale * initialScaleFactor;

            // Começa visualmente "dentro" do planeta.
            transform.position = planetCenter;
            transform.localScale = tinyScale;

            DebugUtility.LogVerbose<DefenseMinionController>(
                $"[Entry] {name} surgiu em {planetCenter} (pequeno) e vai orbitar em {orbitPosition} contra {label}.");

            float elapsed = 0f;

            // Animação simples de saída do planeta (posição + escala) – igual ao EntryDebug. :contentReference[oaicite:2]{index=2}
            while (elapsed < entryDurationSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / entryDurationSeconds);

                transform.position = Vector3.Lerp(planetCenter, orbitPosition, t);
                transform.localScale = Vector3.Lerp(tinyScale, _finalScale, t);

                yield return null;
            }

            // Garante posição e escala finais estáveis.
            transform.position = orbitPosition;
            transform.localScale = _finalScale;

            _state = MinionState.OrbitWait;

            DebugUtility.LogVerbose<DefenseMinionController>(
                $"[Entry] {name} concluiu a saída do planeta e está em órbita. " +
                $"Vai aguardar {idleDelayBeforeChase:0.00}s antes de 'perseguir'.");

            if (idleDelayBeforeChase > 0f)
            {
                yield return new WaitForSeconds(idleDelayBeforeChase);
            }

            // Ao fim do idle, inicia a perseguição fake.
            StartChase();
        }

        #endregion

        #region Chase fake

        private void StartChase()
        {
            if (_chaseRoutine != null)
            {
                StopCoroutine(_chaseRoutine);
            }

            _chaseRoutine = StartCoroutine(ChaseRoutine());
        }

        private IEnumerator ChaseRoutine()
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

            float elapsed = 0f;

            // Movimento fake de perseguição, baseado no DefenseMinionChaseDebug. :contentReference[oaicite:3]{index=3}
            while (elapsed < chaseDurationSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / chaseDurationSeconds);

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
                        transform.position += direction * (currentSpeed * Time.deltaTime);

                        // Olhar gradualmente na direção do alvo.
                        transform.forward = Vector3.Lerp(transform.forward, direction, 0.2f);
                    }
                }
                else
                {
                    // Sem alvo real: move para frente, só para dar feedback visual.
                    transform.position += transform.forward * (currentSpeed * Time.deltaTime);
                }

                yield return null;
            }

            DebugUtility.LogVerbose<DefenseMinionController>(
                targetTransform != null
                    ? $"[Chase] {name} concluiu perseguição fake a '{targetLabel}'. Posição final: {transform.position}."
                    : $"[Chase] {name} concluiu perseguição fake sem alvo concreto. Posição final: {transform.position}.");

            _state = MinionState.Inactive;
            _chaseRoutine = null;
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
