using NUnit.Framework;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense.Tests
{
    /// <summary>
    /// Verifica resolução de alvo baseada em DefenseTargetMode e cache interno.
    /// Comentários em português, código em inglês conforme padrão do projeto.
    /// </summary>
    public class TestDefenseTargeting
    {
        [Test]
        public void PreferPlayer_ShouldReturnPlayerWhenUnknownIdentifier()
        {
            var strategy = new SimplePlanetDefenseStrategy(DefenseTargetMode.PreferPlayer);

            var targetRole = strategy.ResolveTargetRole(null, DefenseRole.Unknown);

            Assert.AreEqual(DefenseRole.Player, targetRole);
            Assert.AreEqual(DefenseRole.Player, strategy.TargetRole);
        }

        [Test]
        public void PlayerOrEater_ShouldRespectRequestedRole()
        {
            var strategy = new SimplePlanetDefenseStrategy(DefenseTargetMode.PlayerOrEater);

            var explicitRole = strategy.ResolveTargetRole("ignored", DefenseRole.Eater);

            Assert.AreEqual(DefenseRole.Eater, explicitRole);
        }

        [Test]
        public void PreferEater_ShouldCacheIdentifierResolution()
        {
            var strategy = new SimplePlanetDefenseStrategy(DefenseTargetMode.PreferEater);

            var first = strategy.ResolveTargetRole("EaterBoss", DefenseRole.Unknown);
            var second = strategy.ResolveTargetRole("EaterBoss", DefenseRole.Unknown);

            Assert.AreEqual(first, second, "Role resolution should be cached for the same identifier.");
        }
    }
}
