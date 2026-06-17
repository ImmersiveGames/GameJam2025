using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;

namespace _ImmersiveGames.NewScripts.Actors.Projectile.Contracts
{
    public interface IActorRuntimePoolDependencyProvider
    {
        IReadOnlyList<PoolDefinitionAsset> RuntimePoolDefinitions { get; }
    }
}
