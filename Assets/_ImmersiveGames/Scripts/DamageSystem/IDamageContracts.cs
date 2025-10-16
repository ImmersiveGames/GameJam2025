namespace _ImmersiveGames.Scripts.DamageSystem
{
    public enum DamageType
    {
        Physical,
        Fire,
        Ice,
        Poison,
        Pure
    }

    public interface IDamageDealer
    {
        void DealDamage(IDamageReceiver target, DamageContext ctx);
    }

    public interface IDamageReceiver
    {
        void ReceiveDamage(DamageContext ctx);
        string GetReceiverId();
    }

    public interface IDamageStrategy
    {
        float CalculateDamage(DamageContext ctx);
    }
}