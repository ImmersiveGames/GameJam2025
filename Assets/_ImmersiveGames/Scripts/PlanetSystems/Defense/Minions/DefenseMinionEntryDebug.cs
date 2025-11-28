using System.Collections;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Controla a "fase de entrada" dos minions de defesa.
    /// Fluxo:
    /// - nasce pequeno no centro do planeta
    /// - se move até a posição de órbita
    /// - escala até o tamanho final
    /// - espera alguns segundos
    /// - ativa os behaviours de movimento/AI configurados
    ///
    /// Mais para frente a parte de movimento pode ser trocada por DOTween,
    /// mas a orquestração de "entrada → habilitar AI" já fica pronta.
    /// </summary>
    [DebugLevel(level: DebugLevel.Verbose)]
    public sealed class DefenseMinionEntryDebug : MonoBehaviour
    {
        [Header("Entrada visual (fake)")]
        [SerializeField, Min(0.1f)]
        private float entryDurationSeconds = 0.75f;

        [SerializeField, Range(0.05f, 1f)]
        private float initialScaleFactor = 0.2f;

        [Header("Delay antes da perseguição real")]
        [SerializeField, Min(0f)]
        private float idleDelayBeforeChase = 0.75f;

        [Header("Behaviours a serem ativados após a entrada")]
        [Tooltip("Scripts de movimento/AI que só devem ligar depois da animação de saída do planeta.")]
        [SerializeField]
        private MonoBehaviour[] behavioursToEnableOnChase;

        [Tooltip("Se verdadeiro, desativa esses behaviours automaticamente em OnEnable, garantindo que comecem desligados.")]
        [SerializeField]
        private bool autoDisableOnEnable = true;

        private Vector3 _finalScale;
        private bool _finalScaleCaptured;
        private bool _runningEntry;

        private void OnEnable()
        {
            // Toda vez que o objeto volta do pool, preparamos para uma nova entrada.
            _runningEntry = false;

            if (autoDisableOnEnable && behavioursToEnableOnChase != null)
            {
                foreach (var behaviour in behavioursToEnableOnChase)
                {
                    if (behaviour != null)
                        behaviour.enabled = false;
                }
            }
        }

        /// <summary>
        /// Chamado pelo RealPlanetDefenseWaveRunner assim que o minion é spawnado.
        /// </summary>
        public void BeginEntryPhase(Vector3 planetCenter, Vector3 orbitPosition, string targetLabel)
        {
            if (_runningEntry)
                return;

            if (!_finalScaleCaptured)
            {
                _finalScale = transform.localScale;
                _finalScaleCaptured = true;
            }

            StopAllCoroutines();
            StartCoroutine(EntryRoutine(planetCenter, orbitPosition, targetLabel));
        }

        private IEnumerator EntryRoutine(Vector3 planetCenter, Vector3 orbitPosition, string targetLabel)
        {
            _runningEntry = true;

            var tinyScale = _finalScale * initialScaleFactor;

            // Começa visualmente "dentro" do planeta.
            transform.position = planetCenter;
            transform.localScale = tinyScale;

            DebugUtility.LogVerbose<DefenseMinionEntryDebug>(
                $"[Entry] {name} surgiu em {planetCenter} (pequeno) e vai orbitar em {orbitPosition} contra {targetLabel}.");

            float elapsed = 0f;

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

            DebugUtility.LogVerbose<DefenseMinionEntryDebug>(
                $"[Entry] {name} concluiu a saída do planeta e está em órbita. Vai aguardar {idleDelayBeforeChase:0.00}s antes de 'perseguir'.");

            if (idleDelayBeforeChase > 0f)
                yield return new WaitForSeconds(idleDelayBeforeChase);

            // Ativa os behaviours de movimento/AI configurados.
            if (behavioursToEnableOnChase is { Length: > 0 })
            {
                foreach (var behaviour in behavioursToEnableOnChase)
                {
                    if (behaviour == null) continue;
                    behaviour.enabled = true;
                    DebugUtility.LogVerbose<DefenseMinionEntryDebug>(
                        $"[Entry] {name} ativou behaviour '{behaviour.GetType().Name}' para iniciar perseguição ao alvo ({targetLabel}).");
                }
            }
            else
            {
                // Fallback: apenas log, como estava antes.
                DebugUtility.LogVerbose<DefenseMinionEntryDebug>(
                    $"[Entry] {name} agora iniciaria a perseguição ao alvo ({targetLabel}). (Somente debug por enquanto)");
            }

            _runningEntry = false;
        }
    }
}
