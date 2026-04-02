using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public interface ICompositionModuleDescriptor
    {
        string ModuleId { get; }
        string InstallerEntry { get; }
        string RuntimeComposerEntry { get; }
        IReadOnlyList<string> InstallerDependencies { get; }
        IReadOnlyList<string> BootstrapDependencies { get; }
        bool Optional { get; }
        bool InstallerOnly { get; }
        string Description { get; }
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
            Action<BootstrapConfigAsset> bootstrap,
            string installerEntry = null,
            string runtimeComposerEntry = null,
            bool optional = false,
            bool installerOnly = false,
            string description = null)
        {
            ModuleId = moduleId ?? throw new ArgumentNullException(nameof(moduleId));
            InstallerDependencies = installerDependencies ?? Array.Empty<string>();
            BootstrapDependencies = bootstrapDependencies ?? Array.Empty<string>();
            Optional = optional;
            InstallerOnly = installerOnly || (installer != null && bootstrap == null);
            Description = description;
            InstallerEntry = installerEntry ?? string.Empty;
            RuntimeComposerEntry = runtimeComposerEntry ?? string.Empty;
            Installer = installer;
            Bootstrap = bootstrap;

            if (!Optional && Installer == null && Bootstrap == null)
            {
                throw new ArgumentException(
                    $"Descriptor '{ModuleId}' deve expor installer e/ou bootstrap, ou marcar Optional=true para skip controlado.");
            }

            if (Installer == null && Bootstrap != null)
            {
                throw new ArgumentException($"Descriptor '{ModuleId}' nao pode expor bootstrap sem installer.");
            }

            if (InstallerOnly && Installer == null)
            {
                throw new ArgumentException($"Descriptor '{ModuleId}' marcou InstallerOnly=true sem Installer.");
            }

            if (InstallerOnly && Bootstrap != null)
            {
                throw new ArgumentException($"Descriptor '{ModuleId}' marcou InstallerOnly=true com Bootstrap nao nulo.");
            }
        }

        public string ModuleId { get; }
        public string InstallerEntry { get; }
        public string RuntimeComposerEntry { get; }
        public IReadOnlyList<string> InstallerDependencies { get; }
        public IReadOnlyList<string> BootstrapDependencies { get; }
        public bool Optional { get; }
        public bool InstallerOnly { get; }
        public string Description { get; }
        public Action<BootstrapConfigAsset> Installer { get; }
        public Action<BootstrapConfigAsset> Bootstrap { get; }
    }
}
