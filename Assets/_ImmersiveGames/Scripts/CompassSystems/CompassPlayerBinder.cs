using _ImmersiveGames.Scripts.CompassSystems.Compass;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
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
            if (CompassRuntimeService.TryGet(out var runtimeService))
            {
                runtimeService.SetPlayer(transform);
            }
            else
            {
                DebugUtility.LogError<CompassPlayerBinder>("CompassRuntimeService indisponível para registrar player.");
            }
        }

        private void OnDisable()
        {
            if (CompassRuntimeService.TryGet(out var runtimeService))
            {
                runtimeService.ClearPlayer(transform);
            }
        }
    }
}
