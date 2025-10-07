using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _ImmersiveGames.Scripts.DamageSystem.Tests
{
    public partial class DamageSystemDebugger
    {
        [ContextMenu("System/Print Status")]
        private void PrintStatus()
        {
            Debug.Log("=== DAMAGE SYSTEM STATUS ===");

            if (_receiver != null)
                Debug.Log($"📋 Receiver: {GetObjectName()} (HP: {_receiver.CurrentHealth}, Dead: {_receiver.IsDead})");
            else
                Debug.Log("📋 Receiver: None");

            if (_dealer != null)
                Debug.Log($"⚔️ Dealer: {GetObjectName()} (Damage: {_dealer.DamageAmount})");
            else
                Debug.Log("⚔️ Dealer: None");

            if (_audio != null)
                Debug.Log("🔊 AudioController ativo.");
        }

        private void OnDrawGizmosSelected()
        {
            if (!showVisualDebug) return;

            if (_receiver != null)
            {
                Gizmos.color = _receiver.IsDead ? Color.red : Color.green;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);

#if UNITY_EDITOR
                Handles.Label(transform.position + Vector3.up * 3f, 
                    $"Health: {_receiver.CurrentHealth}\nDead: {_receiver.IsDead}");
#endif
            }

            if (_dealer != null)
            {
                Gizmos.color = debugColor;
                Gizmos.DrawRay(transform.position, transform.forward * 3f);
                Gizmos.DrawWireSphere(transform.position, 0.3f);
            }
        }
    }
}