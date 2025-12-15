using _ImmersiveGames.Scripts.UISystems.Compass;
using UnityEngine;
namespace _ImmersiveGames.Scripts.CompassSystems
{
    /// <summary>
    /// Interface para objetos que podem ser rastreados pela bússola.
    /// Define transform, tipo e estado de atividade do alvo.
    /// </summary>
    public interface ICompassTrackable
    {
        /// <summary>
        /// Transform associado ao alvo rastreável.
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        /// Tipo de alvo utilizado para mapear aparência e comportamento na HUD.
        /// </summary>
        CompassTargetType TargetType { get; }

        /// <summary>
        /// Indica se o alvo está ativo para fins de rastreamento.
        /// </summary>
        bool IsActive { get; }
    }
}
