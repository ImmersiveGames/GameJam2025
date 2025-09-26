namespace _ImmersiveGames.Scripts.PlanetSystems.PlanetDefenseSystem
{
    /*[DebugLevel(DebugLevel.Verbose)]
    public class DefensesHealth : HealthResource
    {
        private DefensesMaster _defensesMaster;
        private PooledObject _pooledObject;
        protected override void Awake()
        {
            base.Awake();
            _defensesMaster = GetComponent<DefensesMaster>();
            _pooledObject = GetComponent<PooledObject>();
            if (_defensesMaster == null)
            {
                DebugUtility.LogError<DefensesHealth>("DefensesHealth requires a DefensesMaster component.");
            }
        }
        private void OnTriggerEnter(Collider other)
        {
            //Para colisão com EaterMaster ou PlayerMaster
            var actor = other.GetComponentInParent<IActor>();
            if (actor is not EaterMaster && actor is not PlayerMaster) return;
            TakeDamage(10, actor); //Dano a si, pode ser ajustado conforme necessário
            var destructible = other.GetComponentInParent<IDestructible>();
            destructible?.TakeDamage(_defensesMaster.ProjectilesData.damage, _pooledObject.GetComponentInParent<IActor>());
        }
        public override void OnDeath()
        {
            base.OnDeath();
            _defensesMaster.OnDefensesDeath();
        }
    }*/
}