using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DebugLevel(DebugLevel.Logs)]
    [RequireComponent(typeof(PlanetResourceController))]
    public sealed class PlanetsMaster : ActorMaster, IPlanetActor
    {
        [SerializeField] private PlanetResourceController resourceController;

        public PlanetResourceController ResourceController => resourceController;

        protected override void Awake()
        {
            base.Awake();

            if (resourceController == null)
            {
                resourceController = GetComponent<PlanetResourceController>();
            }

            if (resourceController == null)
            {
                DebugUtility.LogError<PlanetsMaster>($"PlanetResourceController ausente em {gameObject.name}.", this);
            }
        }

        public PlanetResourcesSo GetResource() => resourceController != null ? resourceController.CurrentResource : null;

        public IActor PlanetActor => this;
    }

    public interface IPlanetActor
    {
        IActor PlanetActor { get; }
    }
}
