using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using UnityEngine;
namespace _ImmersiveGames.Scripts.CompassSystems
{
    /// <summary>
    /// Componente que vincula o transform do jogador ao serviço de runtime da bússola.
    /// Deve ser adicionado ao GameObject do player para habilitar o rastreamento.
    /// </summary>
    public class CompassPlayerBinder : MonoBehaviour
    {
        private void OnEnable()
        {
            if (TryResolveRuntimeService(out var runtimeService))
            {
                runtimeService.SetPlayer(transform);
            }
            else
            {
                DebugUtility.LogError<CompassPlayerBinder>("ICompassRuntimeService indisponível para registrar player.");
            }
        }

        private void OnDisable()
        {
            if (TryResolveRuntimeService(out var runtimeService))
            {
                runtimeService.ClearPlayer(transform);
            }
        }

        private static bool TryResolveRuntimeService(out IICompassRuntimeService runtimeService)
        {
            runtimeService = null;
            if (DependencyManager.Provider == null)
            {
                return false;
            }

            return DependencyManager.Provider.TryGetGlobal(out runtimeService);
        }
    }
}
