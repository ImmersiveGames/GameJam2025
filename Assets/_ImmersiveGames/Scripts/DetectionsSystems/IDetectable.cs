using _ImmersiveGames.Scripts.PlanetSystems;
namespace _ImmersiveGames.Scripts.DetectionsSystems
{
    // Interface for entities that can detect planets (Player, EaterDetectable)
    public interface IDetectable
    {
        void OnPlanetDetected(PlanetsMaster planetMaster); // When planetMaster enters detection range
        void OnPlanetLost(PlanetsMaster planetMaster);    // When planetMaster exits detection range
        void OnRecognitionRangeEntered(PlanetsMaster planetMaster, PlanetResourcesSo resources); // When in recognition range and facing planetMaster
    }

    // Interface for planets to handle interactions
    public interface IPlanetInteractable
    {
        void ActivateDefenses(IDetectable entity); // Called when detected by player/EaterDetectable
        void SendRecognitionData(IDetectable entity); // Called when recognized
        PlanetResourcesSo GetResources(); // Retrieve planetMaster resources
    }
}