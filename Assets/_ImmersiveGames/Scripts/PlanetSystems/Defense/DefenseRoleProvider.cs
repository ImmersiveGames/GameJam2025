using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Optional component to explicitly assign the defense role for detectors or actors.
    /// Keeps multiplayer-friendly configuration without string-based heuristics.
    /// </summary>
    [DisallowMultipleComponent]
    public class DefenseRoleProvider : MonoBehaviour, IDefenseRoleProvider
    {
        [SerializeField] private DefenseRole role = DefenseRole.Player;

        public DefenseRole GetDefenseRole()
        {
            return role;
        }
    }
}
