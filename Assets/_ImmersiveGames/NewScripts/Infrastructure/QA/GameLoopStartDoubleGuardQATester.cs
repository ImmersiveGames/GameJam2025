using System;
using System.Collections;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameLoopStartDoubleGuardQATester : MonoBehaviour
    {
        // Deve bater com o StartProfileName do GlobalBootstrap/startPlan.
        private const string ExpectedStartProfile = "startup";

        private CountingGameLoopService _counting;
        private bool _seenScenesReady;
        private string _scenesReadyProfile;

        private EventBinding<SceneTransitionScenesReadyEvent> _onScenesReady;

        private void Awake()
        {
            _onScenesReady = new EventBinding<SceneTransitionScenesReadyEvent>(OnScenesReady);
            EventBus<SceneTransitionScenesReadyEvent>.Register(_onScenesReady);
        }

        private void OnDestroy()
        {
            EventBus<SceneTransitionScenesReadyEvent>.Unregister(_onScenesReady);
        }

        private IEnumerator Start()
        {
            DebugUtility.LogVerbose<GameLoopStartDoubleGuardQATester>("[QA] START GameLoopStartDoubleGuard");

            if (!InstallCountingOverride())
                yield break;

            DebugUtility.LogVerbose<GameLoopStartDoubleGuardQATester>("[QA] Step 1: Raise GameStartEvent");
            EventBus<GameStartEvent>.Raise(new GameStartEvent());

            // Aguarda o ScenesReady do START (profile esperado), com timeout.
            var timeout = Time.realtimeSinceStartup + 5f;
            while (!_seenScenesReady && Time.realtimeSinceStartup < timeout)
                yield return null;

            if (!_seenScenesReady)
            {
                DebugUtility.LogError<GameLoopStartDoubleGuardQATester>(
                    "[QA] FAIL: Não observou SceneTransitionScenesReadyEvent do START dentro do timeout. " +
                    $"ExpectedProfile='{ExpectedStartProfile}'. " +
                    "Causa provável: SceneTransitionService não está emitindo ScenesReady ou coordinator não disparou transição.");
                yield break;
            }

            // Aguarda alguns frames para o coordinator liberar o start após ScenesReady.
            for (var i = 0; i < 5; i++)
                yield return null;

            var total = _counting.RequestStartCount;

            if (total != 1)
            {
                DebugUtility.LogError<GameLoopStartDoubleGuardQATester>(
                    $"[QA] FAIL: RequestStart() deveria ser chamado EXATAMENTE 1x após o ScenesReady do START. countTotal={total}. " +
                    $"ScenesReadyProfile='{_scenesReadyProfile}'. " +
                    "Se count=2, você tem 'Start duplo' (bridge + coordinator). " +
                    "Se count=0, o coordinator não liberou start OU ainda está cacheando IGameLoopService (override não observado).");
                yield break;
            }

            DebugUtility.Log<GameLoopStartDoubleGuardQATester>(
                $"[QA] PASS: RequestStart() chamado 1x após ScenesReady do START (profile='{_scenesReadyProfile}').",
                DebugUtility.Colors.Success);
        }

        private void OnScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            if (_seenScenesReady)
                return;

            var profile = evt.Context.TransitionProfileName;

            if (!string.Equals(profile, ExpectedStartProfile, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<GameLoopStartDoubleGuardQATester>(
                    $"[QA] ScenesReady ignorado (profile diferente). expected='{ExpectedStartProfile}', actual='{profile}'.");
                return;
            }

            _seenScenesReady = true;
            _scenesReadyProfile = profile;

            DebugUtility.LogVerbose<GameLoopStartDoubleGuardQATester>(
                $"[QA] Observado SceneTransitionScenesReadyEvent do START (profile='{_scenesReadyProfile}').");
        }

        private bool InstallCountingOverride()
        {
            var provider = DependencyManager.Provider;

            if (!provider.TryGetGlobal<IGameLoopService>(out var original) || original == null)
            {
                DebugUtility.LogError<GameLoopStartDoubleGuardQATester>(
                    "[QA] FAIL: IGameLoopService não encontrado no DI global.");
                return false;
            }

            _counting = new CountingGameLoopService(original);

            provider.RegisterGlobal<IGameLoopService>(_counting, allowOverride: true);

            DebugUtility.LogVerbose<GameLoopStartDoubleGuardQATester>(
                "[QA] CountingGameLoopService instalado como override do IGameLoopService no DI global.");

            return true;
        }

        private sealed class CountingGameLoopService : IGameLoopService
        {
            private readonly IGameLoopService _inner;

            public int RequestStartCount { get; private set; }

            public CountingGameLoopService(IGameLoopService inner)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            }

            public void Initialize() => _inner.Initialize();
            public void Tick(float dt) => _inner.Tick(dt);

            public void RequestStart()
            {
                RequestStartCount++;
                _inner.RequestStart();
            }

            public void RequestPause() => _inner.RequestPause();
            public void RequestResume() => _inner.RequestResume();
            public void RequestReset() => _inner.RequestReset();
            public void Dispose() => _inner.Dispose();
            public string CurrentStateName => _inner.CurrentStateName;
        }
    }
}
