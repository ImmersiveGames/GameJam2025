using _ImmersiveGames.Scripts.PlanetSystems;
namespace _ImmersiveGames.Scripts.DetectionsSystems
{
    // Interface for entities that can detect planets (Player, EaterDetectable)
    public interface IDetectable
    {
        void OnPlanetDetected(Planets planet); // When planet enters detection range
        void OnPlanetLost(Planets planet);    // When planet exits detection range
        void OnRecognitionRangeEntered(Planets planet, PlanetResourcesSo resources); // When in recognition range and facing planet
    }

    // Interface for planets to handle interactions
    public interface IPlanetInteractable
    {
        void ActivateDefenses(IDetectable entity); // Called when detected by player/EaterDetectable
        void SendRecognitionData(IDetectable entity); // Called when recognized
        PlanetResourcesSo GetResources(); // Retrieve planet resources
    }
}