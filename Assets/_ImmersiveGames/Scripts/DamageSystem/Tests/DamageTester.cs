// File: _ImmersiveGames/Scripts/Utils/Test/DamageTester.cs

using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DamageSystem.Services;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.DamageSystem.Tests
{
    public class DamageTester : MonoBehaviour
    {
        [Inject] private DamageService _damageService;

        public ActorMaster sourceActor;
        public ActorMaster targetActor;

        [ContextMenu("Test Damage")]
        public void TestDamage()
        {
            if (_damageService == null)
            {
                Debug.LogError("DamageService not injected.");
                return;
            }

            var ctx = new DamageContext
            {
                Source = sourceActor,
                Target = targetActor,
                Amount = 25f,
                DamageType = DamageType.Physical,
                HitPosition = targetActor != null ? targetActor.transform.position : transform.position
            };

            _damageService.ApplyDamage(ctx);
            Debug.Log("Damage applied via DamageService test.");
        }

        private void OnEnable()
        {
            if (DependencyManager.Instance != null)
                DependencyManager.Instance.InjectDependencies(this);
        }
    }
}