using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.Validation
{
    public static class TransitionStyleProfileRefMigrator
    {
        private const string TransitionStyleCatalogPath = "Assets/Resources/Navigation/TransitionStyleCatalog.asset";
        private const string TransitionProfileCatalogPath = "Assets/Resources/SceneFlow/SceneTransitionProfileCatalog.asset";

        [MenuItem("ImmersiveGames/NewScripts/Config/Migrate TransitionStyles ProfileRef (DataCleanup v1)", priority = 3030)]
        public static void Migrate()
        {
            TransitionStyleCatalogAsset styleCatalog = AssetDatabase.LoadAssetAtPath<TransitionStyleCatalogAsset>(TransitionStyleCatalogPath);
            SceneTransitionProfileCatalogAsset profileCatalog = AssetDatabase.LoadAssetAtPath<SceneTransitionProfileCatalogAsset>(TransitionProfileCatalogPath);

            if (styleCatalog == null)
            {
                throw new InvalidOperationException($"[FATAL][Config] Missing canonical asset: '{TransitionStyleCatalogPath}'.");
            }

            if (profileCatalog == null)
            {
                throw new InvalidOperationException($"[FATAL][Config] Missing canonical asset: '{TransitionProfileCatalogPath}'.");
            }

            SerializedObject serializedCatalog = new SerializedObject(styleCatalog);
            SerializedProperty styles = serializedCatalog.FindProperty("styles");

            if (styles == null || !styles.isArray)
            {
                throw new InvalidOperationException("[FATAL][Config] TransitionStyleCatalog has no serialized 'styles' array.");
            }

            int totalStyles = styles.arraySize;
            int filledCount = 0;
            int skippedAlreadySet = 0;
            int failedCount = 0;
            List<string> failedStyleIds = new List<string>();

            for (int i = 0; i < styles.arraySize; i++)
            {
                SerializedProperty styleEntry = styles.GetArrayElementAtIndex(i);
                string styleId = ReadTypedIdValue(styleEntry.FindPropertyRelative("styleId"));
                SceneFlowProfileId profileId = SceneFlowProfileId.FromName(ReadTypedIdValue(styleEntry.FindPropertyRelative("profileId")));

                SerializedProperty writableProfileRef = GetWritableProfileReference(styleEntry);
                SerializedProperty readableProfileRef = GetReadableProfileReference(styleEntry);

                if (readableProfileRef != null && readableProfileRef.objectReferenceValue != null)
                {
                    skippedAlreadySet++;
                    continue;
                }

                if (!profileId.IsValid)
                {
                    failedCount++;
                    AddFailedStyleId(failedStyleIds, styleId, i);
                    continue;
                }

                if (!profileCatalog.TryGetProfile(profileId, out SceneTransitionProfile profile) || profile == null)
                {
                    failedCount++;
                    AddFailedStyleId(failedStyleIds, styleId, i);
                    continue;
                }

                if (writableProfileRef == null)
                {
                    failedCount++;
                    AddFailedStyleId(failedStyleIds, styleId, i);
                    continue;
                }

                // Mantém compatibilidade com assets legados sem mexer no runtime.
                writableProfileRef.objectReferenceValue = profile;
                filledCount++;
            }

            if (filledCount > 0)
            {
                serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(styleCatalog);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log(
                $"[OBS][Config] TransitionStyle profileRef migration finished. totalStyles={totalStyles}, filledCount={filledCount}, skippedAlreadySet={skippedAlreadySet}, failedCount={failedCount}.");

            if (failedCount > 0)
            {
                string failedIdsSummary = string.Join(", ", failedStyleIds.GetRange(0, Math.Min(10, failedStyleIds.Count)));
                throw new InvalidOperationException(
                    $"[FATAL][Config] TransitionStyle profileRef migration failed for {failedCount} style(s). Check Console. failedStyleIds={failedIdsSummary}");
            }
        }

        private static SerializedProperty GetReadableProfileReference(SerializedProperty styleEntry)
        {
            SerializedProperty profileRef = styleEntry.FindPropertyRelative("profileRef");
            if (profileRef != null)
            {
                return profileRef;
            }

            return styleEntry.FindPropertyRelative("transitionProfile");
        }

        private static SerializedProperty GetWritableProfileReference(SerializedProperty styleEntry)
        {
            SerializedProperty profileRef = styleEntry.FindPropertyRelative("profileRef");
            if (profileRef != null)
            {
                return profileRef;
            }

            return styleEntry.FindPropertyRelative("transitionProfile");
        }

        private static string ReadTypedIdValue(SerializedProperty typedIdProperty)
        {
            if (typedIdProperty == null)
            {
                return string.Empty;
            }

            SerializedProperty rawValue = typedIdProperty.FindPropertyRelative("_value");
            if (rawValue == null)
            {
                return string.Empty;
            }

            return string.IsNullOrWhiteSpace(rawValue.stringValue)
                ? string.Empty
                : rawValue.stringValue.Trim();
        }

        private static void AddFailedStyleId(ICollection<string> failedStyleIds, string styleId, int index)
        {
            if (!string.IsNullOrWhiteSpace(styleId))
            {
                failedStyleIds.Add(styleId);
                return;
            }

            failedStyleIds.Add($"<index:{index}>");
        }
    }
}
