using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneReferences;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode
{
    [CreateAssetMenu(
        fileName = "RuntimePersistentScenesPolicyAsset",
        menuName = "ImmersiveGames/NewScripts/Infrastructure/RuntimeMode/RuntimePersistentScenesPolicyAsset",
        order = 21)]
    public sealed class RuntimePersistentScenesPolicyAsset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string policyId;

        [Header("Entries")]
        [SerializeField] private List<RuntimePersistentSceneEntry> entries = new();

        public string PolicyId => Normalize(policyId);
        public IReadOnlyList<RuntimePersistentSceneEntry> Entries => entries;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnValidate()
        {
            policyId = Normalize(policyId);

            if (TryValidate(out string errorMessage) || string.IsNullOrWhiteSpace(errorMessage))
            {
                return;
            }

            DebugUtility.LogWarning(
                typeof(RuntimePersistentScenesPolicyAsset),
                $"[Config][Editor] policyId='{PolicyId}' invalida. detail='{errorMessage}'");
        }
#endif

        public bool TryValidate(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(PolicyId))
            {
                errorMessage = "policyId is required.";
                return false;
            }

            if (entries == null)
            {
                errorMessage = "entries is required.";
                return false;
            }

            HashSet<string> dedupe = new(StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
            {
                if (!TryResolveSceneName(entries[i], $"entries[{i}]", out string sceneName, out errorMessage))
                {
                    return false;
                }

                if (!dedupe.Add(sceneName))
                {
                    errorMessage = $"entries contains duplicate scene='{sceneName}'.";
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        public IReadOnlyList<string> ResolveSceneNamesOrFail(string owner)
        {
            if (!TryValidate(out string errorMessage))
            {
                HardFailFastH1.Trigger(
                    typeof(RuntimePersistentScenesPolicyAsset),
                    $"[FATAL][Config][RuntimeMode] RuntimePersistentScenesPolicyAsset invalida. owner='{Normalize(owner)}' policyId='{PolicyId}' detail='{errorMessage}'.");
            }

            if (entries.Count == 0)
            {
                return Array.Empty<string>();
            }

            var sceneNames = new List<string>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                if (!TryResolveSceneName(entries[i], $"entries[{i}]", out string sceneName, out _))
                {
                    continue;
                }

                sceneNames.Add(sceneName);
            }

            return sceneNames;
        }

        public string ResolveSceneNameByRoleOrFail(RuntimePersistentSceneRole role, string owner)
        {
            if (!TryValidate(out string errorMessage))
            {
                HardFailFastH1.Trigger(
                    typeof(RuntimePersistentScenesPolicyAsset),
                    $"[FATAL][Config][RuntimeMode] RuntimePersistentScenesPolicyAsset invalida. owner='{Normalize(owner)}' policyId='{PolicyId}' detail='{errorMessage}'.");
            }

            string normalizedOwner = Normalize(owner);
            string matchedSceneName = string.Empty;
            bool hasMatch = false;

            for (int i = 0; i < entries.Count; i++)
            {
                RuntimePersistentSceneEntry entry = entries[i];
                if (entry == null || entry.Role != role)
                {
                    continue;
                }

                if (!TryResolveSceneName(entry, $"entries[{i}]", out string sceneName, out errorMessage))
                {
                    HardFailFastH1.Trigger(
                        typeof(RuntimePersistentScenesPolicyAsset),
                        $"[FATAL][Config][RuntimeMode] persistent scene role resolution failed. owner='{normalizedOwner}' policyId='{PolicyId}' role='{role}' detail='{errorMessage}'.");
                }

                if (hasMatch)
                {
                    HardFailFastH1.Trigger(
                        typeof(RuntimePersistentScenesPolicyAsset),
                        $"[FATAL][Config][RuntimeMode] duplicate persistent scene role detected. owner='{normalizedOwner}' policyId='{PolicyId}' role='{role}' firstScene='{matchedSceneName}' duplicateScene='{sceneName}'.");
                }

                matchedSceneName = sceneName;
                hasMatch = true;
            }

            if (!hasMatch)
            {
                HardFailFastH1.Trigger(
                    typeof(RuntimePersistentScenesPolicyAsset),
                    $"[FATAL][Config][RuntimeMode] required persistent scene role not found. owner='{normalizedOwner}' policyId='{PolicyId}' role='{role}'.");
            }

            return matchedSceneName;
        }

        private static bool TryResolveSceneName(
            RuntimePersistentSceneEntry entry,
            string fieldName,
            out string sceneName,
            out string errorMessage)
        {
            sceneName = string.Empty;
            errorMessage = string.Empty;

            if (entry == null)
            {
                errorMessage = $"{fieldName} is required.";
                return false;
            }

            SceneKeyAsset sceneKey = entry.SceneKey;
            if (sceneKey == null)
            {
                errorMessage = $"{fieldName}.sceneKey is required.";
                return false;
            }

            sceneName = Normalize(sceneKey.SceneName);
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                errorMessage = $"{fieldName}.sceneKey requires a non-empty SceneName. asset='{sceneKey.name}'.";
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                errorMessage = $"{fieldName}.sceneKey scene is missing from Build Settings. asset='{sceneKey.name}', scene='{sceneName}'.";
                return false;
            }

            return true;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    [Serializable]
    public sealed class RuntimePersistentSceneEntry
    {
        [SerializeField] private SceneKeyAsset sceneKey;
        [SerializeField] private RuntimePersistentSceneRole role;
        [SerializeField] private bool required = true;

        public SceneKeyAsset SceneKey => sceneKey;
        public RuntimePersistentSceneRole Role => role;
        public bool Required => required;
    }

    public enum RuntimePersistentSceneRole
    {
        GlobalUi = 0,
        Fade = 1,
        Loading = 2,
        DebugOverlay = 3,
        ServiceHost = 4,
        Other = 5,
    }
}

