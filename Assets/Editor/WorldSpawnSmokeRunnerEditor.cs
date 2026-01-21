using UnityEditor;
using UnityEngine;
using _ImmersiveGames.Tools;
using System.Threading.Tasks;

[CustomEditor(typeof(WorldSpawnSmokeRunner))]
public class WorldSpawnSmokeRunnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var runner = (WorldSpawnSmokeRunner)target;

        EditorGUILayout.Space();

        if (GUILayout.Button("Spawn All (smoke)"))
        {
            // Call async method - use fire-and-forget
            _ = CallSpawn(runner);
        }

        if (GUILayout.Button("Despawn All (smoke)"))
        {
            _ = CallDespawn(runner);
        }
    }

    [MenuItem("GameObject/ImmersiveGames/Smoke Runner/Spawn All", false, 10)]
    private static void MenuSpawn()
    {
        var runner = Selection.activeGameObject?.GetComponent<WorldSpawnSmokeRunner>();
        if (runner == null)
        {
            EditorUtility.DisplayDialog("Smoke Runner", "Select a GameObject with WorldSpawnSmokeRunner component.", "OK");
            return;
        }

        _ = CallSpawn(runner);
    }

    [MenuItem("GameObject/ImmersiveGames/Smoke Runner/Despawn All", false, 10)]
    private static void MenuDespawn()
    {
        var runner = Selection.activeGameObject?.GetComponent<WorldSpawnSmokeRunner>();
        if (runner == null)
        {
            EditorUtility.DisplayDialog("Smoke Runner", "Select a GameObject with WorldSpawnSmokeRunner component.", "OK");
            return;
        }

        _ = CallDespawn(runner);
    }

    private static async Task CallSpawn(WorldSpawnSmokeRunner runner)
    {
        if (runner == null) return;
        await runner.SpawnAllAsync();
        EditorUtility.SetDirty(runner);
    }

    private static async Task CallDespawn(WorldSpawnSmokeRunner runner)
    {
        if (runner == null) return;
        await runner.DespawnAllAsync();
        EditorUtility.SetDirty(runner);
    }
}
