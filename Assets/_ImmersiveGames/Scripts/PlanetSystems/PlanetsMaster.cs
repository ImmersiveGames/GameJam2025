using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [RequireComponent(typeof(PlanetResourceSystemBridge))]
    [DebugLevel(DebugLevel.Logs)]
    public sealed class PlanetsMaster : ActorMaster, IPlanetActor
    {
        private PlanetResourceSystemBridge _resourceBridge;

        protected override void Awake()
        {
            base.Awake();
            InitializeResourceBridge();
        }

        public PlanetResourceSystemBridge GetResourceBridge()
        {
            if (_resourceBridge == null)
            {
                TryGetComponent(out _resourceBridge);
            }

            return _resourceBridge;
        }

        public PlanetResourcesSo GetResource() => _resourceBridge?.CurrentResource;

        public PlanetsMaster GetPlanetsMaster() => this;

        public IActor PlanetActor => this;

        private void InitializeResourceBridge()
        {
            if (!TryGetComponent(out _resourceBridge))
            {
                DebugUtility.LogError<PlanetsMaster>(
                    $"PlanetResourceSystemBridge não encontrado em {gameObject.name}.",
                    this);
            }
        }
    }

    public interface IPlanetActor
    {
        IActor PlanetActor { get; }
    }
}
