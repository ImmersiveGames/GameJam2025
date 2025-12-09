#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using _ImmersiveGames.Scripts.SceneManagement.Configs;
using UnityEditor;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SceneManagement.Editor
{
    /// <summary>
    /// Ferramentas de debug/inspeção para SceneFlowMap + SceneGroupProfile.
    /// 
    /// - Lista todos os grupos e suas cenas.
    /// - Valida se as cenas existem no Build Settings.
    /// 
    /// Não é usada em runtime; apenas em editor.
    /// </summary>
    public static class SceneFlowDebugTools
    {
        private const string MenuRoot = "ImmersiveGames/Scene Flow/";

        [MenuItem(MenuRoot + "Listar grupos e cenas (SceneFlowMap)")]
        private static void PrintSceneGroupsSummary()
        {
            var maps = LoadAllSceneFlowMaps();
            if (maps.Count == 0)
            {
                Debug.LogWarning("[SceneFlowDebugTools] Nenhum SceneFlowMap encontrado no projeto.");
                return;
            }

            foreach (var map in maps)
            {
                Debug.Log($"[SceneFlowDebugTools] --- SceneFlowMap: {map.name} ---");
                PrintNamedGroups(map);
            }
        }

        [MenuItem(MenuRoot + "Validar cenas em Build Settings")]
        private static void ValidateScenesAgainstBuildSettings()
        {
            var maps = LoadAllSceneFlowMaps();
            if (maps.Count == 0)
            {
                Debug.LogWarning("[SceneFlowDebugTools] Nenhum SceneFlowMap encontrado no projeto.");
                return;
            }

            var buildScenes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (s == null || string.IsNullOrEmpty(s.path) || !s.enabled)
                    continue;

                var sceneName = Path.GetFileNameWithoutExtension(s.path);
                if (!string.IsNullOrEmpty(sceneName))
                    buildScenes.Add(sceneName);
            }

            foreach (var map in maps)
            {
                Debug.Log($"[SceneFlowDebugTools] Validando SceneFlowMap '{map.name}' contra Build Settings...");

                foreach (var (key, group) in EnumerateGroups(map))
                {
                    if (group == null)
                    {
                        Debug.LogWarning(
                            $"[SceneFlowDebugTools] Chave '{key}' em '{map.name}' aponta para um SceneGroupProfile nulo.");
                        continue;
                    }

                    // sceneNames via reflection (para não depender da visibilidade no script).
                    var sceneNames = GetSceneNamesFromGroup(group);
                    if (sceneNames == null || sceneNames.Count == 0)
                    {
                        Debug.LogWarning(
                            $"[SceneFlowDebugTools] Grupo '{group.name}' (key='{key}') não possui cenas configuradas.");
                        continue;
                    }

                    foreach (var sceneName in sceneNames)
                    {
                        if (string.IsNullOrWhiteSpace(sceneName))
                            continue;

                        if (!buildScenes.Contains(sceneName))
                        {
                            Debug.LogWarning(
                                $"[SceneFlowDebugTools] Grupo '{group.name}' (key='{key}') referencia cena '{sceneName}' " +
                                "que NÃO está presente (ou habilitada) em Build Settings.");
                        }
                    }
                }
            }

            Debug.Log("[SceneFlowDebugTools] Validação concluída.");
        }

        // ------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------

        private static List<SceneFlowMap> LoadAllSceneFlowMaps()
        {
            var result = new List<SceneFlowMap>();
            var guids = AssetDatabase.FindAssets("t:SceneFlowMap");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<SceneFlowMap>(path);
                if (asset != null)
                    result.Add(asset);
            }

            return result;
        }

        private static void PrintNamedGroups(SceneFlowMap map)
        {
            foreach (var (key, group) in EnumerateGroups(map))
            {
                if (group == null)
                {
                    Debug.LogWarning(
                        $"[SceneFlowDebugTools] Chave '{key}' em '{map.name}' está com SceneGroupProfile nulo.");
                    continue;
                }

                var sceneNames = GetSceneNamesFromGroup(group);
                var scenesText = sceneNames != null && sceneNames.Count > 0
                    ? string.Join(", ", sceneNames)
                    : "<nenhuma cena>";

                Debug.Log(
                    $"[SceneFlowDebugTools] key='{key}' → group='{group.name}' " +
                    $"(Active='{GetActiveSceneName(group) ?? "<não definido>"}') " +
                    $"Cenas=[{scenesText}]");
            }
        }

        /// <summary>
        /// Percorre os entries de namedGroups em SceneFlowMap via reflection.
        /// Isso evita acoplamento forte a tipos internos/privates.
        /// </summary>
        private static IEnumerable<(string key, SceneGroupProfile group)> EnumerateGroups(SceneFlowMap map)
        {
            if (map == null)
                yield break;

            var mapType = typeof(SceneFlowMap);
            var fiNamedGroups = mapType.GetField("namedGroups",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (fiNamedGroups == null)
            {
                Debug.LogWarning(
                    $"[SceneFlowDebugTools] Campo 'namedGroups' não encontrado em SceneFlowMap ({map.name}).");
                yield break;
            }

            var value = fiNamedGroups.GetValue(map) as IEnumerable;
            if (value == null)
                yield break;

            foreach (var entry in value)
            {
                if (entry == null)
                    continue;

                var entryType = entry.GetType();
                var fiKey = entryType.GetField("key",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                var fiGroup = entryType.GetField("group",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (fiKey == null || fiGroup == null)
                    continue;

                var keyObj = fiKey.GetValue(entry);
                var groupObj = fiGroup.GetValue(entry);

                var key = keyObj as string;
                var group = groupObj as SceneGroupProfile;

                if (string.IsNullOrWhiteSpace(key))
                    continue;

                yield return (key, group);
            }
        }

        private static List<string> GetSceneNamesFromGroup(SceneGroupProfile group)
        {
            if (group == null)
                return null;

            var groupType = typeof(SceneGroupProfile);
            var fiScenes = groupType.GetField("sceneNames",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (fiScenes == null)
                return null;

            var value = fiScenes.GetValue(group) as IList;
            if (value == null)
                return null;

            var result = new List<string>();
            foreach (var item in value)
            {
                if (item is string s && !string.IsNullOrWhiteSpace(s))
                    result.Add(s);
            }

            return result;
        }

        private static string GetActiveSceneName(SceneGroupProfile group)
        {
            if (group == null)
                return null;

            var groupType = typeof(SceneGroupProfile);
            var fiActive = groupType.GetField("activeSceneName",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (fiActive == null)
                return null;

            return fiActive.GetValue(group) as string;
        }
    }
}
#endif
