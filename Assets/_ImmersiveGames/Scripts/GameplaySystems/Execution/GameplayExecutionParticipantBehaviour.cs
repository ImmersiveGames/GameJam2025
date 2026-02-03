
using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Core.Composition;
using UnityEngine;
namespace _ImmersiveGames.Scripts.GameplaySystems.Execution
{
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplayExecutionParticipantBehaviour : MonoBehaviour, IGameplayExecutionParticipant
    {
        [Header("Execution Control")]
        [Tooltip("Se verdadeiro, o participante começa bloqueado até o Coordinator liberar.")]
        [SerializeField] private bool startBlocked;

        [Header("Components to Toggle (Manual)")]
        [Tooltip("Lista explícita de componentes que devem ser desativados quando a execução estiver bloqueada.")]
        [SerializeField] private List<Behaviour> behavioursToToggle = new();

        [Tooltip("Opcional: desativar GameObjects (ex.: sub-sistemas) quando bloqueado. Evite desativar o Root do ator.")]
        [SerializeField] private List<GameObject> gameObjectsToToggle = new();

        [Header("Auto Collect (Option 2)")]
        [Tooltip("Se verdadeiro, coleta Behaviours automaticamente quando behavioursToToggle estiver vazio (ou mescla com a lista manual, dependendo de 'mergeWithManualLists').")]
        [SerializeField] private bool autoCollectBehavioursToToggle = true;

        [Tooltip("Se verdadeiro, coleta também Behaviours nos filhos (GetComponentsInChildren).")]
        [SerializeField] private bool includeChildren;

        [Tooltip("Se verdadeiro, mescla a lista manual com a auto-coleta (evita duplicatas). Se falso, auto-coleta só ocorre quando a lista manual estiver vazia.")]
        [SerializeField] private bool mergeWithManualLists;

        [Tooltip("Se verdadeiro, remove automaticamente nulos e duplicatas nas listas.")]
        [SerializeField] private bool sanitizeListsOnEnable = true;

        [Header("Auto Collect Filters")]
        [Tooltip("Se verdadeiro, inclui componentes que estão desativados (enabled=false) durante a coleta.")]
        [SerializeField] private bool includeDisabledBehaviours = true;

        [Tooltip("Nomes de tipos (simples ou full name) a serem excluídos da auto-coleta. Use para registradores e infra que não devem ser desativados.")]
        [SerializeField] private List<string> excludedTypeNames = new()
        {
            // Execução
            nameof(GameplayExecutionParticipantBehaviour),

            // Registradores / domínios (ajuste conforme seus nomes reais)
            "ActorAutoRegistrar",
            "PlayerAutoRegistrar",
            "EaterAutoRegistrar",
            "PlanetsAutoRegistrar",
            "PlanetAutoRegistrar",
            "GameplayDomainBootstrapper",
            "GameplayManager"
        };

        [Tooltip("Se verdadeiro, exclui automaticamente componentes de UI/EventSystem (evita quebrar navegação de UI na gameplay).")]
        [SerializeField] private bool excludeUiRelatedBehaviours = true;

        [Header("Diagnostics")]
        [SerializeField] private bool logChanges;

        private IGameplayExecutionCoordinator _coordinator;
        private bool _registered;
        private bool _isAllowed = true;

        private bool _autoCollectedOnce;

        public bool IsExecutionAllowed => _isAllowed;

        private void Awake()
        {
            TryResolveCoordinator();
        }

        private void OnEnable()
        {
            if (sanitizeListsOnEnable)
            {
                SanitizeLists();
            }

            EnsureAutoCollected();

            if (startBlocked)
            {
                SetExecutionAllowed(false);
            }

            TryRegister();
        }

        private void OnDisable()
        {
            TryUnregister();
        }

        public void SetExecutionAllowed(bool allowed)
        {
            if (_isAllowed == allowed)
                return;

            _isAllowed = allowed;

            bool enable = allowed;

            for (int i = 0; i < behavioursToToggle.Count; i++)
            {
                var b = behavioursToToggle[i];
                if (b != null && b.enabled != enable)
                {
                    b.enabled = enable;
                }
            }

            for (int i = 0; i < gameObjectsToToggle.Count; i++)
            {
                var go = gameObjectsToToggle[i];
                if (go != null && go.activeSelf != enable)
                {
                    go.SetActive(enable);
                }
            }

            if (logChanges)
            {
                DebugUtility.LogVerbose<GameplayExecutionParticipantBehaviour>(
                    $"ExecutionAllowed => {allowed} (GO='{name}')");
            }
        }

        private void TryResolveCoordinator()
        {
            if (_coordinator != null)
                return;

            var sceneName = gameObject.scene.name;

            if (DependencyManager.Provider.TryGetForScene<IGameplayExecutionCoordinator>(sceneName, out var coord) && coord != null)
            {
                _coordinator = coord;
            }
        }

        private void TryRegister()
        {
            if (_registered)
                return;

            if (_coordinator == null)
            {
                TryResolveCoordinator();
            }

            if (_coordinator == null)
            {
                return;
            }

            _coordinator.Register(this);
            _registered = true;

            SetExecutionAllowed(_coordinator.IsExecutionAllowed);
        }

        private void TryUnregister()
        {
            if (!_registered)
                return;

            if (_coordinator != null)
            {
                _coordinator.Unregister(this);
            }

            _registered = false;
        }

        private void EnsureAutoCollected()
        {
            if (!_autoCollectedOnce && autoCollectBehavioursToToggle)
            {
                bool shouldCollect =
                    mergeWithManualLists ||
                    behavioursToToggle == null ||
                    behavioursToToggle.Count == 0;

                if (shouldCollect)
                {
                    AutoCollectBehaviours();
                }

                _autoCollectedOnce = true;
            }
        }

        private void AutoCollectBehaviours()
        {
            var collected = new List<Behaviour>(64);

            if (includeChildren)
            {
                GetComponentsInChildren(includeDisabledBehaviours, collected);
            }
            else
            {
                GetComponents(collected);
                if (!includeDisabledBehaviours)
                {
                    for (int i = collected.Count - 1; i >= 0; i--)
                    {
                        if (collected[i] != null && collected[i].enabled == false)
                        {
                            collected.RemoveAt(i);
                        }
                    }
                }
            }

            int before = behavioursToToggle.Count;

            for (int i = 0; i < collected.Count; i++)
            {
                var b = collected[i];
                if (b == null)
                    continue;

                if (ShouldExcludeFromAutoCollect(b))
                    continue;

                if (!behavioursToToggle.Contains(b))
                {
                    behavioursToToggle.Add(b);
                }
            }

            if (sanitizeListsOnEnable)
            {
                SanitizeLists();
            }

            if (logChanges)
            {
                DebugUtility.LogVerbose<GameplayExecutionParticipantBehaviour>(
                    $"AutoCollectBehaviours: +{behavioursToToggle.Count - before} behaviours (total={behavioursToToggle.Count}) (GO='{name}')");
            }
        }

        private bool ShouldExcludeFromAutoCollect(Behaviour b)
        {
            if (ReferenceEquals(b, this))
                return true;

            var t = b.GetType();

            // PASSO 2 (refeito): marker interface (sem reflexão)
            if (b is IExecutionToggleIgnored)
            {
                return true;
            }

            if (excludeUiRelatedBehaviours)
            {
                var ns = t.Namespace ?? string.Empty;

                if (ns.StartsWith("UnityEngine.UI", StringComparison.Ordinal) ||
                    ns.StartsWith("TMPro", StringComparison.Ordinal) ||
                    ns.StartsWith("UnityEngine.EventSystems", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            if (excludedTypeNames != null && excludedTypeNames.Count > 0)
            {
                string typeName = t.Name;
                string fullName = t.FullName ?? string.Empty;

                for (int i = 0; i < excludedTypeNames.Count; i++)
                {
                    var ex = excludedTypeNames[i];
                    if (string.IsNullOrWhiteSpace(ex))
                        continue;

                    if (string.Equals(ex, typeName, StringComparison.Ordinal) ||
                        string.Equals(ex, fullName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            if (b is IGameplayExecutionCoordinator)
                return true;

            return false;
        }

        private void SanitizeLists()
        {
            if (behavioursToToggle != null)
            {
                for (int i = behavioursToToggle.Count - 1; i >= 0; i--)
                {
                    if (behavioursToToggle[i] == null)
                    {
                        behavioursToToggle.RemoveAt(i);
                    }
                }

                for (int i = 0; i < behavioursToToggle.Count; i++)
                {
                    var a = behavioursToToggle[i];
                    for (int j = behavioursToToggle.Count - 1; j > i; j--)
                    {
                        if (ReferenceEquals(a, behavioursToToggle[j]))
                        {
                            behavioursToToggle.RemoveAt(j);
                        }
                    }
                }
            }

            if (gameObjectsToToggle != null)
            {
                for (int i = gameObjectsToToggle.Count - 1; i >= 0; i--)
                {
                    if (gameObjectsToToggle[i] == null)
                    {
                        gameObjectsToToggle.RemoveAt(i);
                    }
                }

                for (int i = 0; i < gameObjectsToToggle.Count; i++)
                {
                    var a = gameObjectsToToggle[i];
                    for (int j = gameObjectsToToggle.Count - 1; j > i; j--)
                    {
                        if (ReferenceEquals(a, gameObjectsToToggle[j]))
                        {
                            gameObjectsToToggle.RemoveAt(j);
                        }
                    }
                }
            }
        }
    }
}

