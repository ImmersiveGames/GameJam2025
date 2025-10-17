using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [RequireComponent(typeof(PlanetResourceController))]
    [DebugLevel(DebugLevel.Logs)]
    public sealed class PlanetsMaster : ActorMaster, IPlanetActor
    {
        private PlanetResourceController _resourceController;

        protected override void Awake()
        {
            base.Awake();
            InitializeResourceController();
        }

        public PlanetResourceController GetResourceController()
        {
            if (_resourceController == null)
            {
                TryGetComponent(out _resourceController);
            }

            return _resourceController;
        }

        public PlanetResourcesSo GetResource() => _resourceController?.CurrentResource;

        public PlanetsMaster GetPlanetsMaster() => this;

        public IActor PlanetActor => this;

        private void InitializeResourceController()
        {
            if (!TryGetComponent(out _resourceController))
            {
                DebugUtility.LogError<PlanetsMaster>(
                    $"{nameof(PlanetResourceController)} não encontrado em {gameObject.name}.",
                    this);
            }
        }
    }

    public interface IPlanetActor
    {
        IActor PlanetActor { get; }
    }
}
