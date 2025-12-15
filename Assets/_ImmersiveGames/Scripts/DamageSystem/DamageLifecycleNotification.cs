using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Core;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Services;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    /// <summary>
    /// Representa a projeção de um evento de ciclo de vida após qualquer alteração no recurso monitorado.
    /// Reúne as informações necessárias para sincronizar feedbacks visuais, sonoros e lógicos.
    /// </summary>
    internal readonly struct DamageLifecycleNotification
    {
        public RuntimeAttributeChangeContext Change { get; }
        public DamageContext Request { get; }
        public bool DeathStateChanged { get; }
        public bool IsDead { get; }

        public bool IsDamage => Change.IsDecrease;

        public DamageLifecycleNotification(
            RuntimeAttributeChangeContext change,
            DamageContext request,
            bool deathStateChanged,
            bool isDead)
        {
            Change = change;
            Request = request;
            DeathStateChanged = deathStateChanged;
            IsDead = isDead;
        }
    }
}
