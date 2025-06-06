using _ImmersiveGames.Scripts.ActorSystems;
namespace _ImmersiveGames.Scripts.EaterSystem
{
    public class EaterMaster: ActorMaster
    {
        private bool _inHungry;
        private bool _isEating;
        private bool _isChasing;
        
        public bool InHungry
        {
            get => _inHungry;
            set => _inHungry = value;
        }
        public bool IsEating => _isEating;
        public bool IsChasing
        {
            get => _isChasing;
            set => _isChasing = value;
        }
        public override void Reset()
        {
            _isEating = false;
            _isChasing = false;
            _inHungry = false;
        }
    }
}