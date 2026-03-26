using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.Config;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public interface ICompositionModuleDescriptor
    {
        string ModuleId { get; }
        IReadOnlyList<string> InstallerDependencies { get; }
        IReadOnlyList<string> BootstrapDependencies { get; }
        Action<BootstrapConfigAsset> Installer { get; }
        Action<BootstrapConfigAsset> Bootstrap { get; }
    }

    public sealed class CompositionModuleDescriptor : ICompositionModuleDescriptor
    {
        public CompositionModuleDescriptor(
            string moduleId,
            IReadOnlyList<string> installerDependencies,
            IReadOnlyList<string> bootstrapDependencies,
            Action<BootstrapConfigAsset> installer,
            Action<BootstrapConfigAsset> bootstrap)
        {
            ModuleId = moduleId ?? throw new ArgumentNullException(nameof(moduleId));
            InstallerDependencies = installerDependencies ?? Array.Empty<string>();
            BootstrapDependencies = bootstrapDependencies ?? Array.Empty<string>();
            Installer = installer;
            Bootstrap = bootstrap;
        }

        public string ModuleId { get; }
        public IReadOnlyList<string> InstallerDependencies { get; }
        public IReadOnlyList<string> BootstrapDependencies { get; }
        public Action<BootstrapConfigAsset> Installer { get; }
        public Action<BootstrapConfigAsset> Bootstrap { get; }
    }
}
