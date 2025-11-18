using UnityEngine;

namespace _ImmersiveGames.Scripts.World.Compass
{
    /// <summary>
    /// Componente que vincula o transform do jogador ao serviço de runtime da bússola.
    /// Deve ser adicionado ao GameObject do player para habilitar o rastreamento.
    /// </summary>
    public class CompassPlayerBinder : MonoBehaviour
    {
        private void OnEnable()
        {
            CompassRuntimeService.SetPlayer(transform);
        }

        private void OnDisable()
        {
            CompassRuntimeService.ClearPlayer(transform);
        }
    }
}
