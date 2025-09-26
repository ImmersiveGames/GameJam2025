namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    /*public class PlayerHealth : HealthResource
    {
        private PlayerMaster _playerMaster;
        protected override void Awake()
        {
            base.Awake();
            _playerMaster = GetComponent<PlayerMaster>();
            if (_playerMaster == null)
            {
                DebugUtility.LogError<PlayerHealth>("PlayerMaster não encontrado no Actor.");
            }
        }

        public override void TakeDamage(float damage,IActor byActor)
        {
            base.TakeDamage(damage,byActor);
            _playerMaster.OnEventPlayerTakeDamage(byActor);
        }
    }*/
}