namespace _ImmersiveGames.Scripts.ResourceSystems
{
    // Modificador para buffs ou debuffs em recursos
    public class ResourceModifier
    {
        public float AmountPerSecond; // Quantidade por segundo (positivo para buff, negativo para debuff)
        public float Duration; // Duração em segundos
        public bool IsPermanent; // Se verdadeiro, dura até ser removido manualmente
        public float Timer { get; private set; } // Temporizador interno

        public ResourceModifier(float amountPerSecond, float duration, bool isPermanent = false)
        {
            AmountPerSecond = amountPerSecond;
            Duration = duration;
            IsPermanent = isPermanent;
            Timer = 0f;
        }

        // Atualiza o temporizador e retorna true se o modificador expirou
        public bool Update(float deltaTime)
        {
            if (IsPermanent) return false;
            Timer += deltaTime;
            return Timer >= Duration;
        }
    }
}