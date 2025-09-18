using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.EaterSystem.Events;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
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
        private void OnConsumeResource(IDetectable detectable, bool desire, IActor byActor)
        {
            if (detectable == null || byActor is not EaterMaster eater) return;
            float planetSize = detectable.GetPlanetsMaster().GetPlanetInfo().planetScale;
            if (desire)
            {
                float recoverResource = detectable.GetPlanetData().recoveryHungerConsumeDesire * planetSize;
                Heal(recoverResource, eater);
                DebugUtility.Log<EaterHunger>($"Consumiu o recurso desejado: {detectable.GetResource().name} e recuperou: {recoverResource} de heath.");
            }
            else
            {
                float resourceFraction = detectable.GetPlanetData().recoveryHealthConsumeNotDesire * planetSize;
                Heal(resourceFraction, eater); // Consome metade se não for desejado
                DebugUtility.Log<EaterHunger>($"Recurso {detectable.GetResource().name} não desejado.e recuperou: {resourceFraction} de heath.");
            }
        }
        public override void TakeDamage(float damage, IActor byActor)
        {
            base.TakeDamage(damage, byActor);
            lastChanger = byActor;
            _eater.OnEventEaterTakeDamage(byActor);
        }

        public override void OnDeath()
        {
            EventBus<EaterDeathEvent>.Raise(new EaterDeathEvent());
            _eater.IsActive = false;
        }
    }
}