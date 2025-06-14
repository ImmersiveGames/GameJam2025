using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public class EaterHealth : HealthResource
    {
        private EaterMaster _eater;
        protected override void Awake()
        {
            base.Awake();
            _eater = GetComponent<EaterMaster>();
        }
        protected override void OnEnable()
        {
            base.OnEnable();
            _eater.EventConsumeResource += OnConsumeResource;
        }
        private void OnDisable()
        {
            _eater.EventConsumeResource -= OnConsumeResource;
        }
        private void OnConsumeResource(IDetectable detectable, bool desire)
        {
            if (detectable == null) return;
            float planetSize = detectable.GetPlanetsMaster().GetPlanetInfo().planetScale;
            if (desire)
            {
                float recoverResource = detectable.GetPlanetData().recoveryHungerConsumeDesire * planetSize;
                Increase(recoverResource);
                DebugUtility.Log<EaterHunger>($"Consumiu o recurso desejado: {detectable.GetResource().name} e recuperou: {recoverResource} de heath.");
            }
            else
            {
                float resourceFraction = detectable.GetPlanetData().recoveryHealthConsumeNotDesire * planetSize;
                Increase(resourceFraction); // Consome metade se não for desejado
                DebugUtility.Log<EaterHunger>($"Recurso {detectable.GetResource().name} não desejado.e recuperou: {resourceFraction} de heath.");
            }
        }
    }
}