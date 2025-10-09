using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem.Tests
{
    public partial class DamageSystemDebugger
    {
        [ContextMenu("Dealer/Test Damage In Front")]
        private void TestDamageInFront()
        {
            if (_dealer == null)
            {
                Debug.LogWarning("⚔️ Nenhum DamageDealer encontrado.");
                return;
            }

            if (Physics.Raycast(_dealer.transform.position, _dealer.transform.forward, out var hit, 10f, _dealer.DamageableLayers.value))
            {
                var target = hit.collider.GetComponent<IDamageable>();
                if (target != null)
                {
                    target.ReceiveDamage(_dealer.DamageAmount, _dealer.Actor, _dealer.DamageResourceType);
                    DebugUtility.LogVerbose<DamageSystemDebugger>($"⚡ Dano aplicado em {target.Actor?.ActorName ?? hit.collider.name}");
                }
            }
            else
            {
                DebugUtility.LogVerbose<DamageSystemDebugger>("🔍 Nenhum alvo atingido à frente.");
            }
        }

        [ContextMenu("Dealer/Set Damage To 50")]
        private void SetDamageTo50()
        {
            if (_dealer == null)
            {
                Debug.LogWarning("⚔️ Nenhum DamageDealer encontrado.");
                return;
            }

            _dealer.SetDamage(50f);
            DebugUtility.LogVerbose<DamageSystemDebugger>($"⚡ Dano configurado para 50 em {_dealer.Actor?.ActorName ?? gameObject.name}");
        }
    }
}