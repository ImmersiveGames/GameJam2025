using System;
using System.Reflection;
using _ImmersiveGames.NewScripts.Bridges.LegacySceneFlow.QA;
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
        private const string SceneFlowLogTag = "[SceneFlowTest][Runner]";

        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool stopOnFirstFail = false;
        [SerializeField] private bool sceneFlowOnly = false;
        [SerializeField] private MonoBehaviour debugLogTester;
        [SerializeField] private MonoBehaviour diTester;
        [SerializeField] private MonoBehaviour fsmTester;
        [SerializeField] private MonoBehaviour eventBusTester;
        [SerializeField] private MonoBehaviour gameLoopEventBridgeTester;
        [SerializeField] private MonoBehaviour filteredEventBusTester;
        [SerializeField] private MonoBehaviour sceneTransitionServiceTester;
        [SerializeField] private bool runSceneTransitionServiceTester = true;
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

            if (sceneFlowOnly)
            {
                RunSceneFlowOnly();
                return;
            }

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

            if (runSceneTransitionServiceTester)
            {
                sceneTransitionServiceTester = sceneTransitionServiceTester
                                                ?? GetComponent<SceneTransitionServiceSmokeQATester>()
                                                ?? gameObject.AddComponent<SceneTransitionServiceSmokeQATester>();

                ExecuteTester(sceneTransitionServiceTester, "SceneTransitionServiceSmokeQATester");
                if (stopOnFirstFail && _fails > 0)
                {
                    DebugUtility.LogWarning(typeof(NewScriptsInfraSmokeRunner),
                        "[QA][Infra] stopOnFirstFail ativo; execução interrompida após falha.");
                    Complete();
                    return;
                }
            }

            legacySceneFlowBridgeTester = legacySceneFlowBridgeTester
                                          ?? GetComponent<LegacySceneFlowBridgeSmokeQATester>()
                                          ?? gameObject.AddComponent<LegacySceneFlowBridgeSmokeQATester>();

            ExecuteTester(legacySceneFlowBridgeTester, "LegacySceneFlowBridgeSmokeQATester");

            Complete();
        }

        /// <summary>
        /// Configuração específica para habilitar apenas os testers de Scene Flow (nativo + bridge).
        /// Mantém a execução determinística para cenários de CI/PlayMode.
        /// </summary>
        public void ConfigureSceneFlowOnly(
            MonoBehaviour sceneTransitionTester,
            MonoBehaviour legacyBridgeTester,
            bool enableVerbose = true)
        {
            runOnStart = false;
            stopOnFirstFail = true;
            sceneFlowOnly = true;
            verbose = enableVerbose;
            runSceneTransitionServiceTester = true;
            sceneTransitionServiceTester = sceneTransitionTester;
            legacySceneFlowBridgeTester = legacyBridgeTester;
        }

        public bool LastRunHadFailures => _fails > 0;
        public int LastRunPasses => _passes;
        public int LastRunFails => _fails;

        private void RunSceneFlowOnly()
        {
            DebugUtility.Log(typeof(NewScriptsInfraSmokeRunner),
                $"{SceneFlowLogTag} Iniciando execução exclusiva de Scene Flow.");

            sceneTransitionServiceTester = sceneTransitionServiceTester
                                            ?? GetComponent<SceneTransitionServiceSmokeQATester>()
                                            ?? gameObject.AddComponent<SceneTransitionServiceSmokeQATester>();

            ExecuteTester(sceneTransitionServiceTester, "SceneTransitionServiceSmokeQATester");
            if (stopOnFirstFail && _fails > 0)
            {
                DebugUtility.LogWarning(typeof(NewScriptsInfraSmokeRunner),
                    $"{SceneFlowLogTag} stopOnFirstFail ativo; interrompendo após falha.");
                Complete();
                return;
            }

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
            DebugUtility.Log(typeof(NewScriptsInfraSmokeRunner), $"{SceneFlowLogTag} FAIL - {message}");
        }

        private void Complete()
        {
            string status = _fails == 0 ? "PASS" : "FAIL";
            DebugUtility.Log(typeof(NewScriptsInfraSmokeRunner),
                $"[QA][Infra] Resultado => Passes: {_passes} | Fails: {_fails} | Status={status}",
                _fails == 0 ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);
            DebugUtility.Log(typeof(NewScriptsInfraSmokeRunner), "[QA][Infra] QA complete.");

            DebugUtility.Log(typeof(NewScriptsInfraSmokeRunner),
                $"{SceneFlowLogTag} {status} (Passes={_passes} Fails={_fails})");
        }
    }
}
