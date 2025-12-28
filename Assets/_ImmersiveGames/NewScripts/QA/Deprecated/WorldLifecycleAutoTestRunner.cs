// DEPRECATED QA TOOL — ver Docs/Reports/QA-Audit-2025-12-27.md
﻿using System;
using System.Reflection;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA
{
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    [System.Obsolete("Deprecated QA tool; see QA-Audit-2025-12-27", false)]
    public sealed class WorldLifecycleAutoTestRunner : MonoBehaviour
    {
        [Header("Runner")]
        [SerializeField] private string label = "WorldLifecycleAutoTestRunner";
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private int warmupFrames = 1;

        [Tooltip("Se verdadeiro, evita rodar o runner mais de uma vez por instância, mesmo que o GameObject seja reativado.")]
        [SerializeField] private bool runOnlyOnce = true;

        [Tooltip("Se verdadeiro, desabilita o componente ao terminar (evita re-run acidental).")]
        [SerializeField] private bool disableAfterRun = true;

        [Tooltip("Se verdadeiro, destrói o GameObject ao terminar (bom para QA descartável).")]
        [SerializeField] private bool destroyAfterRun;

        [Header("Test Plan")]
        [SerializeField] private bool runSingleReset = true;
        [SerializeField] private bool runStressReset = true;

        [SerializeField] private int stressCount = 3;

        [Tooltip("Delay entre iterações do stress (em segundos).")]
        [SerializeField] private float stressDelaySeconds = 0.25f;

        [Tooltip("Insere um gap mínimo (yield/delay) entre o SingleReset e o primeiro Stress#1, evitando warnings de 'mesmo frame'.")]
        [SerializeField] private float preStressDelaySeconds = 0.05f;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = true;

        private WorldLifecycleController _controller;
        private string _sceneName;
        private bool _hasRun;

        // QA Gate toggle (EventBus)
        private bool _pausedByQaToggle;
        private bool _loggedEventBusUnavailable;

        private void Awake()
        {
            _sceneName = gameObject.scene.name;

            // Detectar duplicatas cedo (o caso mais comum do "rodou duas vezes").
            WorldLifecycleAutoTestRunner[] runners = FindObjectsByType<WorldLifecycleAutoTestRunner>(FindObjectsSortMode.None);
            if (runners is { Length: > 1 })
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleAutoTestRunner),
                    $"[QA] {label}: Detectados {runners.Length} runners na cena '{_sceneName}'. " +
                    $"Isso pode causar AutoTest duplicado. Este runner instanceId={GetInstanceID()}");
            }

            _controller = FindFirstObjectByType<WorldLifecycleController>();
            if (_controller == null)
            {
                DebugUtility.LogError(typeof(WorldLifecycleAutoTestRunner),
                    $"[QA] {label}: WorldLifecycleController não encontrado na cena '{_sceneName}'.");
                return;
            }

            // Opção B: runner controla o Start do controller.
            _controller.AutoInitializeOnStart = false;

            if (verboseLogs)
            {
                DebugUtility.Log(typeof(WorldLifecycleAutoTestRunner),
                    $"[QA] {label}: AutoInitializeOnStart=false aplicado no controller (scene='{_sceneName}').");
            }
        }

        private void Start()
        {
            if (!runOnStart)
            {
                if (verboseLogs)
                {
                    DebugUtility.Log(typeof(WorldLifecycleAutoTestRunner),
                        $"[QA] {label}: runOnStart=false, runner inativo (scene='{_sceneName}').");
                }
                return;
            }

            if (runOnlyOnce && _hasRun)
            {
                if (verboseLogs)
                {
                    DebugUtility.LogWarning(typeof(WorldLifecycleAutoTestRunner),
                        $"[QA] {label}: Start ignorado (já executado). scene='{_sceneName}'.");
                }
                return;
            }

            _ = RunAsync();
        }

        private async Task RunAsync()
        {
            if (_controller == null)
            {
                return;
            }

            if (runOnlyOnce && _hasRun)
            {
                return;
            }

            _hasRun = true;

            DebugUtility.Log(typeof(WorldLifecycleAutoTestRunner),
                $"[QA] {label}: AutoTestRunner starting (scene='{_sceneName}', warmupFrames={warmupFrames}).");

            // Warmup frames (estabiliza boot/DI/registries).
            for (int i = 0; i < Mathf.Max(0, warmupFrames); i++)
            {
                await Task.Yield();
            }

            // Reset único
            if (runSingleReset)
            {
                await _controller.ResetWorldAsync($"{label}/SingleReset");
            }

            // Gap mínimo antes do stress (evita warnings de "mesmo frame")
            if (runStressReset)
            {
                if (preStressDelaySeconds > 0f)
                {
                    int ms = Mathf.Max(1, (int)(preStressDelaySeconds * 1000f));
                    await Task.Delay(ms);
                }
                else
                {
                    await Task.Yield();
                }
            }

            // Stress
            if (runStressReset)
            {
                if (stressCount <= 0)
                {
                    DebugUtility.LogWarning(typeof(WorldLifecycleAutoTestRunner),
                        $"[QA] {label}: stressCount inválido ({stressCount}). Ignorando stress.");
                }
                else
                {
                    DebugUtility.Log(typeof(WorldLifecycleAutoTestRunner),
                        $"[QA] {label}: Running stress reset (count={stressCount}, delay={stressDelaySeconds:0.00}s).");

                    for (int i = 0; i < stressCount; i++)
                    {
                        await _controller.ResetWorldAsync($"{label}/Stress#{i + 1}");

                        if (i < stressCount - 1 && stressDelaySeconds > 0f)
                        {
                            int delayMs = Mathf.Max(1, (int)(stressDelaySeconds * 1000f));
                            await Task.Delay(delayMs);
                        }
                        else
                        {
                            // Sempre dá pelo menos 1 yield entre resets para não colar tudo no mesmo frame.
                            await Task.Yield();
                        }
                    }
                }
            }

            DebugUtility.Log(typeof(WorldLifecycleAutoTestRunner),
                $"[QA] {label}: AutoTestRunner finished (scene='{_sceneName}').");

            if (disableAfterRun)
            {
                enabled = false;
            }

            if (destroyAfterRun)
            {
                Destroy(gameObject);
            }
        }

        [ContextMenu("QA/Run AutoTest Now")]
        public void RunNow()
        {
            _ = RunAsync();
        }

        // --------------------------------------------------------------------
        // QA Gate Toggle (EventBus) — para testar gating no NewPlayerMovementController
        // --------------------------------------------------------------------

        [ContextMenu("QA/Gate/Toggle Pause (EventBus)")]
        public void TogglePauseEventBus()
        {
            _pausedByQaToggle = !_pausedByQaToggle;
            ApplyPauseState(_pausedByQaToggle);
        }

        [ContextMenu("QA/Gate/Force Pause (EventBus)")]
        public void ForcePauseEventBus()
        {
            _pausedByQaToggle = true;
            ApplyPauseState(true);
        }

        [ContextMenu("QA/Gate/Force Resume (EventBus)")]
        public void ForceResumeEventBus()
        {
            _pausedByQaToggle = false;
            ApplyPauseState(false);
        }

        private void ApplyPauseState(bool paused)
        {
            // Publica GamePauseCommandEvent(IsPaused=paused). Para “resume”, também tenta GameResumeRequestedEvent / GameStartCommandEvent.
            try
            {
                PublishGamePauseEvent(paused);

                if (!paused)
                {
                    // Ordem proposital: resume-request primeiro; fallback para start.
                    PublishSimpleEvent<GameResumeRequestedEvent>();
                    PublishSimpleEvent<GameStartRequestedEvent>();
                }

                DebugUtility.LogVerbose(
                    typeof(WorldLifecycleAutoTestRunner),
                    $"[QA Gate Toggle] Published pause/resume via EventBus. paused={paused}",
                    DebugUtility.Colors.Info);

                _loggedEventBusUnavailable = false;
            }
            catch (Exception ex)
            {
                if (!_loggedEventBusUnavailable)
                {
                    DebugUtility.LogWarning(
                        typeof(WorldLifecycleAutoTestRunner),
                        $"[QA Gate Toggle] EventBus indisponível; não foi possível publicar pause/resume ({ex.GetType().Name}).");
                    _loggedEventBusUnavailable = true;
                }
            }
        }

        private void PublishGamePauseEvent(bool paused)
        {
            // Não assume construtor/propriedade; tenta setar IsPaused via reflection se existir.
            var evt = CreateEventInstance<GamePauseCommandEvent>();
            TrySetPausedFlag(evt, paused);
            PublishEvent(evt);
        }

        private void PublishSimpleEvent<T>() where T : class
        {
            // Para classes, tenta instanciar sem assumir construtores públicos.
            var evt = CreateEventInstance<T>();
            PublishEvent(evt);
        }

        private static T CreateEventInstance<T>()
        {
            var t = typeof(T);

            if (t.IsValueType)
            {
                return default;
            }

            // Tenta ctor público sem args; se falhar, tenta non-public; se falhar, cria "uninitialized".
            try
            {
                object instance = Activator.CreateInstance(t);
                if (instance != null)
                {
                    return (T)instance;
                }
            }
            catch { /* ignore */ }

            try
            {
                object instance = Activator.CreateInstance(t, nonPublic: true);
                if (instance != null)
                {
                    return (T)instance;
                }
            }
            catch { /* ignore */ }

            // Último recurso (evita hard fail se o evento não tiver ctor acessível).
            return (T)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(t);
        }

        private static void TrySetPausedFlag<T>(T evt, bool paused)
        {
            if (evt == null)
            {
                return;
            }

            var t = evt.GetType();

            // property IsPaused
            var prop = t.GetProperty("IsPaused", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && prop.CanWrite && prop.PropertyType == typeof(bool))
            {
                prop.SetValue(evt, paused);
                return;
            }

            // field IsPaused
            var field = t.GetField("IsPaused", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.FieldType == typeof(bool))
            {
                field.SetValue(evt, paused);
            }
        }

        private static void PublishEvent<T>(T evt)
        {
            // Descobre dinamicamente o método de publicação no EventBus<T> sem chutar API fixa.
            var busType = typeof(EventBus<>).MakeGenericType(typeof(T));

            // Procura métodos estáticos comuns (Raise/Publish/Fire/Trigger/Emit/Send).
            var method =
                busType.GetMethod("Raise", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(T) }, null) ??
                busType.GetMethod("Publish", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(T) }, null) ??
                busType.GetMethod("Fire", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(T) }, null) ??
                busType.GetMethod("Trigger", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(T) }, null) ??
                busType.GetMethod("Emit", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(T) }, null) ??
                busType.GetMethod("Send", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(T) }, null);

            if (method == null)
            {
                // Se a API for “no args” (raro), tenta sem parâmetros.
                method =
                    busType.GetMethod("Raise", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) ??
                    busType.GetMethod("Publish", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) ??
                    busType.GetMethod("Fire", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) ??
                    busType.GetMethod("Trigger", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) ??
                    busType.GetMethod("Emit", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) ??
                    busType.GetMethod("Send", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            }

            if (method == null)
            {
                throw new MissingMethodException(busType.FullName, "Raise/Publish/Fire/Trigger/Emit/Send");
            }

            ParameterInfo[] parameters = method.GetParameters();
            method.Invoke(null, parameters.Length == 0 ? null : new object[] { evt });
        }
    }
}
