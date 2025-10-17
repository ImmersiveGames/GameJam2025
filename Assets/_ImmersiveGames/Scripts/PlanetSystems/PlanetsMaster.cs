using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Bind;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [RequireComponent(typeof(InjectableEntityVisualBridge))]
    [DebugLevel(DebugLevel.Logs)]
    public sealed class PlanetsMaster : ActorMaster, IPlanetActor
    {
        private InjectableEntityVisualBridge _visualBridge;

        protected override void Awake()
        {
            base.Awake();
            InitializeVisualBridge();
        }

        public InjectableEntityVisualBridge GetVisualBridge()
        {
            if (_visualBridge == null)
            {
                TryGetComponent(out _visualBridge);
            }

            return _visualBridge;
        }

        public PlanetResourcesSo GetResource() => _visualBridge?.CurrentDefinition as PlanetResourcesSo;

        public PlanetsMaster GetPlanetsMaster() => this;

        public IActor PlanetActor => this;

        private void InitializeVisualBridge()
        {
            if (!TryGetComponent(out _visualBridge))
            {
                DebugUtility.LogError<PlanetsMaster>(
                    $"InjectableEntityVisualBridge não encontrado em {gameObject.name}.",
                    this);
            }
        }
    }

    public interface IPlanetActor
    {
        IActor PlanetActor { get; }
    }
}
