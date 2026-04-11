using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime
{
    public static class PhaseContentSceneRuntimeApplier
    {
        private static readonly object StateSync = new object();
        private static readonly HashSet<string> _activeAppliedSceneNames = new HashSet<string>(StringComparer.Ordinal);
        private static PhaseDefinitionAsset _activeAppliedPhaseDefinitionRef;

        public static bool HasActiveAppliedPhaseContent
        {
            get
            {
                lock (StateSync)
                {
                    return _activeAppliedSceneNames.Count > 0;
                }
            }
        }

        public static int ActiveAppliedSceneCount
        {
            get
            {
                lock (StateSync)
                {
                    return _activeAppliedSceneNames.Count;
                }
            }
        }

        public static PhaseDefinitionAsset ActiveAppliedPhaseDefinitionRef
        {
            get
            {
                lock (StateSync)
                {
                    return _activeAppliedPhaseDefinitionRef;
                }
            }
        }

        public static IReadOnlyList<string> ActiveAppliedSceneNames
        {
            get
            {
                lock (StateSync)
                {
                    string[] scenes = _activeAppliedSceneNames.ToArray();
                    Array.Sort(scenes, StringComparer.Ordinal);
                    return scenes;
                }
            }
        }

        public static void RecordAppliedPhaseDefinition(
            PhaseDefinitionAsset phaseDefinitionRef,
            IReadOnlyList<string> appliedSceneNames,
            string activeSceneName,
            string source = "PhaseContentSceneRuntimeApplier")
        {
            if (phaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(PhaseContentSceneRuntimeApplier), "RecordAppliedPhaseDefinition phaseDefinitionRef is null.");
            }

            phaseDefinitionRef.ValidateOrFail("PhaseContentSceneRuntimeApplier/RecordApplied");
            HashSet<string> targetSceneNames = BuildSceneNameSetOrFail(appliedSceneNames, "RecordApplied");

            string normalizedActiveScene = string.IsNullOrWhiteSpace(activeSceneName) ? string.Empty : activeSceneName.Trim();

            UpdateActiveState(targetSceneNames, phaseDefinitionRef);

            EventBus<PhaseContentAppliedEvent>.Raise(new PhaseContentAppliedEvent(
                phaseDefinitionRef,
                appliedSceneNames,
                normalizedActiveScene,
                source));

            DebugUtility.Log(typeof(PhaseContentSceneRuntimeApplier),
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentReadModelCommitted owner='PhaseContentSceneRuntimeApplier' phaseId='{phaseDefinitionRef.PhaseId}' activeScenes=[{string.Join(",", targetSceneNames)}] activeScene='{normalizedActiveScene}' source='{source}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log(typeof(PhaseContentSceneRuntimeApplier),
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentApplied phaseId='{phaseDefinitionRef.PhaseId}' activeScenes=[{string.Join(",", targetSceneNames)}] activeScene='{normalizedActiveScene}'.",
                DebugUtility.Colors.Info);
        }

        public static void RecordCleared()
        {
            ClearActiveState();

            DebugUtility.Log(typeof(PhaseContentSceneRuntimeApplier),
                "[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentCleared.",
                DebugUtility.Colors.Info);
        }

        private static void UpdateActiveState(HashSet<string> sceneNames, PhaseDefinitionAsset activePhaseDefinitionRef)
        {
            lock (StateSync)
            {
                _activeAppliedSceneNames.Clear();
                foreach (string sceneName in sceneNames)
                {
                    _activeAppliedSceneNames.Add(sceneName);
                }

                _activeAppliedPhaseDefinitionRef = activePhaseDefinitionRef;
            }
        }

        private static void ClearActiveState()
        {
            lock (StateSync)
            {
                _activeAppliedSceneNames.Clear();
                _activeAppliedPhaseDefinitionRef = null;
            }
        }

        private static HashSet<string> BuildSceneNameSetOrFail(IReadOnlyList<string> sceneNames, string sourceLabel)
        {
            HashSet<string> set = new HashSet<string>(StringComparer.Ordinal);
            if (sceneNames == null)
            {
                HardFailFastH1.Trigger(typeof(PhaseContentSceneRuntimeApplier), $"{sourceLabel} scene list is null.");
            }

            for (int i = 0; i < sceneNames.Count; i++)
            {
                string normalized = string.IsNullOrWhiteSpace(sceneNames[i]) ? string.Empty : sceneNames[i].Trim();
                if (string.IsNullOrWhiteSpace(normalized))
                {
                    HardFailFastH1.Trigger(typeof(PhaseContentSceneRuntimeApplier), $"{sourceLabel} scene name is empty at index={i}.");
                }

                if (!set.Add(normalized))
                {
                    HardFailFastH1.Trigger(typeof(PhaseContentSceneRuntimeApplier), $"{sourceLabel} scene name is duplicated: '{normalized}'.");
                }
            }

            if (set.Count == 0)
            {
                HardFailFastH1.Trigger(typeof(PhaseContentSceneRuntimeApplier), $"{sourceLabel} scene list is empty.");
            }

            return set;
        }
    }

    public readonly struct PhaseContentAppliedEvent : IEvent
    {
        public PhaseContentAppliedEvent(
            PhaseDefinitionAsset phaseDefinitionRef,
            IReadOnlyList<string> appliedSceneNames,
            string activeSceneName,
            string source)
        {
            PhaseDefinitionRef = phaseDefinitionRef;
            AppliedSceneNames = appliedSceneNames;
            ActiveSceneName = string.IsNullOrWhiteSpace(activeSceneName) ? string.Empty : activeSceneName.Trim();
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
        }

        public PhaseDefinitionAsset PhaseDefinitionRef { get; }
        public IReadOnlyList<string> AppliedSceneNames { get; }
        public string ActiveSceneName { get; }
        public string Source { get; }
    }
}
