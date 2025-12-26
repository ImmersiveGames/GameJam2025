using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.Reset;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.QA.GameplayReset
{
    /// <summary>
    /// QA helper: spawna Players sintéticos (PlayerActor) e dispara GameplayReset sem depender do spawn pipeline.
    /// Objetivo: validar Target=PlayersOnly / AllActorsInScene / ActorIdSet e as fases Cleanup/Restore/Rebind.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayResetQaSpawner : MonoBehaviour
    {
        [Header("QA Spawn")]
        [SerializeField] private int spawnCount = 2;

        [SerializeField]
        [Tooltip("Se true, tenta parentear os QA Players em um 'WorldRoot' da cena.")]
        private bool parentUnderWorldRoot = true;

        [SerializeField]
        [Tooltip("Se atribuído, usa este parent em vez de procurar WorldRoot.")]
        private Transform parentOverride;

        [Header("QA Probe")]
        [SerializeField] private bool addProbeComponent = true;

        [SerializeField]
        [Tooltip("Delay artificial em ms em cada fase do probe (0 = sem delay).")]
        private int probeDelayMs = 0;

        [SerializeField]
        [Tooltip("Log verboso para cada callback de reset.")]
        private bool probeVerboseLogs = true;

        private readonly List<GameObject> _spawned = new(16);
        private string _sceneName;

        private void Awake()
        {
            _sceneName = gameObject.scene.name;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD

        [ContextMenu("QA/GameplayReset/Spawn QA Players")]
        public void QA_SpawnPlayers()
        {
            EnsureSceneName();

            int count = Mathf.Max(0, spawnCount);
            if (count == 0)
            {
                DebugUtility.LogWarning(typeof(GameplayResetQaSpawner),
                    "[QA][GameplayReset] spawnCount=0. Nada para criar.");
                return;
            }

            var parent = ResolveParent();
            int created = 0;

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"QA_Player_{i:00}");
                SceneManager.MoveGameObjectToScene(go, gameObject.scene);

                if (parent != null)
                    go.transform.SetParent(parent, worldPositionStays: false);

                // Marca como Player: PlayerActor implementa IActor e passa no filtro PlayersOnly (GetComponent<PlayerActor>()).
                var actor = go.AddComponent<PlayerActor>();

                // Adiciona um probe que implementa IGameplayResettable para confirmar Cleanup/Restore/Rebind.
                if (addProbeComponent)
                {
                    var probe = go.AddComponent<GameplayResetQaProbe>();
                    probe.Configure(probeVerboseLogs, probeDelayMs);
                }

                _spawned.Add(go);
                created++;
            }

            DebugUtility.Log(typeof(GameplayResetQaSpawner),
                $"[QA][GameplayReset] Spawned QA Players: {created} (scene='{_sceneName}')");
        }

        [ContextMenu("QA/GameplayReset/Clear QA Players")]
        public void QA_ClearPlayers()
        {
            int removed = 0;

            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                var go = _spawned[i];
                _spawned.RemoveAt(i);

                if (go == null)
                    continue;

                removed++;
                DestroyImmediate(go);
            }

            DebugUtility.Log(typeof(GameplayResetQaSpawner),
                $"[QA][GameplayReset] Cleared QA Players: {removed} (scene='{_sceneName}')");
        }

        [ContextMenu("QA/GameplayReset/Run Reset PlayersOnly")]
        public void QA_RunPlayersOnly()
        {
            _ = RunResetAsync(
                new GameplayResetRequest(
                    GameplayResetTarget.PlayersOnly,
                    reason: "QA/GameplayResetPlayersOnly"));
        }

        [ContextMenu("QA/GameplayReset/Run Reset AllActorsInScene")]
        public void QA_RunAllActors()
        {
            _ = RunResetAsync(
                new GameplayResetRequest(
                    GameplayResetTarget.AllActorsInScene,
                    reason: "QA/GameplayResetAllActors"));
        }

        [ContextMenu("QA/GameplayReset/Run Reset ActorIdSet (First Spawned)")]
        public void QA_RunActorIdSet_First()
        {
            if (_spawned.Count == 0 || _spawned[0] == null)
            {
                DebugUtility.LogWarning(typeof(GameplayResetQaSpawner),
                    "[QA][GameplayReset] Nenhum QA Player disponível. Rode 'Spawn QA Players' primeiro.");
                return;
            }

            var actor = _spawned[0].GetComponent<IActor>();
            if (actor == null || string.IsNullOrWhiteSpace(actor.ActorId))
            {
                DebugUtility.LogWarning(typeof(GameplayResetQaSpawner),
                    "[QA][GameplayReset] Primeiro QA Player não tem ActorId válido ainda. Tente novamente após um frame.");
                return;
            }

            _ = RunResetAsync(
                new GameplayResetRequest(
                    GameplayResetTarget.ActorIdSet,
                    reason: "QA/GameplayResetActorIdSet_First",
                    actorIds: new[] { actor.ActorId }));
        }

#endif

        private async Task RunResetAsync(GameplayResetRequest request)
        {
            EnsureSceneName();

            if (!DependencyManager.Provider.TryGetForScene(_sceneName, out IGameplayResetOrchestrator orchestrator) || orchestrator == null)
            {
                DebugUtility.LogWarning(typeof(GameplayResetQaSpawner),
                    $"[QA][GameplayReset] IGameplayResetOrchestrator não encontrado no DI da cena '{_sceneName}'. " +
                    "Verifique se o NewSceneBootstrapper registrou o orchestrator para esta cena.");
                return;
            }

            DebugUtility.Log(typeof(GameplayResetQaSpawner),
                $"[QA][GameplayReset] Request => {request} (scene='{_sceneName}')");

            try
            {
                await orchestrator.RequestResetAsync(request);
                DebugUtility.Log(typeof(GameplayResetQaSpawner),
                    $"[QA][GameplayReset] Completed => {request} (scene='{_sceneName}')");
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(GameplayResetQaSpawner),
                    $"[QA][GameplayReset] Failed => {request}. ex={ex}");
            }
        }

        private void EnsureSceneName()
        {
            if (string.IsNullOrWhiteSpace(_sceneName))
                _sceneName = gameObject.scene.name;
        }

        private Transform ResolveParent()
        {
            if (parentOverride != null)
                return parentOverride;

            if (!parentUnderWorldRoot)
                return null;

            var roots = gameObject.scene.GetRootGameObjects();
            if (roots == null || roots.Length == 0)
                return null;

            for (int i = 0; i < roots.Length; i++)
            {
                var go = roots[i];
                if (go != null && go.name == "WorldRoot")
                    return go.transform;
            }

            return null;
        }
    }
}
