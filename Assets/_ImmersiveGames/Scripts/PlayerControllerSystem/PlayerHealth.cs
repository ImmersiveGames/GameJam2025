using _ImmersiveGames.Scripts.EnemySystem;
using UnityEngine;
        using _ImmersiveGames.Scripts.Utils.DebugSystems;
        
        namespace _ImmersiveGames.Scripts.PlayerControllerSystem
        {
            public class PlayerHealth : DestructibleObject
            {
                [SerializeField] private PlayerHealthData healthData;
        
                private void Start()
                {
                    if (healthData != null)
                    {
                        destructibleObject = healthData;
                        base.Initialize();
                    }
                    else
                    {
                        DebugUtility.LogError<PlayerHealth>("PlayerHealthData não configurado.", this);
                    }
                }
        
                public override void TakeDamage(float damage)
                {
                    base.TakeDamage(damage);
                    DebugUtility.LogVerbose<PlayerHealth>($"Player recebeu {damage} de dano. Vida atual: {CurrentHealth}", "red", this);
        
                    if (!IsAlive)
                    {
                        Die();
                    }
                }
        
                protected override void Die()
                {
                    base.Die();
                    gameObject.SetActive(false);
                    DebugUtility.LogVerbose<PlayerHealth>("Player morreu!", "red", this);
                }
            }
        }