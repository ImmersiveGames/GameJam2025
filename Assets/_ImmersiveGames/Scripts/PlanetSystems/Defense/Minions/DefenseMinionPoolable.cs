using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ProjectilesSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Versão especializada do BulletPoolable para minions de defesa.
    ///
    /// Reaproveita:
    /// - Rigidbody / velocidade
    /// - DamageDealer / colisão / retorno ao pool
    /// - LifetimeManager
    /// </summary>
    public sealed class DefenseMinionPoolable : BulletPoolable
    {
        protected override void OnActivated(Vector3 pos, Vector3? direction, IActor spawner)
        {
            // Para minions, normalmente vamos controlar o movimento via
            // Entry/Chase (DOTween ou lógica própria), então:
            // - chamamos base.OnActivated(pos, null, spawner) para NÃO dar velocidade de bullet.
            base.OnActivated(pos, null, spawner);

            DebugUtility.LogVerbose<DefenseMinionPoolable>(
                $"[Poolable] OnActivated em '{name}' | pos={pos} | spawner={(spawner != null ? spawner.ActorName : "null")}.",
                null,this);
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();

            // Garantir que nenhum estado de perseguição ou referência residual permaneça após a desativação
            if (TryGetComponent<DefenseMinionController>(out var controller))
            {
                controller.CleanupOnDeactivated();
            }
        }
    }
}
