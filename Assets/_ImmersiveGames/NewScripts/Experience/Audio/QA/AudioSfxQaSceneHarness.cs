using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Audio.QA
{
    /// <summary>
    /// Shim legado para orientar migração do QA antigo para harnesses separados.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/NewScripts/Audio/QA/Legacy SFX Harness Shim")]
    public sealed class AudioSfxQaSceneHarness : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour directHarness;
        [SerializeField] private MonoBehaviour pooledHarness;

        [ContextMenu("QA/Audio/SFX/Legacy/Log Migration Hint")]
        private void LogMigrationHint()
        {
            DebugUtility.LogWarning(typeof(AudioSfxQaSceneHarness),
                "[QA][Audio][SFX] Legacy harness shim in use. Configure 'AudioSfxDirectQaSceneHarness' and 'AudioSfxPooledQaSceneHarness' on dedicated GameObjects.");
        }

        [ContextMenu("QA/Audio/SFX/Legacy/Log Linked Harnesses")]
        private void LogLinkedHarnesses()
        {
            DebugUtility.Log(typeof(AudioSfxQaSceneHarness),
                $"[QA][Audio][SFX] directHarness='{(directHarness != null ? directHarness.name : "null")}' pooledHarness='{(pooledHarness != null ? pooledHarness.name : "null")}'.",
                DebugUtility.Colors.Info);
        }
    }
}
