namespace _ImmersiveGames.Scripts.DamageSystem
{
    /// <summary>
    /// Estratégia simples — aplica o valor puro sem modificadores.
    /// </summary>
    public class BasicDamageStrategy : IDamageStrategy
    {
        public float CalculateDamage(DamageContext ctx)
        {
            return ctx.DamageValue;
        }
    }
}