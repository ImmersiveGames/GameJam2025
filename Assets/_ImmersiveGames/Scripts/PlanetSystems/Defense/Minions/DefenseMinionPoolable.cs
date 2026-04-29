using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ProjectilesSystems;
using ImmersiveGames.GameJam2025.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems.Defense.Minions
{
    /// <summary>
    /// Vers�o especializada do BulletPoolable para minions de defesa.
    ///
    /// Reaproveita:
    /// - Rigidbody / velocidade
    /// - DamageDealer / colis�o / retorno ao pool
    /// - LifetimeManager
    /// </summary>
    public sealed class DefenseMinionPoolable : BulletPoolable
    {
        protected override void OnActivated(Vector3 pos, Vector3? direction, IActor spawner)
        {
            // Para minions, normalmente vamos controlar o movimento via
            // Entry/Chase (DOTween ou l�gica pr�pria), ent�o:
            // - chamamos base.OnActivated(pos, null, spawner) para N�O dar velocidade de bullet.
            base.OnActivated(pos, null, spawner);

            DebugUtility.LogVerbose<DefenseMinionPoolable>(
                $"[Poolable] OnActivated em '{name}' | pos={pos} | spawner={(spawner != null ? spawner.ActorName : "null")}.",
                null,this);
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();

            // Garantir que nenhum estado de persegui��o ou refer�ncia residual permane�a ap�s a desativa��o
            if (TryGetComponent<DefenseMinionController>(out var controller))
            {
                controller.CleanupOnDeactivated();
            }
        }
    }
}

