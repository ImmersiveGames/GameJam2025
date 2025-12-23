using System;
using System.Reflection;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    /// <summary>
    /// Runner agregador para executar sequencialmente os QAs de infraestrutura do NewScripts.
    /// Suporta execução automática em Start ou manual via ContextMenu.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    [DisallowMultipleComponent]
    public sealed class NewScriptsInfraSmokeRunner : MonoBehaviour
    {
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool stopOnFirstFail = false;
        [SerializeField] private MonoBehaviour debugLogTester;
        [SerializeField] private MonoBehaviour diTester;
        [SerializeField] private MonoBehaviour fsmTester;
        [SerializeField] private MonoBehaviour eventBusTester;
        [SerializeField] private MonoBehaviour gameLoopEventBridgeTester;
        [SerializeField] private MonoBehaviour filteredEventBusTester;
        [SerializeField] private MonoBehaviour legacySceneFlowBridgeTester;
        [SerializeField] private bool verbose = true;

        private int _passes;
        private int _fails;

        private void Start()
        {
            if (runOnStart)
            {
                RunAll();
            }
        }

        [ContextMenu("QA/Infra/Run All")]
        public void RunAll()
        {
            _passes = 0;
            _fails = 0;

            if (verbose)
            {
                DebugUtility.Log(typeof(NewScriptsInfraSmokeRunner),
                    "[QA][Infra] Iniciando execução dos testes de infraestrutura.");
            }

            ExecuteTester(debugLogTester, "DebugLogSettingsQATester");
            if (stopOnFirstFail && _fails > 0)
            {
                DebugUtility.LogWarning(typeof(NewScriptsInfraSmokeRunner),
                    "[QA][Infra] stopOnFirstFail ativo; execução interrompida após falha.");
                Complete();
                return;
            }

            ExecuteTester(diTester, "DependencyDISmokeQATester");
            if (stopOnFirstFail && _fails > 0)
            {
                DebugUtility.LogWarning(typeof(NewScriptsInfraSmokeRunner),
                    "[QA][Infra] stopOnFirstFail ativo; execução interrompida após falha.");
                Complete();
                return;
            }

            ExecuteTester(fsmTester, "FsmPredicateQATester");
            if (stopOnFirstFail && _fails > 0)
            {
                DebugUtility.LogWarning(typeof(NewScriptsInfraSmokeRunner),
                    "[QA][Infra] stopOnFirstFail ativo; execução interrompida após falha.");
                Complete();
                return;
            }

            ExecuteTester(eventBusTester, "EventBusSmokeQATester");
            if (stopOnFirstFail && _fails > 0)
            {
                DebugUtility.LogWarning(typeof(NewScriptsInfraSmokeRunner),
                    "[QA][Infra] stopOnFirstFail ativo; execução interrompida após falha.");
                Complete();
                return;
            }

            gameLoopEventBridgeTester = gameLoopEventBridgeTester
                                        ?? GetComponent<GameLoopEventInputBridgeSmokeQATester>()
                                        ?? gameObject.AddComponent<GameLoopEventInputBridgeSmokeQATester>();

            ExecuteTester(gameLoopEventBridgeTester, "GameLoopEventInputBridgeSmokeQATester");
            if (stopOnFirstFail && _fails > 0)
            {
                DebugUtility.LogWarning(typeof(NewScriptsInfraSmokeRunner),
                    "[QA][Infra] stopOnFirstFail ativo; execução interrompida após falha.");
                Complete();
                return;
            }

            ExecuteTester(filteredEventBusTester, "FilteredEventBusSmokeQATester");

            legacySceneFlowBridgeTester = legacySceneFlowBridgeTester
                                          ?? GetComponent<LegacySceneFlowBridgeSmokeQATester>()
                                          ?? gameObject.AddComponent<LegacySceneFlowBridgeSmokeQATester>();

            ExecuteTester(legacySceneFlowBridgeTester, "LegacySceneFlowBridgeSmokeQATester");

            Complete();
        }

        private void ExecuteTester(MonoBehaviour tester, string testerLabel)
        {
            if (tester == null)
            {
                RegisterFail($"{testerLabel} reference is null.");
                return;
            }

            MethodInfo runMethod = tester.GetType().GetMethod("Run",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (runMethod == null || runMethod.GetParameters().Length != 0)
            {
                RegisterFail($"{testerLabel} does not expose parameterless Run().");
                return;
            }

            try
            {
                runMethod.Invoke(tester, null);
                _passes++;

                if (verbose)
                {
                    DebugUtility.Log(typeof(NewScriptsInfraSmokeRunner),
                        $"[QA][Infra] {testerLabel} PASS.", DebugUtility.Colors.Success);
                }
            }
            catch (Exception ex)
            {
                Exception rootException = ex is TargetInvocationException tie && tie.InnerException != null
                    ? tie.InnerException
                    : ex;

                RegisterFail($"{testerLabel} FAIL - {rootException.GetType().Name}: {rootException.Message}");
            }
        }

        private void RegisterFail(string message)
        {
            _fails++;
            DebugUtility.LogError(typeof(NewScriptsInfraSmokeRunner), $"[QA][Infra] {message}");
        }

        private void Complete()
        {
            string status = _fails == 0 ? "PASS" : "FAIL";
            DebugUtility.Log(typeof(NewScriptsInfraSmokeRunner),
                $"[QA][Infra] Resultado => Passes: {_passes} | Fails: {_fails} | Status={status}",
                _fails == 0 ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);
            DebugUtility.Log(typeof(NewScriptsInfraSmokeRunner), "[QA][Infra] QA complete.");
        }
    }
}
