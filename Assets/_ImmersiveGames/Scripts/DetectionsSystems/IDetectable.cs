using _ImmersiveGames.Scripts.PlanetSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.DetectionsSystems
{
    // Interface for entities that can detect planets (Player, EaterDetectable)
    public interface IDetectable
    {
        void OnPlanetDetected(IPlanetInteractable planetMaster); // When planetMaster enters detection range
        void OnPlanetLost(IPlanetInteractable planetMaster);    // When planetMaster exits detection range
        void OnRecognitionRangeEntered(IPlanetInteractable planetMaster, PlanetResourcesSo resources); // When in recognition range and facing planetMaster
    }

    // Interface for planets to handle interactions
    public interface IPlanetInteractable
    {
        bool IsActive { get; set; } // Indicates if the planet is active
        Transform Transform { get; }
        string Name { get; } // Planet name
        void ActivateDefenses(IDetectable entity); // Called when detected by player/EaterDetectable
        void SendRecognitionData(IDetectable entity); // Called when recognized
        PlanetResourcesSo GetResources(); // Retrieve planetMaster resources
    }
}