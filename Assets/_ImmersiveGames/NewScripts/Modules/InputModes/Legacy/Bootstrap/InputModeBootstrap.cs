// LEGACY: trilho legado desativado. O owner canônico de InputModes é GlobalCompositionRoot.InputModes.cs.
/*
 * VALIDACAO / CHECKLIST (UIGlobalScene)
 * - Menu/UI deve funcionar via EventSystem + InputSystemUIInputModule (sem PlayerInput global).
 * - PlayerInput deve existir apenas em objetos de jogador (spawnados), especialmente pensando em multiplayer.
 */

using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.InputModes.Bindings
{
    /// <summary>
    /// Shim legado para compatibilidade de callsite explícito.
    ///
    /// Importante:
    /// - NÂO cria PlayerInput.
    /// - NÂO depende de InputActionAsset.
    /// - NÂO faz bootstrap automático.
    /// - NÂO registra IInputModeService; o owner canônico é GlobalCompositionRoot.InputModes.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    [DisallowMultipleComponent]
    public sealed class InputModeBootstrap : MonoBehaviour
    {
        private static bool _loggedInvocation;

        public static void EnsureInstalled()
        {
            LogLegacyInvocation("EnsureInstalled");
        }

        public static void EnsureRegistered(string playerMapName, string menuMapName)
        {
            LogLegacyInvocation("EnsureRegistered");
        }

        private static void LogLegacyInvocation(string source)
        {
            if (_loggedInvocation)
            {
                return;
            }

            _loggedInvocation = true;

            if (DependencyManager.HasInstance &&
                DependencyManager.Provider.TryGetGlobal<IInputModeService>(out var existing) &&
                existing != null)
            {
                DebugUtility.Log(typeof(InputModeBootstrap),
                    $"[OBS][LEGACY][InputModes] InputModeBootstrap {source} invoked; canonical owner is GlobalCompositionRoot.InputModes.",
                    DebugUtility.Colors.Warning);
                return;
            }

            DebugUtility.LogWarning(typeof(InputModeBootstrap),
                $"[OBS][LEGACY][InputModes] InputModeBootstrap {source} invoked without canonical registration; no-op shim. owner='GlobalCompositionRoot.InputModes'.");
        }
    }
}
