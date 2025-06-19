using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
namespace _ImmersiveGames.Scripts.PlanetSystems.PlanetDefenseSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public class DefensesExplosions : DeathExplosionEffect
    {
        private IDestructible _destructible;
        private DefensesMaster _defensesMaster;
        private PooledObject _pooledObject;

        protected override void Awake()
        {
            base.Awake();
            TryGetComponent(out _defensesMaster);
        }
        private void OnEnable()
        {
            _defensesMaster.EventDefensesDeath += OnDeath;
        }

        private void OnDisable()
        {
            _defensesMaster.EventDefensesDeath -= OnDeath;
            DisableParticles();
        }
        private void OnDeath()
        {
            DebugUtility.Log<DefensesExplosions>("Imprimindo explosão de defesa", "yellow", this);
            EnableParticles();
        }
    }
}