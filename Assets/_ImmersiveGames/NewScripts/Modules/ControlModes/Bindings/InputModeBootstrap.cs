/*
 * VALIDACAO / CHECKLIST (UIGlobalScene)
 * - Menu/UI deve funcionar via EventSystem + InputSystemUIInputModule (sem PlayerInput global).
 * - PlayerInput deve existir apenas em objetos de jogador (spawnados), especialmente pensando em multiplayer.
 */

using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.ControlModes.Bindings
{
    /// <summary>
    /// Bootstrap para registrar IInputModeService no DI global.
    ///
    /// Importante:
    /// - NÃO cria PlayerInput.
    /// - NÃO depende de InputActionAsset.
    /// - Apenas registra o serviço com os nomes dos action maps.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    [DisallowMultipleComponent]
    public sealed class InputModeBootstrap : MonoBehaviour
    {
        [Header("Action Map Names")]
        [SerializeField] private string playerMapName = "Player";
        [SerializeField] private string menuMapName = "UI";

        private static bool _initialized;

        public static void EnsureRegistered(string playerMapName, string menuMapName)
        {
            if (_initialized)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<IInputModeService>(out var existing) && existing != null)
            {
                _initialized = true;
                DebugUtility.LogVerbose(typeof(InputModeBootstrap),
                    "[InputMode] IInputModeService ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            var service = new InputModeService(playerMapName, menuMapName);
            DependencyManager.Provider.RegisterGlobal<IInputModeService>(service);

            _initialized = true;

            DebugUtility.Log(typeof(InputModeBootstrap),
                "[InputMode] IInputModeService registrado no DI global.",
                DebugUtility.Colors.Success);
        }

        private void Awake()
        {
            EnsureRegistered(playerMapName, menuMapName);
            DontDestroyOnLoad(gameObject);
        }
    }
}
