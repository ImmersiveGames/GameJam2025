using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.Reset;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Ids;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.QA.GameplayReset
{
    /// <summary>
    /// QA helper para validar reset por ActorKind com Player + Dummy.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayResetKindQaSpawner : MonoBehaviour
    {
        [Header("QA Spawn")]
        [SerializeField]
        [Tooltip("Se true, tenta parentear os atores em um 'WorldRoot' da cena.")]
        private bool parentUnderWorldRoot = true;

        [SerializeField]
        [Tooltip("Se atribuído, usa este parent em vez de procurar WorldRoot.")]
        private Transform parentOverride;

        [Header("QA Probe")]
        [SerializeField]
        private bool addProbeComponent = true;

        [SerializeField]
        private bool probeVerboseLogs = true;

        [Header("QA Eater")]
        [SerializeField]
        private GameplayResetKindQaEaterActor eaterActorPrefab;

        [SerializeField]
        private bool spawnEaterProbe = true;

        private readonly List<GameObject> _spawned = new(4);
        private readonly List<IActor> _actorBuffer = new(8);

        private string _sceneName;
        private IActorRegistry _actorRegistry;
        private IGameplayResetOrchestrator _orchestrator;
        private IUniqueIdFactory _uniqueIdFactory;

        private void Awake()
        {
            _sceneName = gameObject.scene.name;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD

        [ContextMenu("QA/GameplayResetKind/Spawn Player + Dummy")]
        public void QA_SpawnPlayerAndDummy()
        {
            EnsureDependencies();
            ClearSpawnedInternal();

            var parent = ResolveParent();
            var player = SpawnPlayer(parent);
            var dummy = SpawnDummy(parent);
            var eater = SpawnEater(parent);

            int total = (player != null ? 1 : 0) + (dummy != null ? 1 : 0) + (eater != null ? 1 : 0);
            DebugUtility.Log(typeof(GameplayResetKindQaSpawner),
                $"[QA][GameplayResetKind] Spawned actors: {total} (scene='{_sceneName}')");
        }

        [ContextMenu("QA/GameplayResetKind/Clear Spawned Actors")]
        public void QA_ClearSpawned()
        {
            EnsureDependencies();
            ClearSpawnedInternal();

            DebugUtility.Log(typeof(GameplayResetKindQaSpawner),
                $"[QA][GameplayResetKind] Cleared spawned actors (scene='{_sceneName}')");
        }

        [ContextMenu("QA/GameplayResetKind/Run Reset Kind Player")]
        public void QA_RunResetPlayerKind()
        {
            _ = RunResetByKindAsync(ActorKind.Player, "QA/GameplayResetKindPlayer");
        }

        [ContextMenu("QA/GameplayResetKind/Run Reset Kind Dummy")]
        public void QA_RunResetDummyKind()
        {
            _ = RunResetByKindAsync(ActorKind.Dummy, "QA/GameplayResetKindDummy");
        }

#endif

        private GameObject SpawnPlayer(Transform parent)
        {
            var go = new GameObject("QA_Player_Kind");
            SceneManager.MoveGameObjectToScene(go, gameObject.scene);

            if (parent != null)
            {
                go.transform.SetParent(parent, worldPositionStays: false);
            }

            var player = go.AddComponent<PlayerActor>();
            var actorId = GenerateActorId(go, "QA_Player");
            player.Initialize(actorId);

            RegisterActor(player);
            AddProbe(go, "Player Probe");

            _spawned.Add(go);
            return go;
        }

        private GameObject SpawnDummy(Transform parent)
        {
            var go = new GameObject("QA_Dummy_Kind");
            SceneManager.MoveGameObjectToScene(go, gameObject.scene);

            if (parent != null)
            {
                go.transform.SetParent(parent, worldPositionStays: false);
            }

            var dummy = go.AddComponent<GameplayResetKindQaDummyActor>();
            var actorId = GenerateActorId(go, "QA_Dummy");
            dummy.Initialize(actorId);

            RegisterActor(dummy);
            AddProbe(go, "Dummy Probe");

            _spawned.Add(go);
            return go;
        }

        private GameObject SpawnEater(Transform parent)
        {
            if (!spawnEaterProbe || eaterActorPrefab == null)
            {
                return null;
            }

            var instance = Instantiate(eaterActorPrefab, parent);
            var go = instance.gameObject;
            SceneManager.MoveGameObjectToScene(go, gameObject.scene);

            go.name = "QA_Eater_Kind";

            var actorId = GenerateActorId(go, "QA_Eater_Kind");
            instance.Initialize(actorId);

            _ = go.GetComponent<EaterActor>() ?? go.AddComponent<EaterActor>();

            RegisterActor(instance);
            AddProbe(go, "Eater Probe");

            _spawned.Add(go);
            return go;
        }

        private void AddProbe(GameObject go, string label)
        {
            if (!addProbeComponent || go == null)
            {
                return;
            }

            var probe = go.AddComponent<GameplayResetKindQaProbe>();
            probe.Configure(label, probeVerboseLogs);
        }

        private void RegisterActor(IActor actor)
        {
            if (_actorRegistry == null)
            {
                DebugUtility.LogWarning(typeof(GameplayResetKindQaSpawner),
                    "[QA][GameplayResetKind] IActorRegistry ausente; ator não será registrado.");
                return;
            }

            if (actor == null)
            {
                return;
            }

            if (!_actorRegistry.Register(actor))
            {
                DebugUtility.LogWarning(typeof(GameplayResetKindQaSpawner),
                    $"[QA][GameplayResetKind] Falha ao registrar ator. ActorId={actor.ActorId}");
            }
        }

        private void ClearSpawnedInternal()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                var go = _spawned[i];
                _spawned.RemoveAt(i);

                if (go == null)
                {
                    continue;
                }

                var actor = go.GetComponent<IActor>();
                if (actor != null && _actorRegistry != null)
                {
                    _actorRegistry.Unregister(actor.ActorId);
                }

                if (Application.isPlaying)
                {
                    Destroy(go);
                }
                else
                {
                    DestroyImmediate(go);
                }
            }
        }

        private async Task RunResetByKindAsync(ActorKind kind, string reason)
        {
            EnsureDependencies();

            if (_orchestrator == null)
            {
                DebugUtility.LogWarning(typeof(GameplayResetKindQaSpawner),
                    $"[QA][GameplayResetKind] IGameplayResetOrchestrator não encontrado na cena '{_sceneName}'.");
                return;
            }

            LogResolvedTargets(kind);

            var request = GameplayResetRequest.ByActorKind(kind, reason);

            DebugUtility.Log(typeof(GameplayResetKindQaSpawner),
                $"[QA][GameplayResetKind] Request => {request} (scene='{_sceneName}')");

            try
            {
                await _orchestrator.RequestResetAsync(request);
                DebugUtility.Log(typeof(GameplayResetKindQaSpawner),
                    $"[QA][GameplayResetKind] Completed => {request} (scene='{_sceneName}')");
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(GameplayResetKindQaSpawner),
                    $"[QA][GameplayResetKind] Failed => {request}. ex={ex}");
            }
        }

        private void LogResolvedTargets(ActorKind kind)
        {
            if (_actorRegistry == null)
            {
                DebugUtility.LogWarning(typeof(GameplayResetKindQaSpawner),
                    "[QA][GameplayResetKind] IActorRegistry ausente; não foi possível resolver targets.");
                return;
            }

            _actorBuffer.Clear();
            _actorRegistry.GetActors(_actorBuffer);

            int count = 0;
            var labels = new List<string>(_actorBuffer.Count);

            foreach (var actor in _actorBuffer)
            {
                if (actor is not IActorKindProvider provider)
                {
                    continue;
                }

                if (provider.Kind != kind)
                {
                    continue;
                }

                count++;
                string name = string.IsNullOrWhiteSpace(actor.DisplayName) ? actor.ActorId : actor.DisplayName;
                labels.Add($"{name}:{actor.ActorId}");
            }

            string labelText = labels.Count > 0 ? string.Join(", ", labels) : "<none>";

            DebugUtility.Log(typeof(GameplayResetKindQaSpawner),
                $"[QA][GameplayResetKind] Resolved targets for kind={kind}: {count} => {labelText}");
        }

        private string GenerateActorId(GameObject instance, string prefix)
        {
            if (instance == null)
            {
                return $"{prefix}_{Guid.NewGuid():N}";
            }

            if (_uniqueIdFactory != null)
            {
                string generated = _uniqueIdFactory.GenerateId(instance);
                if (!string.IsNullOrWhiteSpace(generated))
                {
                    return generated;
                }
            }

            return $"{prefix}_{Guid.NewGuid():N}";
        }

        private void EnsureDependencies()
        {
            if (string.IsNullOrWhiteSpace(_sceneName))
            {
                _sceneName = gameObject.scene.name;
            }

            var provider = DependencyManager.Provider;
            provider.TryGetForScene(_sceneName, out _actorRegistry);
            provider.TryGetForScene(_sceneName, out _orchestrator);
            provider.TryGetGlobal<IUniqueIdFactory>(out _uniqueIdFactory);
        }

        private Transform ResolveParent()
        {
            if (parentOverride != null)
            {
                return parentOverride;
            }

            if (!parentUnderWorldRoot)
            {
                return null;
            }

            var roots = gameObject.scene.GetRootGameObjects();
            if (roots == null || roots.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root != null && root.name == "WorldRoot")
                {
                    return root.transform;
                }
            }

            return null;
        }
    }
}
