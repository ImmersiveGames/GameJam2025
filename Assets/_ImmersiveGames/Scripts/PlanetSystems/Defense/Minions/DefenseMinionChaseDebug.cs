using System.Collections;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Controla a fase de "perseguição" dos minions de defesa.
    ///
    /// Versão FAKE para debug:
    /// - é iniciado após a fase de entrada (DefenseMinionEntryDebug)
    /// - opcionalmente recebe um alvo (Transform) e um rótulo (Player/Eater/etc)
    /// - faz um movimento simples em direção ao alvo
    /// - emite logs no início, durante e no fim da perseguição
    ///
    /// Futuro:
    /// - esta lógica de movimento pode ser trocada por DOTween
    /// - o BeginChase pode ser chamado direto pelo DefenseMinionEntryDebug
    ///   depois do idleDelayBeforeChase.
    /// </summary>
    [DebugLevel(level: DebugLevel.Verbose)]
    public sealed class DefenseMinionChaseDebug : MonoBehaviour
    {
        [Header("Perseguição fake (debug)")]
        [Tooltip("Duração aproximada da perseguição em segundos (apenas para debug).")]
        [SerializeField, Min(0.1f)]
        private float chaseDurationSeconds = 2f;

        [Tooltip("Velocidade base em unidades por segundo.")]
        [SerializeField, Min(0.1f)]
        private float chaseSpeed = 3f;

        [Tooltip("Curva de aceleração ao longo da perseguição (0 = início, 1 = fim).")]
        [SerializeField]
        private AnimationCurve speedOverLifetime = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        [Header("Configuração do alvo (opcional)")]
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

        private bool _runningChase;

        private void OnEnable()
        {
            // Sempre que o objeto volta do pool, limpamos o estado interno.
            _runningChase = false;

            if (autoAcquireOnEnable)
            {
                TryAutoAcquireTarget();
            }
        }

        private void OnDisable()
        {
            // Importante para objetos em pool: não deixar coroutines penduradas.
            if (_runningChase)
            {
                StopAllCoroutines();
                _runningChase = false;
            }
        }

        /// <summary>
        /// Inicia a perseguição fake SEM alvo concreto, apenas com logs.
        /// Útil se você ainda não tem um Transform do Player/Eater,
        /// mas quer ver o fluxo de debug funcionando.
        /// </summary>
        public void BeginChase(string label)
        {
            BeginChase(null, label);
        }

        /// <summary>
        /// Inicia a perseguição fake para um alvo específico.
        /// Pode ser chamado pelo DefenseMinionEntryDebug após o idleDelayBeforeChase.
        /// </summary>
        private void BeginChase(Transform target, string label)
        {
            if (_runningChase)
                return;

            targetTransform = target;
            if (!string.IsNullOrWhiteSpace(label))
            {
                targetLabel = label;
            }

            StopAllCoroutines();
            StartCoroutine(ChaseRoutine());
        }

        /// <summary>
        /// Tenta encontrar um alvo automaticamente via tag (por exemplo. Player).
        /// Só é chamado se autoAcquireOnEnable estiver true.
        /// </summary>
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
                if (string.IsNullOrWhiteSpace(targetLabel))
                {
                    targetLabel = candidate.name;
                }

                DebugUtility.LogVerbose<DefenseMinionChaseDebug>(
                    $"[Chase] {name} adquiriu alvo automaticamente por tag '{autoTargetTag}': {candidate.name}");
            }
        }

        private IEnumerator ChaseRoutine()
        {
            _runningChase = true;

            if (targetTransform == null)
            {
                DebugUtility.LogVerbose<DefenseMinionChaseDebug>(
                    $"[Chase] {name} iniciou perseguição SEM alvo concreto. " +
                    $"Somente debug contra rótulo '{targetLabel}'.");
            }
            else
            {
                DebugUtility.LogVerbose<DefenseMinionChaseDebug>(
                    $"[Chase] {name} iniciou perseguição a '{targetLabel}' em {targetTransform.position}.");
            }

            float elapsed = 0f;

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
                    // Movimento simples em direção ao alvo.
                    Vector3 direction = (targetTransform.position - transform.position);
                    if (direction.sqrMagnitude > 0.0001f)
                    {
                        direction.Normalize();
                        transform.position += direction * (currentSpeed * Time.deltaTime);

                        // Opcional: olhar na direção do alvo.
                        transform.forward = Vector3.Lerp(transform.forward, direction, 0.2f);
                    }
                }
                else
                {
                    // Sem alvo real: só emula movimento "para frente" em relação à direção inicial.
                    // Isso é apenas para dar sensação de que algo está acontecendo.
                    transform.position += transform.forward * (currentSpeed * Time.deltaTime);
                }

                yield return null;
            }

            // Fim da perseguição fake.
            DebugUtility.LogVerbose<DefenseMinionChaseDebug>(
                targetTransform != null ? $"[Chase] {name} concluiu perseguição fake a '{targetLabel}'. Posição final: {transform.position}." : $"[Chase] {name} concluiu perseguição fake sem alvo concreto. Posição final: {transform.position}.");

            _runningChase = false;
        }
    }
}
