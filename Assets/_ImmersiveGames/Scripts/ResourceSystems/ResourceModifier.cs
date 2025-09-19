namespace _ImmersiveGames.Scripts.ResourceSystems
{
    // Modificador para buffs ou debuffs em recursos
    public class ResourceModifier
    {
        public readonly float amountPerSecond; // Quantidade por segundo (positivo para buff, negativo para debuff)
        private readonly float _duration; // Duração em segundos
        public readonly bool _isPermanent; // Se verdadeiro, dura até ser removido manualmente
        private float Timer { get; set; } // Temporizador interno

        public ResourceModifier(float amountPerSecond, float duration, bool isPermanent = false)
        {
            this.amountPerSecond = amountPerSecond;
            _duration = duration;
            _isPermanent = isPermanent;
            Timer = 0f;
        }

        // Atualiza o temporizador e retorna true se o modificador expirou
        public bool Update(float deltaTime)
        {
            if (_isPermanent) return false;
            Timer += deltaTime;
            return Timer >= _duration;
        }
    }
}