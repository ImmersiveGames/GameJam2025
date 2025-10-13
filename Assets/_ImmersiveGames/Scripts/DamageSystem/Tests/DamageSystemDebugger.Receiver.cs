using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem.Tests
{
    public partial class DamageSystemDebugger
    {
        [Header("Debug Test Values")]
        [SerializeField, Min(0f)] private float testDamage = 10f;
        [SerializeField, Min(0f)] private float reviveHealth = 50f;
        [SerializeField] private ResourceType testResource = ResourceType.Health;
        
        [ContextMenu("Receiver/Test Receive Damage")]
        private void TestReceiveDamage()
        {
            if (_receiver == null)
            {
                DebugUtility.LogWarning<DamageSystemDebugger>("⚠️ Nenhum DamageReceiver encontrado.");
                return;
            }

            if (_receiver.IsDead)
            {
                DebugUtility.LogVerbose<DamageSystemDebugger>($"🚫 {GetObjectName()} está morto. Não é possível aplicar dano.");
                return;
            }

            _receiver.ReceiveDamage(testDamage, null, testResource);
            DebugUtility.LogVerbose<DamageSystemDebugger>($"🎯 Dano aplicado ({testDamage}) em {GetObjectName()}");
        }

        [ContextMenu("Receiver/Kill Object")]
        private void KillObject()
        {
            if (_receiver == null)
            {
                DebugUtility.LogWarning<DamageSystemDebugger>("⚠️ Nenhum DamageReceiver encontrado.");
                return;
            }

            _receiver.KillImmediately();
            DebugUtility.LogVerbose<DamageSystemDebugger>($"💀 {GetObjectName()} morto instantaneamente.");
        }

        [ContextMenu("Receiver/Revive Object")]
        private void ReviveObject()
        {
            if (_receiver == null)
            {
                DebugUtility.LogWarning<DamageSystemDebugger>("⚠️ Nenhum DamageReceiver encontrado.");
                return;
            }

            _receiver.Revive(reviveHealth);
            DebugUtility.LogVerbose<DamageSystemDebugger>($"✨ {GetObjectName()} revivido com {reviveHealth} HP.");
        }

        [ContextMenu("Receiver/Check Health Status")]
        private void CheckHealth()
        {
            if (_receiver == null) return;
            DebugUtility.LogVerbose<DamageSystemDebugger>($"❤️ {GetObjectName()} — HP: {_receiver.CurrentHealth}, Dead: {_receiver.IsDead}");
        }
    }
}