/*
 * VALIDACAO / CHECKLIST (UIGlobalScene)
 * - Menu/UI deve funcionar via EventSystem + InputSystemUIInputModule (sem PlayerInput global).
 * - PlayerInput deve existir apenas em objetos de jogador (spawnados), especialmente pensando em multiplayer.
 */

using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.InputModes.Bindings
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

            var config = ResolveRuntimeConfig();
            if (config != null)
            {
                var settings = config.inputModes;
                if (!settings.enableInputModes)
                {
                    DebugUtility.LogVerbose(typeof(InputModeBootstrap),
                        "[InputMode] InputModes desabilitado via RuntimeModeConfig; bootstrap ignorado.",
                        DebugUtility.Colors.Info);
                    ReportInputModesDegraded("disabled_by_config",
                        "InputModes disabled by RuntimeModeConfig (bootstrap).");
                    _initialized = true;
                    return;
                }

                playerMapName = settings.playerActionMapName;
                menuMapName = settings.menuActionMapName;
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

        private static RuntimeModeConfig ResolveRuntimeConfig()
        {
            if (!DependencyManager.HasInstance)
            {
                return null;
            }

            return DependencyManager.Provider.TryGetGlobal<RuntimeModeConfig>(out var config) ? config : null;
        }

        private static void ReportInputModesDegraded(string reason, string detail)
        {
            if (!DependencyManager.HasInstance)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IDegradedModeReporter>(out var reporter) || reporter == null)
            {
                return;
            }

            reporter.Report(DegradedKeys.Feature.InputModes, reason, detail);
        }
    }
}
